using Godot;

namespace TableCore.Modules.Monopolyish
{
    /// <summary>
    /// Simple circular token used to visualize player positions on the demo board.
    /// </summary>
    public partial class MonopolyishTokenVisual : Node2D
    {
        [Export]
        public Color TokenColor { get; set; } = new Color(0.9f, 0.9f, 0.9f);

        [Export(PropertyHint.Range, "8,32,1")]
        public float Radius { get; set; } = 16f;

        public override void _Ready()
        {
            QueueRedraw();
        }

        public override void _Draw()
        {
            DrawCircle(Vector2.Zero, Radius, TokenColor);
            DrawArc(Vector2.Zero, Radius + 2f, 0f, Mathf.Tau, 32, new Color(0f, 0f, 0f, 0.65f), 2f);
        }
    }
}
