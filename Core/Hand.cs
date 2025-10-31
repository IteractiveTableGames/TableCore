using System;
using System.Collections.Generic;

namespace TableCore.Core
{
    /// <summary>
    /// Represents the private collection of cards owned by a player.
    /// </summary>
    public class Hand
    {
        private readonly List<CardData> _cards = new();
        private readonly IReadOnlyList<CardData> _readOnlyView;

        /// <summary>
        /// Initializes a new instance of the <see cref="Hand"/> class.
        /// </summary>
        /// <param name="ownerPlayerId">The identifier of the player that owns the hand.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="ownerPlayerId"/> is empty.</exception>
        public Hand(Guid ownerPlayerId)
        {
            if (ownerPlayerId == Guid.Empty)
            {
                throw new ArgumentException("A hand must be associated with a player.", nameof(ownerPlayerId));
            }

            OwnerPlayerId = ownerPlayerId;
            _readOnlyView = _cards.AsReadOnly();
        }

        /// <summary>
        /// Gets the identifier of the player that owns this hand.
        /// </summary>
        public Guid OwnerPlayerId { get; }

        /// <summary>
        /// Gets the current cards as a read-only projection. Cards are ordered by draw order, newest last.
        /// </summary>
        public IReadOnlyList<CardData> Cards => _readOnlyView;

        /// <summary>
        /// Adds a card to the hand.
        /// </summary>
        /// <param name="card">The card to add.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
        public void Add(CardData card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            _cards.Add(card);
        }

        /// <summary>
        /// Removes the specified card from the hand.
        /// </summary>
        /// <param name="card">The card to remove.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="card"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the card is not present in the hand.</exception>
        public void Remove(CardData card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            if (!_cards.Remove(card))
            {
                throw new InvalidOperationException("The specified card is not present in the hand.");
            }
        }

        /// <summary>
        /// Removes all cards from the hand.
        /// </summary>
        public void Clear() => _cards.Clear();

        /// <summary>
        /// Determines whether the specified card is currently held.
        /// </summary>
        /// <param name="card">The card to check.</param>
        /// <returns>True when the card is held; otherwise false.</returns>
        public bool Contains(CardData card)
        {
            if (card is null)
            {
                return false;
            }

            return _cards.Contains(card);
        }
    }
}
