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
        private CurrencyBank? _bank;
        private CardService? _cardService;
        private TurnManager? _turnManager;
        private DiceService? _diceService;
        private IHUDService? _hudService;
        private MonopolyTurnEngine? _turnEngine;
        private IReadOnlyList<MonopolyTileDefinition> _tileDefinitions = Array.Empty<MonopolyTileDefinition>();
        private readonly Dictionary<int, MonopolyTileVisual> _tileVisuals = new();
        private readonly Dictionary<Guid, Color> _playerColors = new();
        private PackedScene? _tileTemplate;
        private float _maxTileX;
        private float _minTileX;
        private float _maxTileY;
        private float _minTileY;

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
            _bank = services.GetBank();
            _cardService = services.GetCardService();
            _turnManager = services.GetTurnManager();
            _diceService = services.GetDiceService();
            _hudService = services.GetHUDService();

            EnsureNodeReferences();
            CenterBoard();
            InitializeBoardManager();
            InitializeRuleEngine();
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
            _playerColors.Clear();
            _turnEngine = null;
            _tileDefinitions = Array.Empty<MonopolyTileDefinition>();
            foreach (var visual in _tileVisuals.Values)
            {
                if (IsInstanceValid(visual))
                {
                    visual.QueueFree();
                }
            }
            _tileVisuals.Clear();
            _playerColors.Clear();
            _tileTemplate = null;
            _bank = null;
            _cardService = null;
            _turnManager = null;
            _diceService = null;
            _hudService = null;
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
                _playerColors[profile.PlayerId] = tokenColor;
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
            if (_session == null || _bank == null || _cardService == null || _turnManager == null || _hudService == null)
            {
                return;
            }

            var players = _session.PlayerProfiles.Where(p => p != null && p.PlayerId != Guid.Empty).ToList();
            if (players.Count == 0)
            {
                return;
            }

            if (_turnManager.TurnOrder.Count == 0)
            {
                _turnManager.SetOrder(players.Select(p => p.PlayerId));
            }

            foreach (var player in players)
            {
                _bank.SetBalance(player.PlayerId, 1500);
                _cardService.GetHand(player.PlayerId);
            }

            var diceService = _diceService ?? _services?.GetDiceService();
            if (diceService == null)
            {
                return;
            }

            _hudController = new MonopolyHudController(_session, _hudService, _bank, _cardService, _turnManager, diceService);
            _hudController.RollDiceRequested += HandleRollDiceRequested;
            _hudController.EndTurnRequested += HandleEndTurnRequested;
            _hudController.Initialize();
        }

        private void HandleRollDiceRequested(Guid playerId, IReadOnlyList<int> dice)
        {
            if (_turnManager != null)
            {
                try
                {
                    if (_turnManager.CurrentPlayerId != playerId)
                    {
                        GD.PushWarning("Ignoring roll from non-active player.");
                        return;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Turn order not set; allow roll so gameplay can bootstrap.
                }
            }

            var total = dice.Sum();
            _ = MoveTokenAsync(playerId, total);
        }

        private void HandleEndTurnRequested(Guid playerId)
        {
            _turnManager?.AdvanceTurn();
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
                ApplyTileEffects(playerId, startIndex, totalSteps);
            }
            catch (Exception ex)
            {
                GD.PushWarning($"Token animation failed: {ex.Message}");
            }
        }

        private void ApplyTileEffects(Guid playerId, int startIndex, int steps)
        {
            if (_turnEngine == null)
            {
                return;
            }

            var outcome = _turnEngine.ResolveTurn(playerId, startIndex, steps);
            if (outcome.Events.Count == 0)
            {
                return;
            }

            PublishTurnOutcome(playerId, outcome);
        }

        private void PublishTurnOutcome(Guid playerId, MonopolyTurnOutcome outcome)
        {
            var playerName = ResolvePlayerName(playerId);
            var tileName = outcome.Tile?.DisplayName ?? "the board";
            var playerLines = new List<string>();
            var broadcastLines = new List<string>();

            foreach (var turnEvent in outcome.Events)
            {
                switch (turnEvent)
                {
                    case BonusCollectedEvent bonus:
                        playerLines.Add($"{bonus.Reason} Collected ${bonus.Amount}.");
                        broadcastLines.Add($"{playerName} received ${bonus.Amount}.");
                        break;
                    case PropertyPurchasedEvent purchase:
                        playerLines.Add($"Purchased {purchase.PropertyName} for ${purchase.Cost}.");
                        broadcastLines.Add($"{playerName} bought {purchase.PropertyName}.");
                        break;
                    case RentPaidEvent rent:
                        var ownerName = ResolvePlayerName(rent.OwnerId);
                        var rentText = rent.PaidInFull
                            ? $"Paid ${rent.Amount}"
                            : $"Paid ${rent.Amount} (all remaining funds)";
                        playerLines.Add($"{rentText} to {ownerName} for landing on {rent.PropertyName}.");
                        broadcastLines.Add($"{playerName} paid {ownerName} ${rent.Amount} in rent.");
                        break;
                    case TaxPaidEvent tax:
                        playerLines.Add($"Paid ${tax.Amount} in taxes.");
                        broadcastLines.Add($"{playerName} paid ${tax.Amount} in taxes.");
                        break;
                    case NoActionEvent message:
                        playerLines.Add(message.Message);
                        break;
                }
            }

            if (playerLines.Count == 0)
            {
                playerLines.Add($"Landed on {tileName}.");
            }

            var summaryText = string.Join(" ", playerLines);
            UpdateTileVisual(outcome.Tile);

            if (_session == null || _hudService == null)
            {
                return;
            }

            _hudService.SetPrompt(playerId, summaryText);

            if (broadcastLines.Count == 0)
            {
                broadcastLines.Add($"{playerName} landed on {tileName}.");
            }

            var broadcast = string.Join(" ", broadcastLines);
            foreach (var profile in _session.PlayerProfiles)
            {
                if (profile == null || profile.PlayerId == Guid.Empty || profile.PlayerId == playerId)
                {
                    continue;
                }

                _hudService.SetPrompt(profile.PlayerId, broadcast);
            }
        }

        private string ResolvePlayerName(Guid playerId)
        {
            if (_session == null || playerId == Guid.Empty)
            {
                return "Player";
            }

            foreach (var profile in _session.PlayerProfiles)
            {
                if (profile != null && profile.PlayerId == playerId)
                {
                    return string.IsNullOrWhiteSpace(profile.DisplayName) ? "Player" : profile.DisplayName!;
                }
            }

            return "Player";
        }

        private void BuildTileVisuals()
        {
            _tileVisuals.Clear();

            if (_board == null || _tileDefinitions.Count == 0)
            {
                return;
            }

            if (_tileTemplate == null)
            {
                _tileTemplate = ResourceLoader.Load<PackedScene>("res://Modules/Monopolyish/MonopolyTile.tscn");
            }

            if (_tileTemplate == null)
            {
                GD.PushError("Monopoly tile template could not be loaded.");
                return;
            }

            UpdateTileExtents();

            foreach (var tile in _tileDefinitions)
            {
                if (_board.GetTileVisual(tile.Index) is not Node2D host)
                {
                    continue;
                }

                if (_tileTemplate.Instantiate() is not MonopolyTileVisual visual)
                {
                    continue;
                }

                host.AddChild(visual);
                visual.Position = Vector2.Zero;
                visual.SetDefinition(tile);
                visual.RotationDegrees = ResolveTileRotation(_board.GetTileCenter(tile.Index));
                _tileVisuals[tile.Index] = visual;
                UpdateTileVisual(tile);
            }
        }

        private void UpdateTileVisual(MonopolyTileDefinition? tile)
        {
            if (tile == null || !_tileVisuals.TryGetValue(tile.Index, out var visual))
            {
                return;
            }

            Guid? ownerId = _turnEngine?.GetOwner(tile);
            var owned = ownerId.HasValue && ownerId.Value != Guid.Empty;
            var ownerName = owned ? ResolvePlayerName(ownerId!.Value) : "Unowned";
            var ownerColor = new Color(1f, 1f, 1f, 0.25f);

            if (owned && _playerColors.TryGetValue(ownerId!.Value, out var color))
            {
                ownerColor = color;
            }

            visual.UpdateOwner(ownerName, ownerColor, owned);
        }

        private void UpdateTileExtents()
        {
            if (_board == null)
            {
                return;
            }

            _minTileX = float.MaxValue;
            _maxTileX = float.MinValue;
            _minTileY = float.MaxValue;
            _maxTileY = float.MinValue;

            foreach (var tile in _tileDefinitions)
            {
                var center = _board.GetTileCenter(tile.Index);
                _minTileX = Mathf.Min(_minTileX, center.X);
                _maxTileX = Mathf.Max(_maxTileX, center.X);
                _minTileY = Mathf.Min(_minTileY, center.Y);
                _maxTileY = Mathf.Max(_maxTileY, center.Y);
            }
        }

        private float ResolveTileRotation(Vector2 center)
        {
            const float tolerance = 2f;

            if (Mathf.Abs(center.Y - _minTileY) <= tolerance)
            {
                return 180f;
            }

            if (Mathf.Abs(center.X - _maxTileX) <= tolerance)
            {
                return -90f;
            }

            if (Mathf.Abs(center.Y - _maxTileY) <= tolerance)
            {
                return 180f;
            }

            return 90f;
        }

        private void InitializeBoardManager()
        {
            if (_board == null || _services == null)
            {
                return;
            }

            _services.GetBoardManager().SetBoardRoot(_board);
        }

        private void InitializeRuleEngine()
        {
            if (_board == null || _bank == null || _cardService == null)
            {
                return;
            }

            var tileCount = Math.Max(_board.TileCenters.Count, 1);
            _tileDefinitions = MonopolyTileLibrary.CreateDefaultTrack(tileCount);
            _turnEngine = new MonopolyTurnEngine(_bank, _cardService, _tileDefinitions);
            BuildTileVisuals();
        }
    }
}
