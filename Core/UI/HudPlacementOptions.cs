using Godot;

namespace TableCore.Core.UI
{
    /// <summary>
    /// Configures how player HUDs are positioned within their seat zones.
    /// </summary>
    public sealed class HudPlacementOptions
    {
        /// <summary>
        /// Gets or sets how far HUDs should be pushed away from the board-facing edge, in pixels.
        /// </summary>
        public float BoardClearance { get; set; } = 64f;

        /// <summary>
        /// Gets or sets how much padding to leave near the outer table edge, in pixels.
        /// </summary>
        public float EdgePadding { get; set; } = 12f;

        public static HudPlacementOptions Default => new();

        public HudPlacementOptions Clone()
        {
            return new HudPlacementOptions
            {
                BoardClearance = BoardClearance,
                EdgePadding = EdgePadding
            };
        }
    }
}
