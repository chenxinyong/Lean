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
using System.Collections.Generic;
using QuantConnect.Algorithm.Agents;
using QuantConnect.Data;
using QuantConnect.Data.Market;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Agent-assisted alpha model:
    /// - never places orders directly
    /// - emits <see cref="Insight"/> suggestions only
    /// - uses async, non-blocking external inference with local cache/TTL fallback
    /// </summary>
    public class AgentAlphaModel : AlphaModel
    {
        private readonly AgentAlphaService _service;
        private readonly InsightType _type;
        private readonly TimeSpan _insightPeriod;
        private readonly HashSet<Security> _securities = new();

        /// <summary>
        /// Initializes a new instance with config-based settings.
        /// </summary>
        public AgentAlphaModel()
            : this(AgentAlphaService.FromConfig(), AgentAlphaSettings.FromConfig().InsightPeriod, InsightType.Price)
        {
        }

        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        public AgentAlphaModel(AgentAlphaService service, TimeSpan insightPeriod, InsightType type = InsightType.Price)
        {
            _service = service ?? AgentAlphaService.FromConfig();
            _insightPeriod = insightPeriod;
            _type = type;
            Name = $"{nameof(AgentAlphaModel)}({_type},{_insightPeriod})";
        }

        /// <summary>
        /// Updates this alpha model and emits insights from latest valid agent decisions.
        /// </summary>
        public override IEnumerable<Insight> Update(QCAlgorithm algorithm, Slice data)
        {
            foreach (var security in _securities)
            {
                if (security.Symbol.IsCanonical() || security.Price == 0)
                {
                    continue;
                }

                _service.Refresh(security.Symbol, algorithm.UtcTime, CreateRequest(security, algorithm.UtcTime, data));
                if (_service.TryGetDecision(security.Symbol, algorithm.UtcTime, out var decision)
                    && _service.CanEmit(security.Symbol, algorithm.UtcTime, _insightPeriod))
                {
                    yield return new Insight(
                        security.Symbol,
                        decision.Period ?? _insightPeriod,
                        _type,
                        decision.Direction,
                        decision.Magnitude,
                        decision.Confidence,
                        weight: decision.Weight,
                        sourceModel: Name);
                }
            }
        }

        /// <summary>
        /// Handles universe security changes.
        /// </summary>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var added in changes.AddedSecurities)
            {
                _securities.Add(added);
            }
            foreach (var removed in changes.RemovedSecurities)
            {
                _securities.Remove(removed);
            }
        }

        /// <summary>
        /// Applies a runtime control command.
        /// </summary>
        public bool? ApplyControlCommand(AgentControlCommand command)
        {
            if (command == null || string.IsNullOrWhiteSpace(command.Action))
            {
                return false;
            }

            switch (command.Action.Trim().ToLowerInvariant())
            {
                case "pause":
                    _service.Pause();
                    return true;
                case "resume":
                    _service.Resume();
                    return true;
                case "set-confidence-threshold":
                    if (command.ConfidenceThreshold.HasValue)
                    {
                        _service.SetMinimumConfidence(command.ConfidenceThreshold.Value);
                        return true;
                    }
                    return false;
                default:
                    return false;
            }
        }

        private static AgentDecisionRequest CreateRequest(Security security, DateTime utcTime, Slice data)
        {
            var request = new AgentDecisionRequest
            {
                Symbol = security.Symbol.Value,
                UtcTime = utcTime,
                Price = security.Price
            };

            if (data.Bars.TryGetValue(security.Symbol, out TradeBar tradeBar))
            {
                request.Open = tradeBar.Open;
                request.High = tradeBar.High;
                request.Low = tradeBar.Low;
                request.Close = tradeBar.Close;
                request.Volume = tradeBar.Volume;
            }

            return request;
        }
    }
}
