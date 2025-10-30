using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace TableCore.Core
{
    /// <summary>
    /// Provides deterministic dice rolls with an optional hook for visual animations.
    /// </summary>
    public class DiceService
    {
        private readonly Random _random;

        /// <summary>
        /// Creates a new instance of the dice service.
        /// </summary>
        /// <param name="random">Optional random number generator to support deterministic testing.</param>
        public DiceService(Random? random = null)
        {
            _random = random ?? Random.Shared;
        }

        /// <summary>
        /// Gets or sets the callback used to animate dice rolls.
        /// </summary>
        public Func<IReadOnlyList<int>, CancellationToken, Task>? RollAnimationCallback { get; set; }

        /// <summary>
        /// Rolls a single die with the specified number of sides.
        /// </summary>
        /// <param name="sides">The number of sides on the die. Must be at least two.</param>
        /// <returns>The inclusive result of the roll.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sides"/> is less than two.</exception>
        public int Roll(int sides)
        {
            if (sides < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(sides), sides, "Dice must have at least two sides.");
            }

            return _random.Next(1, sides + 1);
        }

        /// <summary>
        /// Rolls multiple dice of the same type and returns their results.
        /// </summary>
        /// <param name="diceCount">The number of dice to roll. Must be positive.</param>
        /// <param name="sides">The number of sides on the dice. Must be at least two.</param>
        /// <returns>An array containing each die result.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="diceCount"/> or <paramref name="sides"/> are invalid.</exception>
        public int[] RollMultiple(int diceCount, int sides)
        {
            if (diceCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(diceCount), diceCount, "The number of dice must be positive.");
            }

            var results = new int[diceCount];
            for (var i = 0; i < diceCount; i++)
            {
                results[i] = Roll(sides);
            }

            return results;
        }

        /// <summary>
        /// Rolls multiple dice and plays the configured animation before returning the results.
        /// </summary>
        /// <param name="diceCount">The number of dice to roll. Must be positive.</param>
        /// <param name="sides">The number of sides on the dice. Must be at least two.</param>
        /// <param name="cancellationToken">Token used to cancel an in-progress animation.</param>
        /// <returns>The rolled results.</returns>
        public async Task<int[]> RollWithAnimation(int diceCount, int sides, CancellationToken cancellationToken = default)
        {
            var results = RollMultiple(diceCount, sides);
            var animationCallback = RollAnimationCallback;

            if (animationCallback is not null)
            {
                await animationCallback(results, cancellationToken).ConfigureAwait(false);
            }

            return results;
        }
    }
}
