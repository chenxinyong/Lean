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
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Logging;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Agents
{
    /// <summary>
    /// Simple HTTP implementation of <see cref="IAgentDecisionProvider"/>.
    /// </summary>
    public class HttpAgentDecisionProvider : IAgentDecisionProvider, IDisposable
    {
        private readonly Uri _endpoint;
        private readonly HttpClient _httpClient;
        private readonly bool _ownsClient;

        /// <summary>
        /// Creates a new provider targeting a JSON HTTP endpoint.
        /// </summary>
        public HttpAgentDecisionProvider(string endpoint, HttpClient httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(endpoint))
            {
                throw new ArgumentException("Agent endpoint must not be empty.", nameof(endpoint));
            }

            _endpoint = new Uri(endpoint);
            _httpClient = httpClient ?? new HttpClient();
            _ownsClient = httpClient == null;
        }

        /// <summary>
        /// Requests a decision and maps it into LEAN-native direction/confidence output.
        /// </summary>
        public async Task<AgentDecision> GetDecisionAsync(AgentDecisionRequest request, CancellationToken cancellationToken)
        {
            var payload = JsonConvert.SerializeObject(request);
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync(_endpoint, content, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                Log.Debug($"HttpAgentDecisionProvider.GetDecisionAsync(): endpoint returned status {(int)response.StatusCode} for {request.Symbol}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var dto = JsonConvert.DeserializeObject<ResponseDto>(json);
            if (dto == null)
            {
                return null;
            }

            if (!SymbolCache.TryGetSymbol(dto.Symbol ?? request.Symbol, out var symbol))
            {
                symbol = Symbol.Create(dto.Symbol ?? request.Symbol, SecurityType.Base, Market.USA);
            }

            return new AgentDecision
            {
                Symbol = symbol,
                Direction = ParseDirection(dto.Direction),
                Confidence = Math.Max(0, Math.Min(1, dto.Confidence ?? 0)),
                Magnitude = dto.Magnitude,
                Weight = dto.Weight,
                Period = dto.HorizonMinutes.HasValue ? TimeSpan.FromMinutes(Math.Max(1, dto.HorizonMinutes.Value)) : null
            };
        }

        /// <summary>
        /// Disposes owned resources.
        /// </summary>
        public void Dispose()
        {
            if (_ownsClient)
            {
                _httpClient.Dispose();
            }
        }

        private static InsightDirection ParseDirection(string direction)
        {
            if (string.IsNullOrWhiteSpace(direction))
            {
                return InsightDirection.Flat;
            }

            switch (direction.Trim().ToLowerInvariant())
            {
                case "up":
                case "long":
                case "buy":
                    return InsightDirection.Up;
                case "down":
                case "short":
                case "sell":
                    return InsightDirection.Down;
                default:
                    return InsightDirection.Flat;
            }
        }

        private class ResponseDto
        {
            public string Symbol { get; set; }
            public string Direction { get; set; }
            public double? Confidence { get; set; }
            public double? Magnitude { get; set; }
            public double? Weight { get; set; }
            public double? HorizonMinutes { get; set; }
        }
    }
}
