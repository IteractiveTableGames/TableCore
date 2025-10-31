using System;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class HandTests
    {
        [Test]
        public void Constructor_WithEmptyPlayerId_Throws()
        {
            Assert.That(() => new Hand(Guid.Empty), Throws.ArgumentException);
        }

        [Test]
        public void Add_AppendsCardToHand()
        {
            var playerId = Guid.NewGuid();
            var hand = new Hand(playerId);
            var card = new CardData { CardId = "card-1" };

            hand.Add(card);

            Assert.That(hand.Cards, Has.Count.EqualTo(1));
            Assert.That(hand.Cards[0], Is.SameAs(card));
        }

        [Test]
        public void Remove_RemovesCardFromHand()
        {
            var playerId = Guid.NewGuid();
            var hand = new Hand(playerId);
            var card = new CardData { CardId = "card-1" };
            hand.Add(card);

            hand.Remove(card);

            Assert.That(hand.Cards, Is.Empty);
        }

        [Test]
        public void Remove_WhenCardMissing_Throws()
        {
            var hand = new Hand(Guid.NewGuid());
            var card = new CardData { CardId = "missing" };

            Assert.That(() => hand.Remove(card), Throws.InvalidOperationException);
        }

        [Test]
        public void Contains_ReturnsTrueWhenCardPresent()
        {
            var card = new CardData { CardId = "card-1" };
            var hand = new Hand(Guid.NewGuid());
            hand.Add(card);

            Assert.That(hand.Contains(card), Is.True);
            Assert.That(hand.Contains(new CardData { CardId = "card-2" }), Is.False);
        }

        [Test]
        public void Clear_RemovesAllCards()
        {
            var hand = new Hand(Guid.NewGuid());
            hand.Add(new CardData());
            hand.Add(new CardData());

            hand.Clear();

            Assert.That(hand.Cards, Is.Empty);
        }
    }
}
