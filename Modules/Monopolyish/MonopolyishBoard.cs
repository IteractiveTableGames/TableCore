using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TableCore.Modules.Monopolyish
{
    /// <summary>
    /// Stores positional metadata for the Monopoly-ish board using editor-placed markers.
    /// </summary>
    public partial class MonopolyishBoard : Node2D
    {
        [Export]
        private NodePath[] _tileMarkerPaths = Array.Empty<NodePath>();

        private readonly List<Vector2> _tileCenters = new();
        private readonly List<string> _normalizedPaths = new();

        public IReadOnlyList<Vector2> TileCenters => _tileCenters;

        public override void _Ready()
        {
            _tileCenters.Clear();
            _normalizedPaths.Clear();

            foreach (var path in _tileMarkerPaths ?? Array.Empty<NodePath>())
            {
                if (path.IsEmpty)
                {
                    continue;
                }

                if (GetNodeOrNull<Node2D>(path) is { } marker)
                {
                    _tileCenters.Add(marker.Position);
                    _normalizedPaths.Add(path.ToString());
                }
            }
        }

        public Vector2 GetTileCenter(int tileIndex)
        {
            if (_tileCenters.Count == 0)
            {
                return Vector2.Zero;
            }

            var wrapped = Math.Abs(tileIndex) % _tileCenters.Count;
            return _tileCenters[wrapped];
        }

        public string GetMarkerPath(int tileIndex)
        {
            if (_normalizedPaths.Count == 0)
            {
                return string.Empty;
            }

            var wrapped = Math.Abs(tileIndex) % _normalizedPaths.Count;
            return _normalizedPaths[wrapped];
        }
    }
}
