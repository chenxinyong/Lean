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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Securities;

namespace QuantConnect.Algorithm.Agents
{
    /// <summary>
    /// A normalized decision returned by an external agent.
    /// </summary>
    public class AgentDecision
    {
        /// <summary>
        /// Target symbol.
        /// </summary>
        public Symbol Symbol { get; set; }

        /// <summary>
        /// Expected direction.
        /// </summary>
        public InsightDirection Direction { get; set; }

        /// <summary>
        /// Confidence [0,1].
        /// </summary>
        public double Confidence { get; set; }

        /// <summary>
        /// Optional expected magnitude.
        /// </summary>
        public double? Magnitude { get; set; }

        /// <summary>
        /// Optional desired portfolio weight.
        /// </summary>
        public double? Weight { get; set; }

        /// <summary>
        /// Optional period for resulting insight.
        /// </summary>
        public TimeSpan? Period { get; set; }
    }
}
