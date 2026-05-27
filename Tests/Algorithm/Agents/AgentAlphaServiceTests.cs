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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QuantConnect.Algorithm.Agents;
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Securities;

namespace QuantConnect.Tests.Algorithm.Agents
{
    [TestFixture]
    public class AgentAlphaServiceTests
    {
        [Test]
        public void RefreshIsNonBlockingWhileProviderInFlight()
        {
            var provider = new PendingProvider();
            var service = new AgentAlphaService(CreateSettings(), provider);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var request = new AgentDecisionRequest { Symbol = symbol.Value, Price = 100, UtcTime = DateTime.UtcNow };

            var stopwatch = Stopwatch.StartNew();
            service.Refresh(symbol, request.UtcTime, request);
            stopwatch.Stop();

            Assert.Less(stopwatch.ElapsedMilliseconds, 100);
            Assert.IsFalse(service.TryGetDecision(symbol, request.UtcTime, out _));
        }

        [Test]
        public void ReturnsDecisionWhenProviderCompletes()
        {
            var provider = new TestProvider(_ => Task.FromResult(new AgentDecision
            {
                Direction = InsightDirection.Up,
                Confidence = 0.9,
                Magnitude = 0.02
            }));

            var service = new AgentAlphaService(CreateSettings(), provider);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var utcTime = DateTime.UtcNow;
            service.Refresh(symbol, utcTime, new AgentDecisionRequest { Symbol = symbol.Value, Price = 100, UtcTime = utcTime });

            Assert.IsFalse(service.TryGetDecision(symbol, utcTime, out _));
            Assert.IsTrue(service.TryGetDecision(symbol, utcTime.AddSeconds(1), out var decision));
            Assert.AreEqual(InsightDirection.Up, decision.Direction);
        }

        [Test]
        public void CachedDecisionExpiresByTtl()
        {
            var settings = CreateSettings();
            settings.DecisionTtl = TimeSpan.FromSeconds(1);
            var provider = new TestProvider(_ => Task.FromResult(new AgentDecision
            {
                Direction = InsightDirection.Down,
                Confidence = 0.9
            }));

            var service = new AgentAlphaService(settings, provider);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var utcTime = DateTime.UtcNow;
            service.Refresh(symbol, utcTime, new AgentDecisionRequest { Symbol = symbol.Value, Price = 100, UtcTime = utcTime });
            Assert.IsTrue(service.TryGetDecision(symbol, utcTime.AddMilliseconds(10), out _));
            Assert.IsFalse(service.TryGetDecision(symbol, utcTime.AddSeconds(2), out _));
        }

        [Test]
        public void ConfidenceThresholdCanBeUpdated()
        {
            var provider = new TestProvider(_ => Task.FromResult(new AgentDecision
            {
                Direction = InsightDirection.Up,
                Confidence = 0.6
            }));

            var service = new AgentAlphaService(CreateSettings(), provider);
            var symbol = Symbol.Create("SPY", SecurityType.Equity, Market.USA);
            var utcTime = DateTime.UtcNow;
            service.Refresh(symbol, utcTime, new AgentDecisionRequest { Symbol = symbol.Value, Price = 100, UtcTime = utcTime });
            Assert.IsTrue(service.TryGetDecision(symbol, utcTime.AddMilliseconds(10), out _));

            service.SetMinimumConfidence(0.7);
            Assert.IsFalse(service.TryGetDecision(symbol, utcTime.AddMilliseconds(20), out _));
        }

        private static AgentAlphaSettings CreateSettings()
        {
            return new AgentAlphaSettings
            {
                Enabled = true,
                RequestInterval = TimeSpan.Zero,
                DecisionTtl = TimeSpan.FromMinutes(5),
                RequestTimeout = TimeSpan.FromSeconds(1),
                RetryCount = 0,
                MinimumConfidence = 0.5,
                InsightPeriod = TimeSpan.FromMinutes(1)
            };
        }

        private class PendingProvider : IAgentDecisionProvider
        {
            public Task<AgentDecision> GetDecisionAsync(AgentDecisionRequest request, CancellationToken cancellationToken)
            {
                return new TaskCompletionSource<AgentDecision>().Task;
            }
        }

        private class TestProvider : IAgentDecisionProvider
        {
            private readonly Func<AgentDecisionRequest, Task<AgentDecision>> _handler;

            public TestProvider(Func<AgentDecisionRequest, Task<AgentDecision>> handler)
            {
                _handler = handler;
            }

            public Task<AgentDecision> GetDecisionAsync(AgentDecisionRequest request, CancellationToken cancellationToken)
            {
                return _handler(request);
            }
        }
    }
}
