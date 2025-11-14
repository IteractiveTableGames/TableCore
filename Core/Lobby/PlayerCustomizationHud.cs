using System;
using System.Collections.Generic;
using Godot;
using TableCore.Core;
using TableCore.Core.UI;

namespace TableCore.Lobby
{
    /// <summary>
    /// Player personalization HUD rendered inside a seat zone.
    /// </summary>
    public partial class PlayerCustomizationHud : Control
    {
        private const float HorizontalMargin = 12f;
        private const float VerticalMargin = 12f;
        private const float BottomEdgePadding = 0f;
        private const float IndicatorClearance = 76f;
        private const float MaxWidth = 360f;
        private const float MaxHeight = 210f;
        private const float MinLengthRequirement = 260f;
        private const float MinThicknessRequirement = 180f;

        private PlayerProfile? _player;
        private PlayerCustomizationModel? _model;
        private SeatZone? _currentSeat;
        private SeatZone? _pendingSeat;

        private VBoxContainer _contentRoot = default!;
        private Label _nameLabel = default!;
        private OptionButton _colorDropdown = default!;
        private ColorRect _colorSwatch = default!;
        private OptionButton _avatarDropdown = default!;
        private FloatingKeyboard _keyboard = default!;
        private CenterContainer _keyboardContainer = default!;
        private CenterContainer _waitOverlay = default!;
        private TextureRect _waitSpinner = default!;
        private Label _waitLabel = default!;

        private bool _isWaiting;
        private bool _spin;
        private bool _suppressCallbacks;
        private bool _isClosing;

        public event Action<PlayerProfile>? ProfileChanged;
        public event Action<PlayerProfile>? CustomizationCompleted;
        public event Action<PlayerProfile>? CustomizationCancelled;

        public bool IsWaiting => _isWaiting;
        public SeatZone? CurrentSeat => _currentSeat;
        public bool VisibleForInput => Visible && !_isWaiting;

        public static bool CanDisplayInSeat(SeatZone seatZone)
        {
            var (length, thickness) = GetSeatDimensions(seatZone);
            return length >= MinLengthRequirement && thickness >= MinThicknessRequirement;
        }

        public void Initialize(PlayerProfile player, IEnumerable<Color> palette, IEnumerable<AvatarOption> avatars)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _model = new PlayerCustomizationModel(player, palette, avatars);
            _model.ProfileChanged += HandleProfileChanged;
        }

        public override void _Ready()
        {
            base._Ready();

            _contentRoot = GetNode<VBoxContainer>("Panel/Margin/Content");
            _nameLabel = GetNode<Label>("Panel/Margin/Content/NameLabel");
            _colorDropdown = GetNode<OptionButton>("Panel/Margin/Content/SelectionRow/ColorColumn/ColorRow/ColorDropdown");
            _colorSwatch = GetNode<ColorRect>("Panel/Margin/Content/SelectionRow/ColorColumn/ColorRow/ColorSwatch");
            _avatarDropdown = GetNode<OptionButton>("Panel/Margin/Content/SelectionRow/AvatarColumn/AvatarDropdown");
            _keyboardContainer = GetNode<CenterContainer>("Panel/Margin/Content/KeyboardContainer");
            _keyboard = GetNode<FloatingKeyboard>("Panel/Margin/Content/KeyboardContainer/Keyboard");
            _waitOverlay = GetNode<CenterContainer>("WaitOverlay");
            _waitSpinner = GetNode<TextureRect>("WaitOverlay/WaitPanel/WaitVBox/Spinner");
            _waitLabel = GetNode<Label>("WaitOverlay/WaitPanel/WaitVBox/WaitLabel");

            if (_player is null || _model is null)
            {
                GD.PushError("PlayerCustomizationHud must be initialized before entering the scene tree.");
                Visible = false;
                return;
            }

            Name = $"CustomizationHUD_{_player.PlayerId}";
            MouseFilter = MouseFilterEnum.Stop;
            ZIndex = 50;

            _keyboard.TextChanged += OnKeyboardTextChanged;
            _keyboard.TextCommitted += OnKeyboardTextCommitted;
            _keyboard.CloseRequested += OnKeyboardCloseRequested;

            PopulateDropdowns();
            SyncFromProfile();
            SetWaitMode(_isWaiting);

            if (_pendingSeat is not null)
            {
                ApplySeatZone(_pendingSeat);
                _pendingSeat = null;
            }
        }

        public override void _ExitTree()
        {
            if (_model is not null)
            {
                _model.ProfileChanged -= HandleProfileChanged;
            }

            if (_keyboard != null)
            {
                _keyboard.TextChanged -= OnKeyboardTextChanged;
                _keyboard.TextCommitted -= OnKeyboardTextCommitted;
                _keyboard.CloseRequested -= OnKeyboardCloseRequested;
            }

            SetProcess(false);
            base._ExitTree();
        }

        public override void _Process(double delta)
        {
            if (_spin)
            {
                _waitSpinner.RotationDegrees = Mathf.PosMod(_waitSpinner.RotationDegrees + (float)(delta * 180f), 360f);
            }
        }

        public void ApplySeatZone(SeatZone seatZone)
        {
            _currentSeat = seatZone;

            if (!IsInsideTree())
            {
                _pendingSeat = seatZone;
                return;
            }

            ApplySeatGeometry();
        }

        public void SetWaitMode(bool waiting)
        {
            _isWaiting = waiting;

            if (_contentRoot != null)
            {
                _contentRoot.Visible = !waiting;
            }

            if (_waitOverlay != null)
            {
                _waitOverlay.Visible = waiting;
            }

            if (_waitLabel != null)
            {
                _waitLabel.Text = waiting ? "Waiting for other players to finish…" : string.Empty;
            }

            _spin = waiting && _waitSpinner != null;
            SetProcess(_spin);

            if (!waiting && _waitSpinner != null)
            {
                _waitSpinner.RotationDegrees = 0f;
            }

            ApplySeatGeometry();
        }

        private void ApplySeatGeometry()
        {
            if (_currentSeat is null)
            {
                return;
            }

            var seatZone = _currentSeat;
            var size = CalculateSize(seatZone, _isWaiting);
            CustomMinimumSize = size;
            Size = size;
            PivotOffset = size / 2f;

            Position = CalculatePosition(seatZone, size);
            RotationDegrees = seatZone.RotationDegrees;

            AdjustLayout(size);
        }

        private void OnKeyboardCloseRequested()
        {
            if (_player is null || _isClosing)
            {
                return;
            }

            _isClosing = true;
            if (_keyboard != null)
            {
                _keyboard.CloseRequested -= OnKeyboardCloseRequested;
            }
            MouseFilter = MouseFilterEnum.Ignore;
            StartDismissAnimation();
        }

        private void StartDismissAnimation()
        {
            if (!IsInsideTree())
            {
                EmitCancellation();
                return;
            }

            var tween = CreateTween();
            if (tween is null)
            {
                EmitCancellation();
                return;
            }

            var startScale = Scale;
            if (startScale == Vector2.Zero)
            {
                startScale = Vector2.One;
            }

            var targetScale = startScale * 0.9f;
            var fadeTarget = Modulate;
            fadeTarget.A = 0f;

            tween.TweenProperty(this, "scale", targetScale, 0.22f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            tween.TweenProperty(this, "modulate", fadeTarget, 0.22f)
                .SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.InOut);
            tween.Finished += EmitCancellation;
        }

        private void EmitCancellation()
        {
            if (_player is null)
            {
                return;
            }

            CustomizationCancelled?.Invoke(_player);
        }

        private void PopulateDropdowns()
        {
            _colorDropdown.Clear();
            for (var index = 0; index < _model!.Colors.Count; index++)
            {
                var label = $"Color {index + 1}";
                var color = _model.Colors[index];
                _colorDropdown.AddItem(label, index);
                _colorDropdown.SetItemTooltip(index, color.ToHtml());
            }
            _colorDropdown.ItemSelected += OnColorSelected;
            ConfigureDropdownPopup(_colorDropdown);

            _avatarDropdown.Clear();
            for (var index = 0; index < _model.Avatars.Count; index++)
            {
                _avatarDropdown.AddItem(_model.Avatars[index].Id, index);
            }
            _avatarDropdown.ItemSelected += OnAvatarSelected;
            ConfigureDropdownPopup(_avatarDropdown);
        }

        private void ConfigureDropdownPopup(OptionButton dropdown)
        {
            var popup = dropdown.GetPopup();
            popup.AboutToPopup += () => AdjustPopupPlacement(dropdown, popup);
        }

        private void AdjustPopupPlacement(OptionButton dropdown, PopupMenu popup)
        {
            if (_currentSeat is null)
            {
                return;
            }

            var dropdownRect = dropdown.GetGlobalRect();
            var popupSize = popup.Size;
            if (popupSize == Vector2I.Zero)
            {
                var min = popup.GetMinSize();
                popupSize = new Vector2I((int)MathF.Ceiling(min.X), (int)MathF.Ceiling(min.Y));
            }

            var position = dropdownRect.Position;

            switch (_currentSeat.Edge)
            {
                case TableEdge.Top:
                    position.Y -= popupSize.Y;
                    break;
                case TableEdge.Bottom:
                    position.Y += dropdownRect.Size.Y;
                    break;
                case TableEdge.Left:
                    position.X += dropdownRect.Size.X;
                    position.Y -= (popupSize.Y - dropdownRect.Size.Y) / 2f;
                    break;
                case TableEdge.Right:
                    position.X -= popupSize.X;
                    position.Y -= (popupSize.Y - dropdownRect.Size.Y) / 2f;
                    break;
            }

            popup.CallDeferred(nameof(PopupMenu.SetPosition), new Vector2I((int)MathF.Round(position.X), (int)MathF.Round(position.Y)));
        }

        private void SyncFromProfile()
        {
            if (_player is null || _model is null)
            {
                return;
            }

            _nameLabel.Text = _model.DisplayName;
            _keyboard.SetText(_model.DisplayName);

            _colorDropdown.Select(_model.SelectedColorIndex);
            _colorSwatch.Color = _model.Colors[_model.SelectedColorIndex];
            if (_player.DisplayColor.HasValue)
            {
                _colorDropdown.SetItemTooltip(_model.SelectedColorIndex, _player.DisplayColor.Value.ToHtml());
            }

            _avatarDropdown.Select(_model.SelectedAvatarIndex);
        }

        private void HandleProfileChanged(PlayerProfile profile)
        {
            _player = profile;
            _suppressCallbacks = true;
            SyncFromProfile();
            _suppressCallbacks = false;
            ProfileChanged?.Invoke(profile);
        }

        private void OnKeyboardTextChanged(string text)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            _model?.SetDisplayName(text);
            _nameLabel.Text = _model?.DisplayName ?? text;
        }

        private void OnKeyboardTextCommitted(string text)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            _model?.SetDisplayName(text);
            CompleteCustomization();
        }

        private void OnColorSelected(long indexValue)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            var index = (int)indexValue;
            _model?.SelectColor(index);

            if (_model is not null)
            {
                var color = _model.Colors[index];
                _colorSwatch.Color = color;
                _colorDropdown.SetItemTooltip(index, color.ToHtml());
            }
        }

        private void OnAvatarSelected(long indexValue)
        {
            if (_suppressCallbacks)
            {
                return;
            }

            var index = (int)indexValue;
            _model?.SelectAvatar(index);
        }

        private void CompleteCustomization()
        {
            if (_player is null)
            {
                return;
            }

            if (_model is not null && string.IsNullOrWhiteSpace(_model.DisplayName))
            {
                _model.SetDisplayName(_model.BaselineName);
                _nameLabel.Text = _model.BaselineName;
            }

            SetWaitMode(true);
            CustomizationCompleted?.Invoke(_player);
        }

        private void AdjustLayout(Vector2 size)
        {
            if (_keyboard is null || _keyboardContainer is null)
            {
                return;
            }

            if (_isWaiting)
            {
                _keyboardContainer.CustomMinimumSize = Vector2.Zero;
                _keyboard.CustomMinimumSize = Vector2.Zero;
                return;
            }

            var keyboardHeight = Mathf.Clamp(size.Y * 0.42f, 88f, size.Y * 0.6f);
            var rowCount = Math.Max(1, _keyboard.RowCount);
            var keyHeight = Mathf.Clamp(keyboardHeight / rowCount, 22f, 34f);

            _keyboard.SetKeyHeight(keyHeight);
            _keyboard.CustomMinimumSize = new Vector2(size.X, keyboardHeight);
            _keyboardContainer.CustomMinimumSize = new Vector2(size.X, keyboardHeight);
        }

        private Vector2 CalculateSize(SeatZone seatZone, bool waiting)
        {
            var (lengthLimit, thicknessLimit) = GetSeatDimensions(seatZone);

            if (waiting)
            {
                var dimension = Mathf.Clamp(Mathf.Min(lengthLimit, thicknessLimit) * 0.45f, 80f, 140f);
                return new Vector2(dimension, dimension);
            }

            var width = Mathf.Clamp(lengthLimit - (HorizontalMargin * 2f), 220f, Mathf.Min(lengthLimit, MaxWidth));
            var height = Mathf.Clamp(thicknessLimit - (VerticalMargin * 2f), 160f, Mathf.Min(thicknessLimit, MaxHeight));
            return new Vector2(width, height);
        }

        private Vector2 CalculatePosition(SeatZone seatZone, Vector2 size)
        {
            var viewport = GetViewport().GetVisibleRect();

            Vector2 basePosition = seatZone.Edge switch
            {
                TableEdge.Bottom => new Vector2(
                    seatZone.ScreenRegion.Position.X + (seatZone.ScreenRegion.Size.X - size.X) / 2f,
                    seatZone.ScreenRegion.End.Y - size.Y - BottomEdgePadding - IndicatorClearance),
                TableEdge.Top => new Vector2(
                    seatZone.ScreenRegion.Position.X + (seatZone.ScreenRegion.Size.X - size.X) / 2f,
                    seatZone.ScreenRegion.Position.Y + IndicatorClearance),
                TableEdge.Left or TableEdge.Right => seatZone.ScreenRegion.Position + (seatZone.ScreenRegion.Size / 2f) - (size / 2f),
                _ => seatZone.ScreenRegion.Position
            };

            var clampedX = Mathf.Clamp(basePosition.X, viewport.Position.X, viewport.End.X - size.X);
            var clampedY = Mathf.Clamp(basePosition.Y, viewport.Position.Y, viewport.End.Y - size.Y);
            return new Vector2(clampedX, clampedY);
        }

        private static (float length, float thickness) GetSeatDimensions(SeatZone seatZone)
        {
            return seatZone.Edge switch
            {
                TableEdge.Left or TableEdge.Right => (seatZone.ScreenRegion.Size.Y, seatZone.ScreenRegion.Size.X),
                _ => (seatZone.ScreenRegion.Size.X, seatZone.ScreenRegion.Size.Y)
            };
        }

        public bool ContainsGlobalPoint(Vector2 point)
        {
            return GetGlobalRect().HasPoint(point);
        }
    }
}
