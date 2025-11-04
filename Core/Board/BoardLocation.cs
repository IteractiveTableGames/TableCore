using System;
using Godot;

namespace TableCore.Core.Board
{
    /// <summary>
    /// Represents a logical location on the board identified by a marker path and optional offset.
    /// </summary>
    public readonly struct BoardLocation : IEquatable<BoardLocation>
    {
        public BoardLocation(string markerPath, Vector2? offset = null)
        {
            MarkerPath = markerPath ?? string.Empty;
            Offset = offset ?? Vector2.Zero;
        }

        /// <summary>
        /// Relative path (from the board root) to a Node2D that marks this location.
        /// </summary>
        public string MarkerPath { get; }

        /// <summary>
        /// Additional offset applied on top of the marker's position.
        /// </summary>
        public Vector2 Offset { get; }

        public bool Equals(BoardLocation other)
        {
            return string.Equals(MarkerPath, other.MarkerPath, StringComparison.OrdinalIgnoreCase) &&
                   Offset == other.Offset;
        }

        public override bool Equals(object? obj) => obj is BoardLocation other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(MarkerPath.ToLowerInvariant(), Offset);

        public static bool operator ==(BoardLocation left, BoardLocation right) => left.Equals(right);

        public static bool operator !=(BoardLocation left, BoardLocation right) => !left.Equals(right);
    }
}
