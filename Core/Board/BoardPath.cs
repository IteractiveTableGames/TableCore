using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TableCore.Core.Board
{
    /// <summary>
    /// Represents an ordered sequence of board locations to traverse.
    /// </summary>
    public sealed class BoardPath : IReadOnlyList<BoardLocation>
    {
        private readonly List<BoardLocation> _steps;

        public BoardPath(IEnumerable<BoardLocation> steps)
        {
            if (steps is null)
            {
                throw new ArgumentNullException(nameof(steps));
            }

            _steps = steps.ToList();
        }

        public int Count => _steps.Count;

        public bool IsEmpty => _steps.Count == 0;

        public BoardLocation this[int index] => _steps[index];

        public IEnumerator<BoardLocation> GetEnumerator() => _steps.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
