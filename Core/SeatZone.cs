using Godot;

namespace TableCore.Core
{
    public class SeatZone
    {
        public Rect2 ScreenRegion { get; set; }
        public TableEdge Edge { get; set; }
        public float RotationDegrees { get; set; }
        public Vector2 AnchorPoint { get; set; }
    }
}