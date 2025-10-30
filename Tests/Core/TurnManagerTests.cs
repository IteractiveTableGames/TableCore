using System;
using System.Collections.Generic;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class TurnManagerTests
    {
        [Test]
        public void SetOrder_RemovesDuplicates_AndHonorsStartIndex()
        {
            var playerA = Guid.NewGuid();
            var playerB = Guid.NewGuid();
            var manager = new TurnManager();

            manager.SetOrder(new[] { playerA, playerA, Guid.Empty, playerB }, startIndex: 1);

            Assert.That(manager.TurnOrder, Is.EqualTo(new List<Guid> { playerA, playerB }));
            Assert.That(manager.CurrentPlayerId, Is.EqualTo(playerB));
        }

        [Test]
        public void AdvanceTurn_CyclesThroughPlayers()
        {
            var playerA = Guid.NewGuid();
            var playerB = Guid.NewGuid();
            var playerC = Guid.NewGuid();
            var manager = new TurnManager();

            manager.SetOrder(new[] { playerA, playerB, playerC });

            Assert.That(manager.CurrentPlayerId, Is.EqualTo(playerA));
            Assert.That(manager.AdvanceTurn(), Is.EqualTo(playerB));
            Assert.That(manager.AdvanceTurn(), Is.EqualTo(playerC));
            Assert.That(manager.AdvanceTurn(), Is.EqualTo(playerA));
        }

        [Test]
        public void TrySetCurrentPlayer_SucceedsOnlyWhenPlayerExists()
        {
            var playerA = Guid.NewGuid();
            var playerB = Guid.NewGuid();
            var manager = new TurnManager();
            manager.SetOrder(new[] { playerA });

            Assert.That(manager.TrySetCurrentPlayer(playerB), Is.False);
            Assert.That(manager.TrySetCurrentPlayer(playerA), Is.True);
            Assert.That(manager.CurrentPlayerId, Is.EqualTo(playerA));
        }

        [Test]
        public void Reset_ClearsTurnState()
        {
            var manager = new TurnManager();
            manager.SetOrder(new[] { Guid.NewGuid() });

            manager.Reset();

            Assert.That(() => _ = manager.CurrentPlayerId, Throws.InvalidOperationException);
        }

        [Test]
        public void CurrentPlayer_ThrowsWhenUnconfigured()
        {
            var manager = new TurnManager();

            Assert.That(() => _ = manager.CurrentPlayerId, Throws.InvalidOperationException);
        }

        [Test]
        public void AdvanceTurn_ThrowsWhenUnconfigured()
        {
            var manager = new TurnManager();

            Assert.That(() => manager.AdvanceTurn(), Throws.InvalidOperationException);
        }

        [Test]
        public void SetOrder_ValidatesArguments()
        {
            var manager = new TurnManager();

            Assert.That(() => manager.SetOrder(null!), Throws.ArgumentNullException);
            Assert.That(() => manager.SetOrder(Array.Empty<Guid>()), Throws.ArgumentException);
            Assert.That(() => manager.SetOrder(new[] { Guid.NewGuid() }, startIndex: 5), Throws.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
