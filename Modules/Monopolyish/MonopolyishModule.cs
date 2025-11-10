using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using TableCore.Core;
using TableCore.Core.Board;
using TableCore.Core.Modules;
using TableCore.Core.Modules.Monopolyish;

namespace TableCore.Modules.Monopolyish
{
    /// <summary>
    /// Root node for the Monopoly-ish sample module. Renders a simple placeholder board and
    /// creates player tokens so runtime validation has immediate visual feedback.
    /// </summary>
    public partial class MonopolyishModule : Node2D, IGameModule
    {
        private const string DefaultBoardPath = "BoardRoot";
        private const string DefaultTokenRootPath = "BoardRoot/Tokens";

        private SessionState? _session;
        private IModuleServices? _services;
        private MonopolyishBoard? _board;
        private Node2D? _tokensRoot;
        private readonly Dictionary<Guid, MonopolyishTokenVisual> _tokens = new();
        private readonly Dictionary<Guid, int> _tokenTileIndices = new();
        private MonopolyHudController? _hudController;

        [Export]
        public NodePath BoardPath { get; set; } = new NodePath(DefaultBoardPath);

        [Export]
        public NodePath TokensRootPath { get; set; } = new NodePath(DefaultTokenRootPath);

        public override void _Ready()
        {
            EnsureNodeReferences();
            CenterBoard();
        }

        public void Initialize(SessionState session, IModuleServices services)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _services = services ?? throw new ArgumentNullException(nameof(services));

            EnsureNodeReferences();
            CenterBoard();
            InitializeBoardManager();
            InitializeTokens();
            InitializeHud();

            GD.Print("Monopoly-ish module initialized for ", _session.PlayerProfiles.Count, " players.");
        }

        public void Tick(double delta)
        {
            // Future updates will advance animations, timers, and AI logic here.
        }

        public void Shutdown()
        {
            foreach (var token in _tokens.Values)
            {
                token.QueueFree();
            }

            if (_hudController != null)
            {
                _hudController.RollDiceRequested -= HandleRollDiceRequested;
                _hudController.EndTurnRequested -= HandleEndTurnRequested;
                _hudController.Dispose();
                _hudController = null;
            }

            _tokens.Clear();
            _tokenTileIndices.Clear();
            _session = null;
            _services = null;
        }

        private void EnsureNodeReferences()
        {
            var boardPath = BoardPath.IsEmpty ? DefaultBoardPath : BoardPath.ToString();
            _board ??= GetNodeOrNull<MonopolyishBoard>(boardPath);
            _board?.RefreshTileMetadata();

            if (_board != null && (_tokensRoot == null || !_tokensRoot.IsInsideTree()))
            {
                var tokenPath = TokensRootPath.IsEmpty ? DefaultTokenRootPath : TokensRootPath.ToString();
                if (tokenPath.StartsWith(boardPath, StringComparison.OrdinalIgnoreCase))
                {
                    var relative = tokenPath.Substring(boardPath.Length).TrimStart('/');
                    _tokensRoot = _board.GetNodeOrNull<Node2D>(relative);
                }
                else
                {
                    _tokensRoot = _board.GetNodeOrNull<Node2D>(tokenPath);
                }
            }

            if (_board != null && _tokensRoot == null)
            {
                _tokensRoot = new Node2D { Name = "Tokens" };
                _board.AddChild(_tokensRoot);
            }
        }

        private void InitializeTokens()
        {
            if (_session == null || _board == null || _tokensRoot == null)
            {
                return;
            }

            foreach (Node child in _tokensRoot.GetChildren())
            {
                child.QueueFree();
            }

            _tokens.Clear();
            _tokenTileIndices.Clear();

            var players = _session.PlayerProfiles;
            const int startingTileIndex = 0;
            var startMarkerPath = _board.GetMarkerPath(startingTileIndex);
            var startTileCenter = _board.GetTileCenter(startingTileIndex);

            for (var index = 0; index < players.Count; index++)
            {
                var profile = players[index];
                if (profile == null || profile.PlayerId == Guid.Empty)
                {
                    continue;
                }

                var tokenColor = profile.DisplayColor ?? new Color(0.85f, 0.85f, 0.85f);
                var token = new MonopolyishTokenVisual
                {
                    Name = $"Token_{profile.PlayerId}",
                    TokenColor = tokenColor,
                    Position = startTileCenter,
                    OwnerPlayerId = profile.PlayerId,
                    TokenName = profile.DisplayName ?? "Player"
                };

                if (_services != null)
                {
                    token.AnimationService = _services.GetAnimationService();
                }

                _tokensRoot.AddChild(token);
                _tokens[profile.PlayerId] = token;
                _tokenTileIndices[profile.PlayerId] = startingTileIndex;

                _services?.GetBoardManager().PlaceToken(profile.PlayerId, token, new BoardLocation(startMarkerPath));
            }
        }

        private void CenterBoard()
        {
            if (_board == null)
            {
                return;
            }

            var viewportSize = GetViewportRect().Size;
            _board.Position = viewportSize / 2f;

            if (_tokensRoot != null)
            {
                _tokensRoot.Position = Vector2.Zero;
            }
        }

        private void InitializeHud()
        {
            if (_session == null || _services == null)
            {
                return;
            }

            var players = _session.PlayerProfiles.Where(p => p != null && p.PlayerId != Guid.Empty).ToList();
            if (players.Count == 0)
            {
                return;
            }

            var turnManager = _services.GetTurnManager();
            if (turnManager.TurnOrder.Count == 0)
            {
                turnManager.SetOrder(players.Select(p => p.PlayerId));
            }

            var bank = _services.GetBank();
            foreach (var player in players)
            {
                bank.SetBalance(player.PlayerId, 1500);
            }

            var cardService = _services.GetCardService();
            foreach (var player in players)
            {
                cardService.GetHand(player.PlayerId);
            }

            var hudService = _services.GetHUDService();
            var diceService = _services.GetDiceService();

            _hudController = new MonopolyHudController(_session, hudService, bank, cardService, turnManager, diceService);
            _hudController.RollDiceRequested += HandleRollDiceRequested;
            _hudController.EndTurnRequested += HandleEndTurnRequested;
            _hudController.Initialize();
        }

        private void HandleRollDiceRequested(Guid playerId, IReadOnlyList<int> dice)
        {
            var total = dice.Sum();
            _ = MoveTokenAsync(playerId, total);
        }

        private void HandleEndTurnRequested(Guid playerId)
        {
            var turnManager = _services?.GetTurnManager();
            turnManager?.AdvanceTurn();
        }

        private async Task MoveTokenAsync(Guid playerId, int stepCount)
        {
            if (_board == null || !_tokens.TryGetValue(playerId, out var token) || _services == null)
            {
                return;
            }

            var totalSteps = Math.Max(0, stepCount);
            if (totalSteps == 0)
            {
                await token.BounceAsync();
                return;
            }

            var startIndex = _tokenTileIndices.TryGetValue(playerId, out var index) ? index : 0;
            var steps = new List<BoardLocation>(totalSteps);

            for (var i = 1; i <= totalSteps; i++)
            {
                steps.Add(new BoardLocation(_board.GetMarkerPath(startIndex + i)));
            }

            try
            {
                await _services.GetBoardManager().MoveToken(playerId, token, new BoardPath(steps));
                _tokenTileIndices[playerId] = startIndex + totalSteps;
                await token.HighlightAsync(new Color(1f, 0.95f, 0.5f, 1f));
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Token animation failed: {ex.Message}");
            }
        }

        private void InitializeBoardManager()
        {
            if (_board == null || _services == null)
            {
                return;
            }

            _services.GetBoardManager().SetBoardRoot(_board);
        }
    }
}
