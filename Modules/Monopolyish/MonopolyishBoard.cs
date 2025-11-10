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
        private readonly List<string> _markerPaths = new();

        public IReadOnlyList<Vector2> TileCenters
        {
            get
            {
                EnsureTileMetadata();
                return _tileCenters;
            }
        }

        public override void _Ready()
        {
            RefreshTileMetadata();
        }

        public Vector2 GetTileCenter(int tileIndex)
        {
            EnsureTileMetadata();

            if (_tileCenters.Count == 0)
            {
                return Vector2.Zero;
            }

            var wrapped = Math.Abs(tileIndex) % _tileCenters.Count;
            return _tileCenters[wrapped];
        }

        public string GetMarkerPath(int tileIndex)
        {
            EnsureTileMetadata();

            if (_markerPaths.Count == 0)
            {
                return string.Empty;
            }

            var wrapped = Math.Abs(tileIndex) % _markerPaths.Count;
            return _markerPaths[wrapped];
        }

        public void RefreshTileMetadata()
        {
            _tileCenters.Clear();
            _markerPaths.Clear();

            var explicitPaths = _tileMarkerPaths?.Length > 0
                ? _tileMarkerPaths
                : BuildPathsFromMarkersRoot();

            foreach (var path in explicitPaths)
            {
                if (path.IsEmpty)
                {
                    continue;
                }

                if (GetNodeOrNull<Node2D>(path) is { } marker)
                {
                    _tileCenters.Add(marker.Position);
                    _markerPaths.Add(path.ToString());
                }
            }
        }

        private NodePath[] BuildPathsFromMarkersRoot()
        {
            var markersRoot = GetNodeOrNull<Node>("Markers");
            if (markersRoot == null)
            {
                return Array.Empty<NodePath>();
            }

            var paths = new List<NodePath>();
            foreach (var child in markersRoot.GetChildren())
            {
                if (child is Node node)
                {
                    paths.Add(new NodePath($"{markersRoot.Name}/{node.Name}"));
                }
            }

            return paths.ToArray();
        }

        private void EnsureTileMetadata()
        {
            if (_tileCenters.Count > 0 && _markerPaths.Count > 0)
            {
                return;
            }

            RefreshTileMetadata();
        }
    }
}
