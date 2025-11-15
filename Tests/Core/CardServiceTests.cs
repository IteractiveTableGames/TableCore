using System;
using System.Collections.Generic;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.UI;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class CardServiceTests
    {
        [Test]
        public void GetHand_WithEmptyPlayerId_Throws()
        {
            var service = new CardService();

            Assert.That(() => service.GetHand(Guid.Empty), Throws.ArgumentException);
        }

        [Test]
        public void GetHand_ReturnsSameInstancePerPlayer()
        {
            var service = new CardService();
            var playerId = Guid.NewGuid();

            var first = service.GetHand(playerId);
            var second = service.GetHand(playerId);

            Assert.That(second, Is.SameAs(first));
            Assert.That(first.OwnerPlayerId, Is.EqualTo(playerId));
        }

        [Test]
        public void GiveCardToPlayer_AddsCardAndNotifiesHud()
        {
            var hud = new RecordingHudService();
            var service = new CardService(hud);
            var playerId = Guid.NewGuid();
            var card = new CardData { CardId = "card-1" };
            (Guid Player, IReadOnlyList<CardData> Cards)? observedEvent = null;
            service.HandUpdated += (id, cards) => observedEvent = (id, cards);

            service.GiveCardToPlayer(playerId, card);

            var hand = service.GetHand(playerId);
            Assert.That(hand.Cards, Has.Count.EqualTo(1));
            Assert.That(hand.Cards[0], Is.SameAs(card));

            Assert.That(observedEvent?.Player, Is.EqualTo(playerId));
            Assert.That(observedEvent?.Cards, Has.Count.EqualTo(1));
            Assert.That(hud.HandUpdates, Is.EqualTo(new List<(Guid, IReadOnlyList<CardData>)>
            {
                (playerId, hand.Cards)
            }));
        }

        [Test]
        public void RemoveCardFromPlayer_RemovesCardAndNotifiesHud()
        {
            var hud = new RecordingHudService();
            var service = new CardService(hud);
            var playerId = Guid.NewGuid();
            var card = new CardData { CardId = "card-1" };
            service.GiveCardToPlayer(playerId, card);
            hud.HandUpdates.Clear();

            service.RemoveCardFromPlayer(playerId, card);

            Assert.That(service.GetHand(playerId).Cards, Is.Empty);
            Assert.That(hud.HandUpdates, Has.Count.EqualTo(1));
            Assert.That(hud.HandUpdates[0].Item1, Is.EqualTo(playerId));
            Assert.That(hud.HandUpdates[0].Item2, Is.Empty);
        }

        [Test]
        public void RemoveCardFromPlayer_WhenMissing_Throws()
        {
            var service = new CardService();
            var playerId = Guid.NewGuid();
            var card = new CardData { CardId = "missing" };

            Assert.That(() => service.RemoveCardFromPlayer(playerId, card), Throws.InvalidOperationException);
        }

        [Test]
        public void Reset_ClearsAllHandsAndNotifiesHud()
        {
            var hud = new RecordingHudService();
            var service = new CardService(hud);
            var playerA = Guid.NewGuid();
            var playerB = Guid.NewGuid();
            service.GiveCardToPlayer(playerA, new CardData { CardId = "A1" });
            service.GiveCardToPlayer(playerB, new CardData { CardId = "B1" });
            hud.HandUpdates.Clear();

            service.Reset();

            Assert.That(service.GetHand(playerA).Cards, Is.Empty);
            Assert.That(service.GetHand(playerB).Cards, Is.Empty);
            Assert.That(hud.HandUpdates, Has.Count.EqualTo(2));
        }

        private sealed class RecordingHudService : IHUDService
        {
            public List<(Guid, IReadOnlyList<CardData>)> HandUpdates { get; } = new();

            public IPlayerHUD CreatePlayerHUD(PlayerProfile player) => new NullPlayerHud(player.PlayerId);

            public void UpdateFunds(Guid playerId, int newAmount)
            {
            }

            public void UpdateHand(Guid playerId, IReadOnlyList<CardData> cards)
            {
                HandUpdates.Add((playerId, cards));
            }

            public void SetPrompt(Guid playerId, string message)
            {
            }

            public void ConfigureHudPlacement(HudPlacementOptions options)
            {
            }

            public void SetSeatRegionResolver(Func<SeatZone, Rect2>? resolver)
            {
            }

            private sealed class NullPlayerHud : IPlayerHUD
            {
                public NullPlayerHud(Guid playerId)
                {
                    PlayerId = playerId;
                }

                public Guid PlayerId { get; }

                public Control GetRootControl() => null;

                public void AddControl(Godot.Node controlNode)
                {
                }
            }
        }
    }
}
