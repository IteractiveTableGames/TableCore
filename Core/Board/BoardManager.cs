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
        private readonly AnimationService _animationService;
        private readonly Dictionary<TokenController, BoardLocation> _tokenLocations = new();
        private readonly Dictionary<Guid, TokenController> _playerTokens = new();
        private Node2D? _boardRoot;

        public BoardManager(AnimationService animationService)
        {
            _animationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
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

            var position = GetWorldPosition(location);
            token.Position = position;

            _tokenLocations[token] = location;
            _playerTokens[playerId] = token;
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

            foreach (var step in path)
            {
                var from = token.Position;
                var to = GetWorldPosition(step);
                await _animationService.AnimateMove(token, from, to, 250);
                token.Position = to;
            }

            var finalLocation = path[path.Count - 1];
            _tokenLocations[token] = finalLocation;
            _playerTokens[playerId] = token;
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
    }
}
