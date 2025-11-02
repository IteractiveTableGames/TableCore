using Godot;

namespace TableCore.Core
{
    public class SeatZone
    {
        public Rect2 ScreenRegion { get; set; }
        public TableEdge Edge { get; set; }
        public float RotationDegrees { get; set; }
        public Vector2 AnchorPoint { get; set; }

        /// <summary>
        /// Creates a copy of this <see cref="SeatZone"/> preserving all spatial values.
        /// </summary>
        public SeatZone Clone()
        {
            return new SeatZone
            {
                ScreenRegion = ScreenRegion,
                Edge = Edge,
                RotationDegrees = RotationDegrees,
                AnchorPoint = AnchorPoint
            };
        }
    }
}
