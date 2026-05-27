/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Agents
{
    /// <summary>
    /// Non-blocking decision service with timeout, retry, and per-symbol caching.
    /// </summary>
    public class AgentAlphaService
    {
        private readonly AgentAlphaSettings _settings;
        private readonly IAgentDecisionProvider _provider;
        private readonly ConcurrentDictionary<Symbol, SymbolState> _states = new();
        private volatile bool _isPaused;
        private double _minimumConfidence;

        /// <summary>
        /// Initializes a new instance of the <see cref="AgentAlphaService"/> class.
        /// </summary>
        public AgentAlphaService(AgentAlphaSettings settings, IAgentDecisionProvider provider = null)
        {
            _settings = settings ?? new AgentAlphaSettings();
            _provider = provider ?? CreateDefaultProvider(settings);
            _minimumConfidence = _settings.MinimumConfidence;
        }

        /// <summary>
        /// Pause outbound agent requests and insight emissions.
        /// </summary>
        public void Pause() => _isPaused = true;

        /// <summary>
        /// Resume outbound agent requests and insight emissions.
        /// </summary>
        public void Resume() => _isPaused = false;

        /// <summary>
        /// Updates minimum confidence threshold in [0,1].
        /// </summary>
        public void SetMinimumConfidence(double minimumConfidence)
        {
            _minimumConfidence = Math.Max(0, Math.Min(1, minimumConfidence));
        }

        /// <summary>
        /// Creates a service with settings loaded from global config.
        /// </summary>
        public static AgentAlphaService FromConfig(string prefix = "agent-alpha")
        {
            return new AgentAlphaService(AgentAlphaSettings.FromConfig(prefix));
        }

        /// <summary>
        /// Triggers asynchronous refresh for a symbol if due and never blocks caller.
        /// </summary>
        public void Refresh(Symbol symbol, DateTime utcTime, AgentDecisionRequest request)
        {
            if (!_settings.Enabled || _isPaused || _provider == null || symbol == null)
            {
                return;
            }

            var state = _states.GetOrAdd(symbol, _ => new SymbolState());
            state.TryFinalize(utcTime);

            if (utcTime < state.NextRequestUtc || state.InFlight != null)
            {
                return;
            }

            state.NextRequestUtc = utcTime + _settings.RequestInterval;
            state.InFlight = RequestWithPolicyAsync(request);
        }

        /// <summary>
        /// Gets latest cached decision for symbol if it is still valid.
        /// </summary>
        public bool TryGetDecision(Symbol symbol, DateTime utcTime, out AgentDecision decision)
        {
            decision = null;
            if (_isPaused || symbol == null)
            {
                return false;
            }

            if (!_states.TryGetValue(symbol, out var state))
            {
                return false;
            }

            state.TryFinalize(utcTime);
            if (state.LastDecision == null)
            {
                return false;
            }

            if (utcTime - state.LastDecisionUtc > _settings.DecisionTtl)
            {
                return false;
            }

            if (state.LastDecision.Confidence < _minimumConfidence)
            {
                return false;
            }

            decision = state.LastDecision;
            return true;
        }

        /// <summary>
        /// Determines whether a symbol can emit now and marks the symbol as emitted.
        /// </summary>
        public bool CanEmit(Symbol symbol, DateTime utcTime, TimeSpan emitInterval)
        {
            if (!_states.TryGetValue(symbol, out var state))
            {
                return false;
            }

            if (state.LastEmitUtc != DateTime.MinValue && utcTime - state.LastEmitUtc < emitInterval)
            {
                return false;
            }

            state.LastEmitUtc = utcTime;
            return true;
        }

        private async Task<AgentDecision> RequestWithPolicyAsync(AgentDecisionRequest request)
        {
            var attempts = _settings.RetryCount + 1;
            for (var attempt = 1; attempt <= attempts; attempt++)
            {
                using var cts = new CancellationTokenSource(_settings.RequestTimeout);
                try
                {
                    var decision = await _provider.GetDecisionAsync(request, cts.Token).ConfigureAwait(false);
                    if (decision != null && decision.Symbol == null)
                    {
                        if (SymbolCache.TryGetSymbol(request.Symbol, out var symbol))
                        {
                            decision.Symbol = symbol;
                        }
                    }
                    return decision;
                }
                catch (OperationCanceledException)
                {
                    if (attempt == attempts)
                    {
                        Log.Debug($"AgentAlphaService.RequestWithPolicyAsync(): timeout requesting decision for {request.Symbol}");
                    }
                }
                catch (Exception exception)
                {
                    if (attempt == attempts)
                    {
                        Log.Debug($"AgentAlphaService.RequestWithPolicyAsync(): request failed for {request.Symbol}. {exception.Message}");
                    }
                }

                if (attempt < attempts && _settings.RetryBackoff > TimeSpan.Zero)
                {
                    await Task.Delay(_settings.RetryBackoff).ConfigureAwait(false);
                }
            }
            return null;
        }

        private static IAgentDecisionProvider CreateDefaultProvider(AgentAlphaSettings settings)
        {
            if (settings == null || string.IsNullOrWhiteSpace(settings.Endpoint))
            {
                return null;
            }
            return new HttpAgentDecisionProvider(settings.Endpoint);
        }

        private class SymbolState
        {
            public Task<AgentDecision> InFlight;
            public DateTime NextRequestUtc;
            public AgentDecision LastDecision;
            public DateTime LastDecisionUtc;
            public DateTime LastEmitUtc;

            public void TryFinalize(DateTime utcTime)
            {
                var inFlight = InFlight;
                if (inFlight == null || !inFlight.IsCompleted)
                {
                    return;
                }

                try
                {
                    var decision = inFlight.GetAwaiter().GetResult();
                    if (decision != null)
                    {
                        LastDecision = decision;
                        LastDecisionUtc = utcTime;
                    }
                }
                catch
                {
                    // ignore and keep previous decision
                }
                finally
                {
                    InFlight = null;
                }
            }
        }
    }
}
