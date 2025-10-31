using System;
using System.Collections.Generic;

namespace TableCore.Core
{
    /// <summary>
    /// Represents an ordered collection of <see cref="CardData"/> that can be shuffled and drawn from.
    /// </summary>
    public class Deck
    {
        private readonly List<CardData> _cards;
        private readonly Random _random;
        private readonly IReadOnlyList<CardData> _readOnlyView;

        /// <summary>
        /// Initializes a new instance of the <see cref="Deck"/> class.
        /// </summary>
        /// <param name="cards">Optional initial card sequence. The last element is considered the top of the deck.</param>
        /// <param name="random">Optional random number generator. When omitted, <see cref="Random.Shared"/> is used.</param>
        public Deck(IEnumerable<CardData>? cards = null, Random? random = null)
        {
            _cards = cards is null ? new List<CardData>() : new List<CardData>(cards);
            _random = random ?? Random.Shared;
            _readOnlyView = _cards.AsReadOnly();
        }

        /// <summary>
        /// Gets the current number of cards in the deck.
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        /// Exposes the current deck ordering as a read-only list. Index zero is the bottom of the deck.
        /// </summary>
        public IReadOnlyList<CardData> Cards => _readOnlyView;

        /// <summary>
        /// Randomizes the order of the deck using the provided or default random number generator.
        /// </summary>
        /// <param name="random">Optional random source to use for the shuffle.</param>
        public void Shuffle(Random? random = null)
        {
            if (_cards.Count <= 1)
            {
                return;
            }

            var rng = random ?? _random;

            for (var i = _cards.Count - 1; i > 0; i--)
            {
                var swapIndex = rng.Next(i + 1);
                (_cards[i], _cards[swapIndex]) = (_cards[swapIndex], _cards[i]);
            }
        }

        /// <summary>
        /// Removes and returns the card at the top of the deck.
        /// </summary>
        /// <returns>The card from the top of the deck.</returns>
        /// <exception cref="InvalidOperationException">Thrown when attempting to draw from an empty deck.</exception>
        public CardData Draw()
        {
            if (_cards.Count == 0)
            {
                throw new InvalidOperationException("Cannot draw from an empty deck.");
            }

            var lastIndex = _cards.Count - 1;
            var card = _cards[lastIndex];
            _cards.RemoveAt(lastIndex);
            return card;
        }

        /// <summary>
        /// Places the specified card on top of the deck.
        /// </summary>
        /// <param name="card">The card to place on top.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
        public void PlaceOnTop(CardData card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _cards.Add(card);
        }

        /// <summary>
        /// Inserts the specified card at the bottom of the deck.
        /// </summary>
        /// <param name="card">The card to insert.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
        public void InsertBottom(CardData card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _cards.Insert(0, card);
        }
    }
}
