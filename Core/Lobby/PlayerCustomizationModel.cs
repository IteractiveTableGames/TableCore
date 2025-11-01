using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using TableCore.Core;

namespace TableCore.Lobby
{
    /// <summary>
    /// Applies display name, color, and avatar selections to a <see cref="PlayerProfile"/>.
    /// </summary>
    public sealed class PlayerCustomizationModel
    {
        private readonly PlayerProfile _profile;
        private readonly List<Color> _colors;
        private readonly List<AvatarOption> _avatars;
        private readonly string _baselineName;

        /// <summary>
        /// Occurs whenever the underlying profile is updated.
        /// </summary>
        public event Action<PlayerProfile>? ProfileChanged;

        public PlayerCustomizationModel(PlayerProfile profile, IEnumerable<Color> colors, IEnumerable<AvatarOption> avatars)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            _colors = (colors ?? throw new ArgumentNullException(nameof(colors))).ToList();
            _avatars = (avatars ?? throw new ArgumentNullException(nameof(avatars))).ToList();

            if (_colors.Count == 0)
            {
                throw new ArgumentException("At least one color must be provided.", nameof(colors));
            }

            if (_avatars.Count == 0)
            {
                throw new ArgumentException("At least one avatar must be provided.", nameof(avatars));
            }

            _baselineName = string.IsNullOrWhiteSpace(_profile.DisplayName)
                ? "Player"
                : _profile.DisplayName!;

            if (string.IsNullOrWhiteSpace(_profile.DisplayName))
            {
                _profile.DisplayName = _baselineName;
            }

            SelectedColorIndex = ResolveColorIndex(_profile.DisplayColor);
            SelectedAvatarIndex = ResolveAvatarIndex(_profile.Avatar);

            // Normalize to ensure profile has a valid color/avatar even if null initially.
            ApplyColor(SelectedColorIndex, raiseEvent: false);
            ApplyAvatar(SelectedAvatarIndex, raiseEvent: false);
        }

        /// <summary>
        /// Gets the index of the currently selected color.
        /// </summary>
        public int SelectedColorIndex { get; private set; }

        /// <summary>
        /// Gets the index of the currently selected avatar.
        /// </summary>
        public int SelectedAvatarIndex { get; private set; }

        /// <summary>
        /// Gets the player's current display name.
        /// </summary>
        public string DisplayName => _profile.DisplayName ?? string.Empty;

        /// <summary>
        /// Gets the baseline name used when no custom value is provided.
        /// </summary>
        public string BaselineName => _baselineName;

        public IReadOnlyList<Color> Colors => _colors;

        public IReadOnlyList<AvatarOption> Avatars => _avatars;

        /// <summary>
        /// Sets the player's display name, trimming whitespace and limiting length to 24 characters.
        /// </summary>
        public void SetDisplayName(string? value)
        {
            var sanitized = (value ?? string.Empty).Trim();

            if (sanitized.Length > 24)
            {
                sanitized = sanitized.Substring(0, 24);
            }

            if (string.Equals(_profile.DisplayName, sanitized, StringComparison.Ordinal))
            {
                return;
            }

            _profile.DisplayName = sanitized;
            ProfileChanged?.Invoke(_profile);
        }

        /// <summary>
        /// Applies the color at the specified index to the player.
        /// </summary>
        public void SelectColor(int index)
        {
            ValidateIndex(index, _colors.Count, nameof(index));

            if (index == SelectedColorIndex)
            {
                return;
            }

            ApplyColor(index, raiseEvent: true);
        }

        /// <summary>
        /// Applies the avatar at the specified index to the player.
        /// </summary>
        public void SelectAvatar(int index)
        {
            ValidateIndex(index, _avatars.Count, nameof(index));

            if (index == SelectedAvatarIndex)
            {
                return;
            }

            ApplyAvatar(index, raiseEvent: true);
        }

        private void ApplyColor(int index, bool raiseEvent)
        {
            SelectedColorIndex = index;
            _profile.DisplayColor = _colors[index];

            if (raiseEvent)
            {
                ProfileChanged?.Invoke(_profile);
            }
        }

        private void ApplyAvatar(int index, bool raiseEvent)
        {
            SelectedAvatarIndex = index;
            _profile.Avatar = _avatars[index].Texture;

            if (raiseEvent)
            {
                ProfileChanged?.Invoke(_profile);
            }
        }

        private int ResolveColorIndex(Color? currentColor)
        {
            if (currentColor.HasValue)
            {
                for (var index = 0; index < _colors.Count; index++)
                {
                    if (AreColorsEqual(_colors[index], currentColor.Value))
                    {
                        return index;
                    }
                }
            }

            return 0;
        }

        private int ResolveAvatarIndex(Texture2D? currentAvatar)
        {
            if (currentAvatar is not null)
            {
                for (var index = 0; index < _avatars.Count; index++)
                {
                    if (ReferenceEquals(_avatars[index].Texture, currentAvatar))
                    {
                        return index;
                    }
                }
            }

            return 0;
        }

        private static bool AreColorsEqual(Color a, Color b)
        {
            return Mathf.IsEqualApprox(a.R, b.R)
                && Mathf.IsEqualApprox(a.G, b.G)
                && Mathf.IsEqualApprox(a.B, b.B)
                && Mathf.IsEqualApprox(a.A, b.A);
        }

        private static void ValidateIndex(int index, int count, string parameterName)
        {
            if (index < 0 || index >= count)
            {
                throw new ArgumentOutOfRangeException(parameterName, index, "Index is outside the available options.");
            }
        }
    }
}
