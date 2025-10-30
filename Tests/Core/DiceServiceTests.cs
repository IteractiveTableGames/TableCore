#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class DiceServiceTests
    {
        [Test]
        public void Roll_ReturnsValueWithinRange()
        {
            var service = new DiceService(new Random(42));

            var result = service.Roll(6);

            Assert.That(result, Is.InRange(1, 6));
        }

        [Test]
        public void RollMultiple_ProducesExpectedCount()
        {
            var service = new DiceService(new Random(123));

            var results = service.RollMultiple(5, 8);

            Assert.That(results, Has.Length.EqualTo(5));
            Assert.That(results, Has.All.InRange(1, 8));
        }

        [Test]
        public void Roll_ValidatesSides()
        {
            var service = new DiceService();
            Assert.That(() => service.Roll(1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void RollMultiple_ValidatesArguments()
        {
            var service = new DiceService(new Random(0));

            Assert.That(() => service.RollMultiple(0, 6), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => service.RollMultiple(2, 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public async Task RollWithAnimation_InvokesCallback()
        {
            var service = new DiceService(new Random(7));
            IReadOnlyList<int>? animationResults = null;
            service.RollAnimationCallback = (values, _) =>
            {
                animationResults = values;
                return Task.CompletedTask;
            };

            var results = await service.RollWithAnimation(3, 6, CancellationToken.None);

            Assert.That(results, Has.Length.EqualTo(3));
            Assert.That(animationResults, Is.EqualTo(results));
        }

        [Test]
        public void RollWithAnimation_PropagatesCancellation()
        {
            var service = new DiceService(new Random(99));
            service.RollAnimationCallback = (_, token) => Task.Run(async () =>
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(10, token);
            }, token);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            Assert.That(async () => await service.RollWithAnimation(2, 6, cts.Token), Throws.InstanceOf<OperationCanceledException>());
        }
    }
}
