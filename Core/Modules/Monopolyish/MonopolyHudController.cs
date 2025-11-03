using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace TableCore.Core.Modules.Monopolyish
{
    /// <summary>
    /// Orchestrates the per-player HUD experience for the Monopoly-ish sample module.
    /// </summary>
    public class MonopolyHudController : IDisposable
    {
        private readonly SessionState _sessionState;
        private readonly IHUDService _hudService;
        private readonly CurrencyBank _currencyBank;
        private readonly CardService _cardService;
        private readonly TurnManager _turnManager;
        private readonly DiceService _diceService;
        private readonly IMonopolyHudViewFactory _viewFactory;
        private readonly Dictionary<Guid, IMonopolyHudViewContext> _contexts = new();

        private bool _isInitialized;
        private Guid _activePlayerId = Guid.Empty;

        /// <summary>
        /// Raised when the active player taps the "Roll Dice" button.
        /// </summary>
        public event Action<Guid, IReadOnlyList<int>>? RollDiceRequested;

        /// <summary>
        /// Raised when the active player taps the "End Turn" button.
        /// </summary>
        public event Action<Guid>? EndTurnRequested;

        /// <summary>
        /// Initializes a new instance of the <see cref="MonopolyHudController"/> class.
        /// </summary>
        public MonopolyHudController(
            SessionState sessionState,
            IHUDService hudService,
            CurrencyBank currencyBank,
            CardService cardService,
            TurnManager turnManager,
            DiceService diceService,
            IMonopolyHudViewFactory? viewFactory = null)
        {
            _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
            _hudService = hudService ?? throw new ArgumentNullException(nameof(hudService));
            _currencyBank = currencyBank ?? throw new ArgumentNullException(nameof(currencyBank));
            _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));
            _turnManager = turnManager ?? throw new ArgumentNullException(nameof(turnManager));
            _diceService = diceService ?? throw new ArgumentNullException(nameof(diceService));
            _viewFactory = viewFactory ?? new GodotMonopolyHudViewFactory();
        }

        /// <summary>
        /// Sets up HUD panels for all players in the session and wires service callbacks.
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            foreach (var player in _sessionState.PlayerProfiles)
            {
                if (player is null || player.PlayerId == Guid.Empty)
                {
                    continue;
                }

                var playerHud = _hudService.CreatePlayerHUD(player);
                var context = _viewFactory.Create(player, playerHud, () => HandleRollDicePressed(player.PlayerId), () => HandleEndTurnPressed(player.PlayerId));
                _contexts[player.PlayerId] = context;

                var startingFunds = _currencyBank.GetBalance(player.PlayerId);
                _hudService.UpdateFunds(player.PlayerId, startingFunds);

                var hand = _cardService.GetHand(player.PlayerId);
                _hudService.UpdateHand(player.PlayerId, hand.Cards);

                _hudService.SetPrompt(player.PlayerId, "Waiting for your turn...");
            }

            _currencyBank.BalanceChanged += HandleBalanceChanged;
            _cardService.HandUpdated += HandleHandUpdated;
            _turnManager.TurnChanged += HandleTurnChanged;

            _activePlayerId = GetCurrentTurnPlayer();
            UpdatePrompts(_activePlayerId, turnStarted: false);

            _isInitialized = true;
        }

        /// <summary>
        /// Releases subscriptions to the various framework services.
        /// </summary>
        public void Dispose()
        {
            _currencyBank.BalanceChanged -= HandleBalanceChanged;
            _cardService.HandUpdated -= HandleHandUpdated;
            _turnManager.TurnChanged -= HandleTurnChanged;

            foreach (var context in _contexts.Values)
            {
                context.Dispose();
            }

            _contexts.Clear();
        }

        private void HandleBalanceChanged(Guid playerId, int newAmount)
        {
            if (!_contexts.ContainsKey(playerId))
            {
                return;
            }

            _hudService.UpdateFunds(playerId, newAmount);
        }

        private void HandleHandUpdated(Guid playerId, IReadOnlyList<CardData> cards)
        {
            if (!_contexts.ContainsKey(playerId))
            {
                return;
            }

            _hudService.UpdateHand(playerId, cards);
        }

        private void HandleTurnChanged(Guid activePlayerId)
        {
            _activePlayerId = activePlayerId;
            UpdatePrompts(activePlayerId, turnStarted: true);
        }

        private void HandleRollDicePressed(Guid playerId)
        {
            if (!IsActivePlayer(playerId))
            {
                return;
            }

            var results = _diceService.RollMultiple(2, 6);
            RollDiceRequested?.Invoke(playerId, results);

            var total = results.Sum();
            var resultText = $"You rolled {string.Join(" + ", results)} = {total}.";
            _hudService.SetPrompt(playerId, resultText);

            NotifyOthersOfRoll(playerId, total);
        }

        private void HandleEndTurnPressed(Guid playerId)
        {
            if (!IsActivePlayer(playerId))
            {
                return;
            }

            EndTurnRequested?.Invoke(playerId);
            _hudService.SetPrompt(playerId, "Turn complete. Handing play to the next player.");
        }

        private void UpdatePrompts(Guid activePlayerId, bool turnStarted)
        {
            string? activePlayerName = null;
            if (activePlayerId != Guid.Empty && _contexts.TryGetValue(activePlayerId, out var activeContext))
            {
                activePlayerName = activeContext.DisplayName;
            }

            foreach (var (playerId, context) in _contexts)
            {
                var isActive = playerId == activePlayerId && activePlayerId != Guid.Empty;
                context.SetInteractionEnabled(isActive);

                if (isActive)
                {
                    var prompt = turnStarted
                        ? "Your turn! Roll the dice when you're ready."
                        : "You're up first! Roll the dice to begin.";
                    _hudService.SetPrompt(playerId, prompt);
                }
                else if (activePlayerName is not null)
                {
                    var prompt = turnStarted
                        ? $"{activePlayerName} is taking their turn."
                        : $"Waiting for {activePlayerName} to begin.";
                    _hudService.SetPrompt(playerId, prompt);
                }
                else
                {
                    _hudService.SetPrompt(playerId, "Waiting for the game to start...");
                }
            }
        }

        private void NotifyOthersOfRoll(Guid rollerId, int total)
        {
            if (!_contexts.TryGetValue(rollerId, out var roller))
            {
                return;
            }

            var message = $"{roller.DisplayName} rolled {total}.";
            foreach (var (playerId, _) in _contexts)
            {
                if (playerId == rollerId)
                {
                    continue;
                }

                _hudService.SetPrompt(playerId, message);
            }
        }

        private bool IsActivePlayer(Guid playerId)
        {
            return playerId != Guid.Empty && playerId == _activePlayerId;
        }

        private Guid GetCurrentTurnPlayer()
        {
            try
            {
                return _turnManager.CurrentPlayerId;
            }
            catch (InvalidOperationException)
            {
                return Guid.Empty;
            }
        }

        private sealed class GodotMonopolyHudViewFactory : IMonopolyHudViewFactory
        {
            public IMonopolyHudViewContext Create(PlayerProfile player, IPlayerHUD hud, Action rollHandler, Action endHandler)
            {
                var content = new VBoxContainer
                {
                    Name = "MonopolyHudContent",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    ThemeTypeVariation = "MonopolyHudContent"
                };
                content.AddThemeConstantOverride("separation", 12);

                var actionRow = new HBoxContainer
                {
                    Name = "ActionRow",
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter
                };
                actionRow.AddThemeConstantOverride("separation", 12);

                var rollButton = CreateActionButton("Roll Dice");
                rollButton.Pressed += rollHandler;

                var endButton = CreateActionButton("End Turn");
                endButton.Pressed += endHandler;

                actionRow.AddChild(rollButton);
                actionRow.AddChild(endButton);
                content.AddChild(actionRow);

                hud.AddControl(content);

                return new GodotMonopolyHudViewContext(player, hud, rollButton, rollHandler, endButton, endHandler);
            }

            private static Button CreateActionButton(string text)
            {
                return new Button
                {
                    Text = text,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                    MouseFilter = Control.MouseFilterEnum.Stop,
                    Name = $"{text.Replace(" ", string.Empty)}Button"
                };
            }
        }

        private sealed class GodotMonopolyHudViewContext : IMonopolyHudViewContext
        {
            private readonly Action _rollHandler;
            private readonly Action _endHandler;

            public GodotMonopolyHudViewContext(
                PlayerProfile player,
                IPlayerHUD hud,
                Button rollButton,
                Action rollHandler,
                Button endButton,
                Action endHandler)
            {
                PlayerId = player.PlayerId;
                DisplayName = string.IsNullOrWhiteSpace(player.DisplayName) ? "Player" : player.DisplayName;
                Hud = hud;
                RollButton = rollButton;
                EndButton = endButton;
                _rollHandler = rollHandler;
                _endHandler = endHandler;
            }

            public Guid PlayerId { get; }
            public string DisplayName { get; }
            public IPlayerHUD Hud { get; }
            public Button RollButton { get; }
            public Button EndButton { get; }

            public void SetInteractionEnabled(bool enabled)
            {
                RollButton.Disabled = !enabled;
                EndButton.Disabled = !enabled;
            }

            public void InvokeRoll() => RollButton.EmitSignal(BaseButton.SignalName.Pressed);

            public void InvokeEndTurn() => EndButton.EmitSignal(BaseButton.SignalName.Pressed);

            public void Dispose()
            {
                RollButton.Pressed -= _rollHandler;
                EndButton.Pressed -= _endHandler;
            }
        }
    }

    public interface IMonopolyHudViewFactory
    {
        IMonopolyHudViewContext Create(PlayerProfile player, IPlayerHUD hud, Action rollHandler, Action endHandler);
    }

    public interface IMonopolyHudViewContext : IDisposable
    {
        Guid PlayerId { get; }
        string DisplayName { get; }
        void SetInteractionEnabled(bool enabled);
        void InvokeRoll();
        void InvokeEndTurn();
    }
}
