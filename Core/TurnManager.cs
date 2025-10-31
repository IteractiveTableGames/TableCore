using System;
using System.Collections.Generic;

namespace TableCore.Core
{
    /// <summary>
    /// Manages the linear sequence of player turns for a session.
    /// </summary>
    public class TurnManager
    {
        private readonly List<Guid> _turnOrder = new();
        private int _currentIndex;

        /// <summary>
        /// Raised whenever the active player changes. Emits <see cref="Guid.Empty"/> when no player is active.
        /// </summary>
        public event Action<Guid>? TurnChanged;

        /// <summary>
        /// Gets the identifier of the player whose turn is currently active.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the turn order has not been configured.</exception>
        public Guid CurrentPlayerId
        {
            get
            {
                if (_turnOrder.Count == 0)
                {
                    throw new InvalidOperationException("Turn order has not been configured.");
                }

                return _turnOrder[_currentIndex];
            }
        }

        /// <summary>
        /// Exposes the configured turn order as a read-only list.
        /// </summary>
        public IReadOnlyList<Guid> TurnOrder => _turnOrder.AsReadOnly();

        /// <summary>
        /// Configures the turn order using the provided player identifiers.
        /// </summary>
        /// <param name="playerIds">Unique player identifiers in the desired turn sequence.</param>
        /// <param name="startIndex">Optional starting index for the first active player.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="playerIds"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when no valid player identifiers are provided.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="startIndex"/> is outside the valid range.</exception>
        public void SetOrder(IEnumerable<Guid> playerIds, int startIndex = 0)
        {
            if (playerIds is null)
            {
                throw new ArgumentNullException(nameof(playerIds));
            }

            var distinctOrder = new List<Guid>();
            var seen = new HashSet<Guid>();

            foreach (var playerId in playerIds)
            {
                if (playerId == Guid.Empty)
                {
                    continue;
                }

                if (seen.Add(playerId))
                {
                    distinctOrder.Add(playerId);
                }
            }

            if (distinctOrder.Count == 0)
            {
                throw new ArgumentException("At least one non-empty player identifier is required.", nameof(playerIds));
            }

            if (startIndex < 0 || startIndex >= distinctOrder.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(startIndex), startIndex, "Start index must reference a valid player in the turn order.");
            }

            _turnOrder.Clear();
            _turnOrder.AddRange(distinctOrder);
            _currentIndex = startIndex;

            TurnChanged?.Invoke(_turnOrder[_currentIndex]);
        }

        /// <summary>
        /// Advances the turn to the next player in the sequence and returns their identifier.
        /// </summary>
        /// <returns>The identifier of the player whose turn becomes active.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the turn order has not been configured.</exception>
        public Guid AdvanceTurn()
        {
            if (_turnOrder.Count == 0)
            {
                throw new InvalidOperationException("Turn order has not been configured.");
            }

            _currentIndex = (_currentIndex + 1) % _turnOrder.Count;
            var currentPlayer = _turnOrder[_currentIndex];
            TurnChanged?.Invoke(currentPlayer);
            return currentPlayer;
        }

        /// <summary>
        /// Attempts to reposition the current turn to the specified player.
        /// </summary>
        /// <param name="playerId">The player that should become the current turn holder.</param>
        /// <returns>True when the player exists in the configured order and was set as current; otherwise false.</returns>
        public bool TrySetCurrentPlayer(Guid playerId)
        {
            if (_turnOrder.Count == 0)
            {
                return false;
            }

            var index = _turnOrder.IndexOf(playerId);
            if (index < 0)
            {
                return false;
            }

            _currentIndex = index;
            TurnChanged?.Invoke(_turnOrder[_currentIndex]);
            return true;
        }

        /// <summary>
        /// Clears the configured turn order and resets the manager.
        /// </summary>
        public void Reset()
        {
            var hadPlayers = _turnOrder.Count > 0;
            _turnOrder.Clear();
            _currentIndex = 0;

            if (hadPlayers)
            {
                TurnChanged?.Invoke(Guid.Empty);
            }
        }
    }
}
