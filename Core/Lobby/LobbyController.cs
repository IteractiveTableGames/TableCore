using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using TableCore.Core;
using TableCore.Input;

namespace TableCore.Lobby
{
    /// <summary>
    /// Controls the lobby scene where players can claim seats by touching near an edge of the table.
    /// </summary>
    public partial class LobbyController : Control
    {
        private const long MouseTouchKey = -1;

        [Export(PropertyHint.Range, "0.2,3.0,0.1")]
        public float HoldDurationSeconds { get; set; } = 1.0f;

        [Export(PropertyHint.Range, "20,400,5")]
        public float EdgeJoinMargin { get; set; } = 120.0f;

        [Export(PropertyHint.Range, "120,540,10")]
        public float HudStripThickness { get; set; } = 320.0f;

        [Export(PropertyHint.Range, "200,1200,10")]
        public float HudStripLength { get; set; } = 520.0f;

        [Export(PropertyHint.Range, "2,60,1")]
        public float SeatIndicatorThickness { get; set; } = 12.0f;

        [Export(PropertyHint.Range, "0.05,0.5,0.05")]
        public float MaxSeatShiftFraction { get; set; } = 0.35f;

        [Export]
        public string JoinPromptText { get; set; } = "Touch and hold near an edge to join the table";

        private readonly Dictionary<long, TouchTracker> _activeTouches = new();
        private readonly SessionState _sessionState = new();
        private readonly Dictionary<Guid, ColorRect> _seatIndicators = new();
        private readonly Color[] _playerPalette =
        {
            Color.FromHtml("F94144"),
            Color.FromHtml("F3722C"),
            Color.FromHtml("F9C74F"),
            Color.FromHtml("90BE6D"),
            Color.FromHtml("577590")
        };

        private RichTextLabel? _playerListLabel;
        private Label? _promptLabel;
        private Label? _statusLabel;
        private InputRouter? _inputRouter;
        private Control? _seatOverlayRoot;

        public SessionState Session => _sessionState;

        public override void _Ready()
        {
            SetProcess(true);
            SetProcessInput(true);

            _playerListLabel = GetNodeOrNull<RichTextLabel>("PlayerPanel/PlayerList");
            _promptLabel = GetNodeOrNull<Label>("Prompts/JoinPrompt");
            _statusLabel = GetNodeOrNull<Label>("Prompts/StatusMessage");
            _inputRouter = GetNodeOrNull<InputRouter>("InputRouter");
            _seatOverlayRoot = GetNodeOrNull<Control>("SeatOverlays");

            if (_promptLabel != null)
            {
                _promptLabel.Text = JoinPromptText;
            }

            RefreshPlayerDisplay();
            UpdateInputRouterSession();
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);

            switch (@event)
            {
                case InputEventScreenTouch screenTouch:
                    HandleScreenTouch(screenTouch);
                    break;
                case InputEventScreenDrag screenDrag:
                    HandleScreenDrag(screenDrag);
                    break;
                case InputEventMouseButton mouseButton:
                    HandleMouseButton(mouseButton);
                    break;
                case InputEventMouseMotion mouseMotion:
                    UpdateMousePosition(mouseMotion);
                    break;
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);

            if (_activeTouches.Count == 0)
            {
                return;
            }

            var completedTouches = new List<long>();

            foreach (var (key, tracker) in _activeTouches)
            {
                tracker.HoldTime += (float)delta;

                if (tracker.HoldTime < HoldDurationSeconds)
                {
                    continue;
                }

                TryClaimSeat(tracker.Position);
                completedTouches.Add(key);
            }

            foreach (var identifier in completedTouches)
            {
                _activeTouches.Remove(identifier);
            }
        }

        private void HandleScreenTouch(InputEventScreenTouch screenTouch)
        {
            if (screenTouch.Pressed)
            {
                _activeTouches[screenTouch.Index] = new TouchTracker(screenTouch.Position);
                return;
            }

            _activeTouches.Remove(screenTouch.Index);
        }

        private void HandleScreenDrag(InputEventScreenDrag screenDrag)
        {
            if (_activeTouches.TryGetValue(screenDrag.Index, out var tracker))
            {
                tracker.Position = screenDrag.Position;
            }
        }

        private void HandleMouseButton(InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex != MouseButton.Left)
            {
                return;
            }

            if (mouseButton.Pressed)
            {
                _activeTouches[MouseTouchKey] = new TouchTracker(mouseButton.Position);
                return;
            }

            _activeTouches.Remove(MouseTouchKey);
        }

        private void UpdateMousePosition(InputEventMouseMotion mouseMotion)
        {
            if (_activeTouches.TryGetValue(MouseTouchKey, out var tracker))
            {
                tracker.Position = mouseMotion.Position;
            }
        }

        private bool TryClaimSeat(Vector2 anchorPoint)
        {
            var viewportRect = GetViewport().GetVisibleRect();
            var distance = LobbySeatPlanner.GetDistanceToNearestEdge(anchorPoint, viewportRect);

            if (distance > EdgeJoinMargin)
            {
                UpdateStatusMessage("Move closer to any edge to claim a seat.");
                return false;
            }

            var edge = LobbySeatPlanner.GetNearestEdge(anchorPoint, viewportRect);
            var initialSeatZone = LobbySeatPlanner.CreateSeatZone(edge, viewportRect, HudStripThickness, HudStripLength, anchorPoint);
            var seatItems = BuildSeatAssignmentItems(edge, initialSeatZone);
            var desiredCenters = seatItems.Select(item => item.DesiredCenter).ToList();

            if (!LobbySeatPlanner.TryArrangeSeatCenters(edge, viewportRect, HudStripLength, MaxSeatShiftFraction, desiredCenters, out var arrangedCenters))
            {
                UpdateStatusMessage("Not enough space on this edge.");
                return false;
            }

            PlayerProfile? newProfile = null;

            for (var index = 0; index < seatItems.Count; index++)
            {
                var arrangedZone = LobbySeatPlanner.CreateSeatZoneFromAxisCenter(
                    edge,
                    viewportRect,
                    HudStripThickness,
                    HudStripLength,
                    arrangedCenters[index]);

                var item = seatItems[index];

                if (item.Player != null)
                {
                    item.Player.Seat = arrangedZone;
                    UpdateSeatIndicator(item.Player);
                }
                else
                {
                    newProfile = CreatePlayerProfile(arrangedZone);
                }
            }

            if (newProfile == null)
            {
                UpdateStatusMessage("Unable to register seat.");
                return false;
            }

            _sessionState.PlayerProfiles.Add(newProfile);
            CreateSeatIndicator(newProfile);

            RefreshPlayerDisplay();
            UpdateInputRouterSession();
            UpdateStatusMessage($"Seat claimed near the {edge.ToString().ToLower()} edge.");

            return true;
        }

        private PlayerProfile CreatePlayerProfile(SeatZone seatZone)
        {
            var playerIndex = _sessionState.PlayerProfiles.Count;
            var color = _playerPalette[playerIndex % _playerPalette.Length];

            return new PlayerProfile
            {
                PlayerId = Guid.NewGuid(),
                DisplayName = $"Player {playerIndex + 1}",
                DisplayColor = color,
                Seat = seatZone,
                IsGameMaster = false
            };
        }

        private void RefreshPlayerDisplay()
        {
            if (_playerListLabel == null)
            {
                return;
            }

            if (_sessionState.PlayerProfiles.Count == 0)
            {
                _playerListLabel.Text = "No players joined yet.";
                return;
            }

            var builder = new StringBuilder();
            builder.AppendLine("Players:");

            for (var index = 0; index < _sessionState.PlayerProfiles.Count; index++)
            {
                var profile = _sessionState.PlayerProfiles[index];
                var edgeName = profile.Seat?.Edge.ToString() ?? "Unknown";
                var anchor = profile.Seat?.AnchorPoint ?? Vector2.Zero;
                var region = profile.Seat?.ScreenRegion ?? new Rect2();
                builder.AppendLine(
                    $"{index + 1}. {profile.DisplayName} ({edgeName}) @ ({anchor.X:0}, {anchor.Y:0}) " +
                    $"region=({region.Position.X:0}, {region.Position.Y:0}) size=({region.Size.X:0}, {region.Size.Y:0})");
            }

            _playerListLabel.Text = builder.ToString();
        }

        private void UpdateStatusMessage(string message)
        {
            if (_statusLabel != null)
            {
                _statusLabel.Text = message;
            }
        }

        private void UpdateInputRouterSession()
        {
            if (_inputRouter != null)
            {
                _inputRouter.Session = _sessionState;
            }
        }

        private List<SeatAssignmentItem> BuildSeatAssignmentItems(TableEdge edge, SeatZone newSeatZone)
        {
            var items = new List<SeatAssignmentItem>();

            foreach (var profile in GetPlayersOnEdge(edge))
            {
                if (profile.Seat == null)
                {
                    continue;
                }

                items.Add(new SeatAssignmentItem
                {
                    Player = profile,
                    DesiredCenter = GetAxisCenter(profile.Seat, edge)
                });
            }

            items.Add(new SeatAssignmentItem
            {
                Player = null,
                DesiredCenter = GetAxisCenter(newSeatZone, edge)
            });

            return items;
        }

        private IEnumerable<PlayerProfile> GetPlayersOnEdge(TableEdge edge)
        {
            return _sessionState.PlayerProfiles.Where(profile => profile.Seat?.Edge == edge);
        }

        private void CreateSeatIndicator(PlayerProfile profile)
        {
            if (_seatOverlayRoot == null || profile.Seat == null)
            {
                return;
            }

            if (_seatIndicators.ContainsKey(profile.PlayerId))
            {
                UpdateSeatIndicator(profile);
                return;
            }

            var indicatorRect = ComputeIndicatorRect(profile.Seat);
            var indicator = new ColorRect
            {
                MouseFilter = MouseFilterEnum.Ignore,
                Position = indicatorRect.Position,
                Size = indicatorRect.Size,
                Color = GetIndicatorColor(profile)
            };

            _seatOverlayRoot.AddChild(indicator);
            _seatIndicators[profile.PlayerId] = indicator;
        }

        private void UpdateSeatIndicator(PlayerProfile profile)
        {
            if (profile.Seat == null)
            {
                return;
            }

            if (!_seatIndicators.TryGetValue(profile.PlayerId, out var indicator))
            {
                CreateSeatIndicator(profile);
                return;
            }

            var rect = ComputeIndicatorRect(profile.Seat);
            indicator.Position = rect.Position;
            indicator.Size = rect.Size;
            indicator.Color = GetIndicatorColor(profile);
        }

        private Color GetIndicatorColor(PlayerProfile profile)
        {
            var baseColor = profile.DisplayColor ?? new Color(0.8f, 0.8f, 0.8f);
            baseColor.A = 0.7f;
            return baseColor;
        }

        private Rect2 ComputeIndicatorRect(SeatZone seatZone)
        {
            var maxThickness = seatZone.Edge is TableEdge.Top or TableEdge.Bottom
                ? Mathf.Max(1f, seatZone.ScreenRegion.Size.Y)
                : Mathf.Max(1f, seatZone.ScreenRegion.Size.X);
            var thickness = Mathf.Clamp(SeatIndicatorThickness, 1f, maxThickness);

            return seatZone.Edge switch
            {
                TableEdge.Bottom => new Rect2(
                    seatZone.ScreenRegion.Position.X,
                    seatZone.ScreenRegion.Position.Y + seatZone.ScreenRegion.Size.Y - thickness,
                    seatZone.ScreenRegion.Size.X,
                    thickness),
                TableEdge.Top => new Rect2(
                    seatZone.ScreenRegion.Position.X,
                    seatZone.ScreenRegion.Position.Y,
                    seatZone.ScreenRegion.Size.X,
                    thickness),
                TableEdge.Left => new Rect2(
                    seatZone.ScreenRegion.Position.X,
                    seatZone.ScreenRegion.Position.Y,
                    thickness,
                    seatZone.ScreenRegion.Size.Y),
                TableEdge.Right => new Rect2(
                    seatZone.ScreenRegion.Position.X + seatZone.ScreenRegion.Size.X - thickness,
                    seatZone.ScreenRegion.Position.Y,
                    thickness,
                    seatZone.ScreenRegion.Size.Y),
                _ => new Rect2()
            };
        }

        private static float GetAxisCenter(SeatZone seatZone, TableEdge edge)
        {
            return edge switch
            {
                TableEdge.Bottom or TableEdge.Top => seatZone.ScreenRegion.Position.X + (seatZone.ScreenRegion.Size.X / 2f),
                TableEdge.Left or TableEdge.Right => seatZone.ScreenRegion.Position.Y + (seatZone.ScreenRegion.Size.Y / 2f),
                _ => 0f
            };
        }

        private sealed class TouchTracker
        {
            public TouchTracker(Vector2 position)
            {
                Position = position;
                HoldTime = 0f;
            }

            public Vector2 Position { get; set; }
            public float HoldTime { get; set; }
        }

        private sealed class SeatAssignmentItem
        {
            public PlayerProfile? Player { get; init; }
            public float DesiredCenter { get; init; }
        }
    }
}
