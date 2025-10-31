using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class DeckTests
    {
        [Test]
        public void Draw_FromEmptyDeck_Throws()
        {
            var deck = new Deck();

            Assert.That(() => deck.Draw(), Throws.InvalidOperationException);
        }

        [Test]
        public void Draw_ReturnsTopCard()
        {
            var cardA = new CardData { CardId = "A" };
            var cardB = new CardData { CardId = "B" };
            var deck = new Deck(new[] { cardA, cardB });

            var drawn = deck.Draw();

            Assert.That(drawn, Is.SameAs(cardB));
            Assert.That(deck.Count, Is.EqualTo(1));
            Assert.That(deck.Cards.Single(), Is.SameAs(cardA));
        }

        [Test]
        public void InsertBottom_PlacesCardAtBottom()
        {
            var cardA = new CardData { CardId = "A" };
            var cardB = new CardData { CardId = "B" };
            var deck = new Deck();
            deck.PlaceOnTop(cardA);

            deck.InsertBottom(cardB);

            Assert.That(deck.Count, Is.EqualTo(2));
            Assert.That(deck.Draw(), Is.SameAs(cardA));
            Assert.That(deck.Draw(), Is.SameAs(cardB));
        }

        [Test]
        public void Shuffle_UsesProvidedRandom()
        {
            var cards = new[]
            {
                new CardData { CardId = "A" },
                new CardData { CardId = "B" },
                new CardData { CardId = "C" }
            };
            var deck = new Deck(cards);
            var random = new SequenceRandom(new[] { 1, 0 });

            deck.Shuffle(random);

            Assert.That(deck.Cards.Select(c => c.CardId), Is.EqualTo(new[] { "C", "A", "B" }));
        }

        private sealed class SequenceRandom : System.Random
        {
            private readonly Queue<int> _values;

            public SequenceRandom(IEnumerable<int> values)
            {
                _values = new Queue<int>(values);
            }

            public override int Next(int maxValue)
            {
                if (_values.Count == 0)
                {
                    return 0;
                }

                var value = _values.Dequeue();
                if (value < 0)
                {
                    value = 0;
                }

                if (value >= maxValue)
                {
                    value = maxValue - 1;
                }

                return value;
            }
        }
    }
}
