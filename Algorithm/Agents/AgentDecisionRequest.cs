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

namespace QuantConnect.Algorithm.Agents
{
    /// <summary>
    /// Feature payload sent to the external agent per symbol.
    /// </summary>
    public class AgentDecisionRequest
    {
        /// <summary>
        /// Symbol value (for transport).
        /// </summary>
        public string Symbol { get; set; }

        /// <summary>
        /// Current UTC time.
        /// </summary>
        public DateTime UtcTime { get; set; }

        /// <summary>
        /// Latest price.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Optional bar open.
        /// </summary>
        public decimal? Open { get; set; }

        /// <summary>
        /// Optional bar high.
        /// </summary>
        public decimal? High { get; set; }

        /// <summary>
        /// Optional bar low.
        /// </summary>
        public decimal? Low { get; set; }

        /// <summary>
        /// Optional bar close.
        /// </summary>
        public decimal? Close { get; set; }

        /// <summary>
        /// Optional bar volume.
        /// </summary>
        public decimal? Volume { get; set; }
    }
}
