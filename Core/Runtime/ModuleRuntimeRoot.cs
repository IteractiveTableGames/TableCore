using Godot;
using TableCore.Core;

namespace TableCore.Core.Runtime
{
    /// <summary>
    /// Root node for the gameplay runtime. Consumes the lobby's session snapshot and
    /// prepares the layers modules will populate.
    /// </summary>
    public partial class ModuleRuntimeRoot : Node
    {
        private const string DefaultLobbyScene = "res://Core/Lobby/Lobby.tscn";

        private Node2D? _boardLayer;
        private CanvasLayer? _hudLayer;

        [Export(PropertyHint.File, "*.tscn")]
        public string LobbyScenePath { get; set; } = DefaultLobbyScene;

        public SessionState? Session { get; private set; }

        public override void _Ready()
        {
            _boardLayer = GetNodeOrNull<Node2D>("BoardLayer");
            _hudLayer = GetNodeOrNull<CanvasLayer>("HUDLayer");

            Session = SessionTransfer.Consume();

            if (Session != null)
            {
                return;
            }

            GD.PushWarning("ModuleRuntimeRoot loaded without a pending session. Returning to the lobby.");

            if (!string.IsNullOrWhiteSpace(LobbyScenePath))
            {
                GetTree().ChangeSceneToFile(LobbyScenePath);
            }
        }

        public Node2D? GetBoardLayer() => _boardLayer;

        public CanvasLayer? GetHudLayer() => _hudLayer;
    }
}
