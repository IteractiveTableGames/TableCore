using System;
using System.Collections.Generic;

namespace TableCore.Core
{
    /// <summary>
    /// Tracks card hands per player and relays updates to interested systems such as the HUD.
    /// </summary>
    public class CardService
    {
        private readonly Dictionary<Guid, Hand> _handsByPlayer = new();
        private readonly IHUDService? _hudService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CardService"/> class.
        /// </summary>
        /// <param name="hudService">Optional HUD service that is notified when hands change.</param>
        public CardService(IHUDService? hudService = null)
        {
            _hudService = hudService;
        }

        /// <summary>
        /// Raised whenever a player's hand changes.
        /// </summary>
        public event Action<Guid, IReadOnlyList<CardData>>? HandUpdated;

        /// <summary>
        /// Retrieves the hand for the specified player, creating it when necessary.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <returns>The player's hand.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="playerId"/> is empty.</exception>
        public Hand GetHand(Guid playerId)
        {
            if (playerId == Guid.Empty)
            {
                throw new ArgumentException("Player identifier cannot be empty.", nameof(playerId));
            }

            if (_handsByPlayer.TryGetValue(playerId, out var hand))
            {
                return hand;
            }

            hand = new Hand(playerId);
            _handsByPlayer[playerId] = hand;
            return hand;
        }

        /// <summary>
        /// Adds the specified card to the player's hand and notifies listeners.
        /// </summary>
        /// <param name="playerId">The player receiving the card.</param>
        /// <param name="card">The card to add.</param>
        public void GiveCardToPlayer(Guid playerId, CardData card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var hand = GetHand(playerId);
            hand.Add(card);
            NotifyHandChanged(hand);
        }

        /// <summary>
        /// Removes the specified card from the player's hand and notifies listeners.
        /// </summary>
        /// <param name="playerId">The player whose hand should be updated.</param>
        /// <param name="card">The card to remove.</param>
        public void RemoveCardFromPlayer(Guid playerId, CardData card)
        {
            if (card is null)
            {
                throw new ArgumentNullException(nameof(card));
            }

            var hand = GetHand(playerId);
            hand.Remove(card);
            NotifyHandChanged(hand);
        }

        /// <summary>
        /// Clears all tracked hands.
        /// </summary>
        public void Reset()
        {
            if (_handsByPlayer.Count == 0)
            {
                return;
            }

            foreach (var hand in _handsByPlayer.Values)
            {
                hand.Clear();
                NotifyHandChanged(hand);
            }

            _handsByPlayer.Clear();
        }

        private void NotifyHandChanged(Hand hand)
        {
            var cards = hand.Cards;
            HandUpdated?.Invoke(hand.OwnerPlayerId, cards);
            _hudService?.UpdateHand(hand.OwnerPlayerId, cards);
        }
    }
}
