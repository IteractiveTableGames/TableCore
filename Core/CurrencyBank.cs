using System;
using System.Collections.Generic;

namespace TableCore.Core
{
    /// <summary>
    /// Tracks player currency balances and raises events when they change.
    /// </summary>
    public class CurrencyBank
    {
        private readonly Dictionary<Guid, int> _balances = new();

        /// <summary>
        /// Raised whenever a player's balance changes.
        /// </summary>
        public event Action<Guid, int>? BalanceChanged;

        /// <summary>
        /// Gets the balance for the specified player. Players without an account default to zero.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <returns>The player's current balance.</returns>
        public int GetBalance(Guid playerId)
        {
            if (_balances.TryGetValue(playerId, out var balance))
            {
                return balance;
            }

            return 0;
        }

        /// <summary>
        /// Sets the balance for the specified player and raises the change event.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <param name="amount">The new balance.</param>
        public void SetBalance(Guid playerId, int amount)
        {
            _balances[playerId] = amount;
            BalanceChanged?.Invoke(playerId, amount);
        }

        /// <summary>
        /// Adds the specified amount to a player's balance.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <param name="amount">The amount to add. Can be negative to subtract.</param>
        /// <returns>The new balance for the player.</returns>
        public int Add(Guid playerId, int amount)
        {
            var newBalance = GetBalance(playerId) + amount;
            _balances[playerId] = newBalance;
            BalanceChanged?.Invoke(playerId, newBalance);
            return newBalance;
        }

        /// <summary>
        /// Attempts to transfer funds between two players.
        /// </summary>
        /// <param name="fromPlayer">The player paying the funds.</param>
        /// <param name="toPlayer">The player receiving the funds.</param>
        /// <param name="amount">The amount to transfer. Must be non-negative.</param>
        /// <returns>True when the transfer is successful; otherwise false.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="amount"/> is negative.</exception>
        public bool Transfer(Guid fromPlayer, Guid toPlayer, int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Transfer amount cannot be negative.");
            }

            if (amount == 0)
            {
                EnsureAccountExists(toPlayer);
                EnsureAccountExists(fromPlayer);
                return true;
            }

            if (fromPlayer == toPlayer)
            {
                EnsureAccountExists(fromPlayer);
                return true;
            }

            var fromBalance = GetBalance(fromPlayer);
            if (fromBalance < amount)
            {
                return false;
            }

            var toBalance = GetBalance(toPlayer) + amount;
            _balances[fromPlayer] = fromBalance - amount;
            _balances[toPlayer] = toBalance;

            BalanceChanged?.Invoke(fromPlayer, _balances[fromPlayer]);
            BalanceChanged?.Invoke(toPlayer, toBalance);

            return true;
        }

        /// <summary>
        /// Clears all stored balances.
        /// </summary>
        public void Reset()
        {
            if (_balances.Count == 0)
            {
                return;
            }

            var affectedPlayers = new List<Guid>(_balances.Keys);
            _balances.Clear();

            foreach (var playerId in affectedPlayers)
            {
                BalanceChanged?.Invoke(playerId, 0);
            }
        }

        private void EnsureAccountExists(Guid playerId)
        {
            if (!_balances.ContainsKey(playerId))
            {
                _balances[playerId] = 0;
                BalanceChanged?.Invoke(playerId, 0);
            }
        }
    }
}
