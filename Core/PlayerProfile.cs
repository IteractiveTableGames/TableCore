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
    }
}
