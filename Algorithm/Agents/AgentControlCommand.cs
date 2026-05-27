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

using QuantConnect.Interfaces;
using QuantConnect.Commands;
using QuantConnect.Algorithm;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Algorithm.Agents
{
    /// <summary>
    /// Optional command contract for controlling <see cref="AgentAlphaModel"/> at runtime.
    /// </summary>
    public class AgentControlCommand : Command
    {
        /// <summary>
        /// Action name: pause | resume | set-confidence-threshold
        /// </summary>
        public string Action { get; set; }

        /// <summary>
        /// New confidence threshold in [0,1], used by set-confidence-threshold.
        /// </summary>
        public double? ConfidenceThreshold { get; set; }

        /// <summary>
        /// Executes this command against the algorithm's current alpha model when applicable.
        /// </summary>
        public override bool? Run(IAlgorithm algorithm)
        {
            if (algorithm is QCAlgorithm qcAlgorithm && qcAlgorithm.Alpha is AgentAlphaModel model)
            {
                return model.ApplyControlCommand(this);
            }
            return false;
        }
    }
}
