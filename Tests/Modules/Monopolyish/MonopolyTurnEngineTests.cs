using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Modules.Monopolyish;

namespace TableCore.Tests.Modules.Monopolyish
{
    [TestFixture]
    public sealed class MonopolyTurnEngineTests
    {
        private const int TileCount = 16;

        [Test]
        public void ResolveTurn_AwardsPassingBonus()
        {
            var (engine, bank, _, playerId, _) = CreateEngine();
            var startingBalance = 500;
            bank.SetBalance(playerId, startingBalance);

            var outcome = engine.ResolveTurn(playerId, 0, TileCount + 7);

            Assert.That(bank.GetBalance(playerId), Is.EqualTo(startingBalance + 200));
            Assert.That(outcome.Events.OfType<BonusCollectedEvent>(), Is.Not.Empty);
        }

        [Test]
        public void ResolveTurn_PurchasesUnownedProperty()
        {
            var (engine, bank, cardService, playerId, _) = CreateEngine();
            bank.SetBalance(playerId, 500);

            var outcome = engine.ResolveTurn(playerId, 0, 1);

            Assert.That(bank.GetBalance(playerId), Is.EqualTo(380)); // 500 - cost 120
            var hand = cardService.GetHand(playerId);
            Assert.That(hand.Cards, Has.Some.Matches<CardData>(card => card.Title == "Maple Street"));
            Assert.That(outcome.Events.OfType<PropertyPurchasedEvent>(), Is.Not.Empty);
        }

        [Test]
        public void ResolveTurn_PaysRentToOwner()
        {
            var (engine, bank, _, ownerId, _) = CreateEngine();
            var renterId = Guid.NewGuid();

            bank.SetBalance(ownerId, 500);
            bank.SetBalance(renterId, 500);

            // Owner buys the property at tile index 1.
            engine.ResolveTurn(ownerId, 0, 1);
            var ownerAfterPurchase = bank.GetBalance(ownerId);

            // Renter lands on the same property.
            engine.ResolveTurn(renterId, 0, 1);

            Assert.That(bank.GetBalance(renterId), Is.EqualTo(488)); // 500 - rent 12
            Assert.That(bank.GetBalance(ownerId), Is.EqualTo(ownerAfterPurchase + 12));
        }

        [Test]
        public void GetOwner_ReturnsBuyer()
        {
            var (engine, bank, cardService, playerId, tiles) = CreateEngine();
            bank.SetBalance(playerId, 500);

            engine.ResolveTurn(playerId, 0, 1);
            var propertyTile = tiles[1];
            Assert.That(engine.GetOwner(propertyTile), Is.EqualTo(playerId));
        }

        private static (MonopolyTurnEngine engine, CurrencyBank bank, CardService cardService, Guid playerId, IReadOnlyList<MonopolyTileDefinition> tiles) CreateEngine()
        {
            var bank = new CurrencyBank();
            var cardService = new CardService();
            var tiles = MonopolyTileLibrary.CreateDefaultTrack(TileCount);
            var engine = new MonopolyTurnEngine(bank, cardService, tiles);
            var playerId = Guid.NewGuid();
            return (engine, bank, cardService, playerId, tiles);
        }
    }
}
