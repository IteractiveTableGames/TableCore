using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace TableCore.Core.Board
{
    /// <summary>
    /// Default board manager that looks up marker nodes under the board root to position tokens.
    /// </summary>
    public sealed class BoardManager : IBoardManager
    {
        private readonly AnimationService _animationService;
        private Node2D? _boardRoot;
        private readonly Dictionary<TokenController, BoardLocation> _tokenLocations = new();
        private readonly Dictionary<Guid, TokenController> _playerTokens = new();

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
            token.OwnerPlayerId = playerId;
            token.AnimationService ??= _animationService;

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

            var positions = path.Select(GetWorldPosition).ToList();
            await token.PlayMovePath(positions);

            var lastLocation = path[path.Count - 1];
            _tokenLocations[token] = lastLocation;
            _playerTokens[playerId] = token;
        }

        public BoardLocation? GetLocation(TokenController token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            return _tokenLocations.TryGetValue(token, out var location) ? location : null;
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
            return _boardRoot.GlobalPosition + location.Offset;
        }
    }
}
