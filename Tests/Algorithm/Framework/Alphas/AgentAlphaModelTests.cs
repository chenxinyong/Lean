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
using NUnit.Framework;
using QuantConnect.Algorithm.Agents;
using QuantConnect.Algorithm.Framework.Alphas;

namespace QuantConnect.Tests.Algorithm.Framework.Alphas
{
    [TestFixture]
    public class AgentAlphaModelTests
    {
        [Test]
        public void AppliesControlCommands()
        {
            var service = new AgentAlphaService(new AgentAlphaSettings { Enabled = true });
            var model = new AgentAlphaModel(service, TimeSpan.FromMinutes(1));

            Assert.IsTrue(model.ApplyControlCommand(new AgentControlCommand { Action = "pause" }));
            Assert.IsTrue(model.ApplyControlCommand(new AgentControlCommand { Action = "resume" }));
            Assert.IsTrue(model.ApplyControlCommand(new AgentControlCommand { Action = "set-confidence-threshold", ConfidenceThreshold = 0.8 }));
            Assert.IsFalse(model.ApplyControlCommand(new AgentControlCommand { Action = "set-confidence-threshold" }));
            Assert.IsFalse(model.ApplyControlCommand(new AgentControlCommand { Action = "unknown-action" }));
        }
    }
}
