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
using QuantConnect.Configuration;

namespace QuantConnect.Algorithm.Agents
{
    /// <summary>
    /// Configuration settings for <see cref="AgentAlphaService"/> and <see cref="Framework.Alphas.AgentAlphaModel"/>.
    /// </summary>
    public class AgentAlphaSettings
    {
        /// <summary>
        /// True to enable external agent requests.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// Agent endpoint URL.
        /// </summary>
        public string Endpoint { get; set; } = string.Empty;

        /// <summary>
        /// How often to request a new decision per symbol.
        /// </summary>
        public TimeSpan RequestInterval { get; set; } = TimeSpan.FromMinutes(1);

        /// <summary>
        /// How long to keep a decision active before expiring it.
        /// </summary>
        public TimeSpan DecisionTtl { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Timeout per outbound request.
        /// </summary>
        public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromMilliseconds(1500);

        /// <summary>
        /// Number of retries after first failed attempt.
        /// </summary>
        public int RetryCount { get; set; } = 1;

        /// <summary>
        /// Delay between retries.
        /// </summary>
        public TimeSpan RetryBackoff { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Minimum confidence required to emit an insight.
        /// </summary>
        public double MinimumConfidence { get; set; } = 0.55;

        /// <summary>
        /// Default insight period used when the agent doesn't specify one.
        /// </summary>
        public TimeSpan InsightPeriod { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Loads settings from <see cref="Config"/> using the provided prefix.
        /// </summary>
        public static AgentAlphaSettings FromConfig(string prefix = "agent-alpha")
        {
            return new AgentAlphaSettings
            {
                Enabled = Config.GetBool($"{prefix}-enabled", false),
                Endpoint = Config.Get($"{prefix}-endpoint", string.Empty),
                RequestInterval = TimeSpan.FromSeconds(Config.GetInt($"{prefix}-request-interval-seconds", 60)),
                DecisionTtl = TimeSpan.FromSeconds(Config.GetInt($"{prefix}-decision-ttl-seconds", 300)),
                RequestTimeout = TimeSpan.FromMilliseconds(Config.GetInt($"{prefix}-request-timeout-ms", 1500)),
                RetryCount = Math.Max(0, Config.GetInt($"{prefix}-retry-count", 1)),
                RetryBackoff = TimeSpan.FromMilliseconds(Math.Max(0, Config.GetInt($"{prefix}-retry-backoff-ms", 200))),
                MinimumConfidence = Math.Max(0, Math.Min(1, Config.GetDouble($"{prefix}-minimum-confidence", 0.55))),
                InsightPeriod = TimeSpan.FromSeconds(Math.Max(1, Config.GetInt($"{prefix}-insight-period-seconds", 300)))
            };
        }
    }
}
