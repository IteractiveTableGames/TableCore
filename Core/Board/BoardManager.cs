using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace TableCore.Core.Board
{
    /// <summary>
    /// Default implementation that looks up marker nodes under a board root.
    /// </summary>
    public sealed class BoardManager : IBoardManager
    {
        private const float TokenSpacing = 32f;

        private readonly AnimationService _animationService;
        private readonly Func<Node2D, Vector2, Vector2> _toLocalSpace;
        private readonly Func<TokenController, Vector2> _getTokenPosition;
        private readonly Action<TokenController, Vector2> _setTokenPosition;
        private readonly Func<TokenController, Vector2, Vector2, double, Task> _animateMove;
        private readonly Dictionary<TokenController, BoardLocation> _tokenLocations = new();
        private readonly Dictionary<Guid, TokenController> _playerTokens = new();
        private readonly Dictionary<TokenController, Guid> _tokenOwners = new();
        private Node2D? _boardRoot;

        public BoardManager(
            AnimationService animationService,
            Func<Node2D, Vector2, Vector2>? toLocalSpace = null,
            Func<TokenController, Vector2>? getTokenPosition = null,
            Action<TokenController, Vector2>? setTokenPosition = null,
            Func<TokenController, Vector2, Vector2, double, Task>? animateMove = null)
        {
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
            _toLocalSpace = toLocalSpace ?? DefaultToLocalSpace;
            _getTokenPosition = getTokenPosition ?? (token => token.Position);
            _setTokenPosition = setTokenPosition ?? ((token, position) => token.Position = position);
            _animateMove = animateMove ?? ((token, from, to, duration) => _animationService.AnimateMove(token, from, to, duration));
        }

        public void SetBoardRoot(Node2D boardRoot)
        {
            _boardRoot = boardRoot ?? throw new ArgumentNullException(nameof(boardRoot));
        }

        public void PlaceToken(Guid playerId, TokenController token, BoardLocation location)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var previousLocation = _tokenLocations.TryGetValue(token, out var existingLocation)
                ? existingLocation
                : (BoardLocation?)null;

            _tokenLocations[token] = location;
            _playerTokens[playerId] = token;
            _tokenOwners[token] = playerId;

            if (previousLocation.HasValue && !SameLocation(previousLocation.Value, location))
            {
                ReflowLocation(previousLocation.Value);
            }

            ReflowLocation(location);
        }

        public async Task MoveToken(Guid playerId, TokenController token, BoardPath path)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (path.IsEmpty)
            {
                await token.BounceAsync();
                return;
            }

            var previousLocation = _tokenLocations.TryGetValue(token, out var storedLocation)
                ? storedLocation
                : (BoardLocation?)null;

            foreach (var step in path)
            {
                var current = _getTokenPosition(token);
                var to = _toLocalSpace(token, GetWorldPosition(step));
                await _animateMove(token, current, to, 250);
                _setTokenPosition(token, to);
            }

            var finalLocation = path[path.Count - 1];
            _tokenLocations[token] = finalLocation;
            _playerTokens[playerId] = token;
            _tokenOwners[token] = playerId;

            if (previousLocation.HasValue && !SameLocation(previousLocation.Value, finalLocation))
            {
                ReflowLocation(previousLocation.Value);
            }

            ReflowLocation(finalLocation);
        }

        public BoardLocation? GetLocation(TokenController token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return _tokenLocations.TryGetValue(token, out var location) ? location : (BoardLocation?)null;
        }

        public Vector2 GetWorldPosition(BoardLocation location)
        {
            if (_boardRoot == null)
            {
                return location.Offset;
            }

            if (string.IsNullOrWhiteSpace(location.MarkerPath))
            {
                return _boardRoot.ToGlobal(location.Offset);
            }

            if (_boardRoot.GetNodeOrNull<Node2D>(location.MarkerPath) is { } marker)
            {
                return marker.GlobalPosition + location.Offset;
            }

            GD.PushWarning($"Board marker not found: {location.MarkerPath}");
            return _boardRoot.ToGlobal(location.Offset);
        }

        private void ReflowLocation(BoardLocation referenceLocation)
        {
            var tokens = new List<TokenController>();

            foreach (var entry in _tokenLocations)
            {
                if (SameLocation(entry.Value, referenceLocation))
                {
                    tokens.Add(entry.Key);
                }
            }

            if (tokens.Count == 0)
            {
                return;
            }

            tokens.Sort(CompareTokensByOwner);
            var offsets = ComputeOffsets(tokens);

            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                var baseLocation = _tokenLocations[token];
                var worldPosition = GetWorldPosition(baseLocation);
                var localPosition = _toLocalSpace(token, worldPosition);
                _setTokenPosition(token, localPosition + offsets[index]);
            }
        }

        private int CompareTokensByOwner(TokenController left, TokenController right)
        {
            var leftOwner = _tokenOwners.TryGetValue(left, out var leftId) ? leftId : Guid.Empty;
            var rightOwner = _tokenOwners.TryGetValue(right, out var rightId) ? rightId : Guid.Empty;
            return leftOwner.CompareTo(rightOwner);
        }

        private static bool SameLocation(BoardLocation first, BoardLocation second)
        {
            return string.Equals(first.MarkerPath, second.MarkerPath, StringComparison.OrdinalIgnoreCase) &&
                   first.Offset == second.Offset;
        }

        private static Vector2[] ComputeOffsets(IReadOnlyList<TokenController> tokens)
        {
            var count = tokens.Count;

            if (count <= 1)
            {
                return new[] { Vector2.Zero };
            }

            var spacing = TokenSpacing;
            var maxDiameter = 0f;

            foreach (var token in tokens)
            {
                if (token == null)
                {
                    continue;
                }

                var diameter = token.GetFootprintRadius() * 2f;
                if (diameter > maxDiameter)
                {
                    maxDiameter = diameter;
                }
            }

            if (maxDiameter > 0f)
            {
                spacing = Math.Max(TokenSpacing, maxDiameter + 4f);
            }

            var columns = Mathf.CeilToInt(Mathf.Sqrt(count));
            var rows = Mathf.CeilToInt(count / (float)columns);
            var totalWidth = (columns - 1) * spacing;
            var totalHeight = (rows - 1) * spacing;
            var offsets = new Vector2[count];

            for (var index = 0; index < count; index++)
            {
                var row = index / columns;
                var column = index % columns;
                offsets[index] = new Vector2(
                    column * spacing - totalWidth / 2f,
                    row * spacing - totalHeight / 2f);
            }

            return offsets;
        }

        private static Vector2 DefaultToLocalSpace(Node2D node, Vector2 worldPosition)
        {
            if (node.GetParent() is Node2D parent)
            {
                return parent.ToLocal(worldPosition);
            }

            return worldPosition;
        }
    }
}
