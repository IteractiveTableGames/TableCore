using Godot;
using System;

namespace TableCore.Core
{
    public class PlayerProfile
    {
        public Guid PlayerId { get; set; }
        public string? DisplayName { get; set; }
        public Color? DisplayColor { get; set; }
        public Texture2D? Avatar { get; set; }
        public SeatZone? Seat { get; set; }
        public bool IsGameMaster { get; set; }

        /// <summary>
        /// Produces a copy of this profile so mutations in a new session do not affect the original instance.
        /// </summary>
        public PlayerProfile Clone()
        {
            return new PlayerProfile
            {
                PlayerId = PlayerId,
                DisplayName = DisplayName,
                DisplayColor = DisplayColor,
                Avatar = Avatar,
                Seat = Seat?.Clone(),
                IsGameMaster = IsGameMaster
            };
        }
    }
}
