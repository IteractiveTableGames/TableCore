using System;
using System.Collections.Generic;
using Godot;
using TableCore.Core;

namespace TableCore.Core.UI
{
    /// <summary>
    /// Provides per-player HUD containers and convenience helpers for updating standard HUD elements.
    /// </summary>
    public class HUDService : IHUDService
    {
        private readonly CanvasLayer _hudLayer;
        private readonly SessionState _sessionState;
        private readonly Dictionary<Guid, PlayerHud> _hudsByPlayer = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="HUDService"/> class.
        /// </summary>
        /// <param name="hudLayer">The canvas layer where HUDs will be added.</param>
        /// <param name="sessionState">The active session state.</param>
        public HUDService(CanvasLayer hudLayer, SessionState sessionState)
        {
            _hudLayer = hudLayer ?? throw new ArgumentNullException(nameof(hudLayer));
            _sessionState = sessionState ?? throw new ArgumentNullException(nameof(sessionState));
        }

        /// <inheritdoc/>
        public IPlayerHUD CreatePlayerHUD(PlayerProfile player)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (player.PlayerId == Guid.Empty)
            {
                throw new ArgumentException("PlayerId must be a non-empty GUID.", nameof(player));
            }

            if (_hudsByPlayer.TryGetValue(player.PlayerId, out var existing))
            {
                return existing;
            }

            if (player.Seat is null)
            {
                throw new InvalidOperationException("Player seat information is required to create a HUD.");
            }

            var seat = player.Seat;
            var layout = ComputeLayout(seat);
            var wrapper = CreateWrapperControl(player, layout);
            var hudRoot = CreateRotatedRootControl(player, layout);
            wrapper.AddChild(hudRoot);

            var hudPanel = BuildHudPanel(player);
            hudRoot.AddChild(hudPanel.Root);

            _hudLayer.AddChild(wrapper);

            var hud = new PlayerHud(player.PlayerId, hudRoot, hudPanel);
            _hudsByPlayer[player.PlayerId] = hud;
            return hud;
        }

        /// <inheritdoc/>
        public void UpdateFunds(Guid playerId, int newAmount)
        {
            if (_hudsByPlayer.TryGetValue(playerId, out var hud))
            {
                hud.UpdateFunds(newAmount);
            }
        }

        /// <inheritdoc/>
        public void UpdateHand(Guid playerId, IReadOnlyList<CardData> cards)
        {
            if (_hudsByPlayer.TryGetValue(playerId, out var hud))
            {
                hud.UpdateHand(cards);
            }
        }

        /// <inheritdoc/>
        public void SetPrompt(Guid playerId, string message)
        {
            if (_hudsByPlayer.TryGetValue(playerId, out var hud))
            {
                hud.SetPrompt(message);
            }
        }

        internal static HudLayout ComputeLayout(SeatZone seat)
        {
            var rect = seat.ScreenRegion;
            var center = rect.Size / 2f;

            return new HudLayout(
                wrapperPosition: rect.Position,
                wrapperSize: rect.Size,
                rootPosition: center,
                rootPivot: center,
                rotationDegrees: seat.RotationDegrees);
        }

        private static Control CreateWrapperControl(PlayerProfile player, HudLayout layout)
        {
            var wrapper = new Control
            {
                Name = $"HUDWrapper_{player.PlayerId}",
                Position = layout.WrapperPosition,
                Size = layout.WrapperSize,
                CustomMinimumSize = layout.WrapperSize,
                MouseFilter = Control.MouseFilterEnum.Pass
            };

            return wrapper;
        }

        private static Control CreateRotatedRootControl(PlayerProfile player, HudLayout layout)
        {
            var root = new Control
            {
                Name = $"HUDContent_{player.PlayerId}",
                Position = layout.RootPosition,
                PivotOffset = layout.RootPivot,
                CustomMinimumSize = layout.WrapperSize,
                Size = layout.WrapperSize,
                RotationDegrees = layout.RotationDegrees,
                MouseFilter = Control.MouseFilterEnum.Pass,
                ZIndex = 1
            };

            return root;
        }

        private static HudPanel BuildHudPanel(PlayerProfile player)
        {
            var panel = new PanelContainer
            {
                Name = "HudPanel",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                MouseFilter = Control.MouseFilterEnum.Stop
            };

            var margin = new MarginContainer
            {
                Name = "HudMargin"
            };
            margin.AddThemeConstantOverride("margin_left", 16);
            margin.AddThemeConstantOverride("margin_right", 16);
            margin.AddThemeConstantOverride("margin_top", 16);
            margin.AddThemeConstantOverride("margin_bottom", 16);

            var stack = new VBoxContainer
            {
                Name = "HudStack",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };
            stack.AddThemeConstantOverride("separation", 8);

            var nameLabel = new Label
            {
                Name = "PlayerNameLabel",
                Text = player.DisplayName ?? "Player",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var fundsLabel = new Label
            {
                Name = "FundsLabel",
                Text = "Funds: 0",
                HorizontalAlignment = HorizontalAlignment.Left
            };

            var promptLabel = new Label
            {
                Name = "PromptLabel",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                HorizontalAlignment = HorizontalAlignment.Left,
                Text = string.Empty
            };

            var handLabel = new Label
            {
                Name = "HandLabel",
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                Text = "Hand: (empty)"
            };

            var customContent = new VBoxContainer
            {
                Name = "CustomContent",
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill
            };

            stack.AddChild(nameLabel);
            stack.AddChild(fundsLabel);
            stack.AddChild(promptLabel);
            stack.AddChild(handLabel);
            stack.AddChild(customContent);

            margin.AddChild(stack);
            panel.AddChild(margin);

            return new HudPanel(panel, fundsLabel, handLabel, promptLabel, customContent);
        }

        private sealed class HudPanel
        {
            public HudPanel(PanelContainer root, Label fundsLabel, Label handLabel, Label promptLabel, VBoxContainer customContent)
            {
                Root = root;
                FundsLabel = fundsLabel;
                HandLabel = handLabel;
                PromptLabel = promptLabel;
                CustomContent = customContent;
            }

            public PanelContainer Root { get; }
            public Label FundsLabel { get; }
            public Label HandLabel { get; }
            public Label PromptLabel { get; }
            public VBoxContainer CustomContent { get; }
        }

        private sealed class PlayerHud : IPlayerHUD
        {
            private readonly HudPanel _panel;

            public PlayerHud(Guid playerId, Control root, HudPanel panel)
            {
                PlayerId = playerId;
                Root = root;
                _panel = panel;
            }

            public Control Root { get; }

            public Guid PlayerId { get; }

            public Control GetRootControl() => Root;

            public void AddControl(Node controlNode)
            {
                if (controlNode is null)
                {
                    throw new ArgumentNullException(nameof(controlNode));
                }

                _panel.CustomContent.AddChild(controlNode);
            }

            public void UpdateFunds(int amount)
            {
                _panel.FundsLabel.Text = FormatFundsLabel(amount);
            }

            public void UpdateHand(IReadOnlyList<CardData> cards)
            {
                _panel.HandLabel.Text = FormatHandLabel(cards);
            }

            public void SetPrompt(string message)
            {
                _panel.PromptLabel.Text = message ?? string.Empty;
            }
        }

        internal static string FormatFundsLabel(int amount) => $"Funds: {amount}";

        internal static string FormatHandLabel(IReadOnlyList<CardData> cards)
        {
            if (cards is null || cards.Count == 0)
            {
                return "Hand: (empty)";
            }

            return $"Hand: {cards.Count} card(s)";
        }

        internal readonly struct HudLayout
        {
            public HudLayout(Vector2 wrapperPosition, Vector2 wrapperSize, Vector2 rootPosition, Vector2 rootPivot, float rotationDegrees)
            {
                WrapperPosition = wrapperPosition;
                WrapperSize = wrapperSize;
                RootPosition = rootPosition;
                RootPivot = rootPivot;
                RotationDegrees = rotationDegrees;
            }

            public Vector2 WrapperPosition { get; }
            public Vector2 WrapperSize { get; }
            public Vector2 RootPosition { get; }
            public Vector2 RootPivot { get; }
            public float RotationDegrees { get; }
        }
    }
}
