using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using TableCore.Core;
using TableCore.Core.Modules;
using TableCore.Core.UI;

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
        private Node? _moduleInstance;
        private IGameModule? _gameModule;
        private ModuleServices? _services;

        [Export(PropertyHint.File, "*.tscn")]
        public string LobbyScenePath { get; set; } = DefaultLobbyScene;

        public SessionState? Session { get; private set; }

        public override void _Ready()
        {
            _boardLayer = GetNodeOrNull<Node2D>("BoardLayer");
            _hudLayer = GetNodeOrNull<CanvasLayer>("HUDLayer");

            Session = SessionTransfer.Consume();

            if (Session == null)
            {
                GD.PushWarning("ModuleRuntimeRoot loaded without a pending session. Returning to the lobby.");
                ReturnToLobby();
                return;
            }

            if (!TryLoadSelectedModule(Session.SelectedModule))
            {
                GD.PushWarning("Failed to load selected module. Returning to the lobby.");
                ReturnToLobby();
            }
        }

        public Node2D? GetBoardLayer() => _boardLayer;

        public CanvasLayer? GetHudLayer() => _hudLayer;

        public override void _Process(double delta)
        {
            base._Process(delta);
            _gameModule?.Tick(delta);
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            _gameModule?.Shutdown();
            _services = null;
            _moduleInstance = null;
            _gameModule = null;
        }

        private bool TryLoadSelectedModule(ModuleDescriptor? descriptor)
        {
            if (descriptor == null)
            {
                GD.PushWarning("No module selected.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(descriptor.EntryScenePath))
            {
                GD.PushWarning($"Module '{descriptor.DisplayName}' has no entry scene defined.");
                return false;
            }

            var absoluteScenePath = ResolveEntryScenePath(descriptor);
            if (absoluteScenePath == null)
            {
                GD.PushWarning($"Unable to resolve entry scene for module '{descriptor.DisplayName}'.");
                return false;
            }

            var resourcePath = LocalizePath(absoluteScenePath);
            if (resourcePath == null || !ResourceLoader.Exists(resourcePath))
            {
                GD.PushWarning($"Module scene not found: {absoluteScenePath}");
                return false;
            }

            if (ResourceLoader.Load(resourcePath) is not PackedScene scene)
            {
                GD.PushWarning($"Module scene is not a PackedScene: {resourcePath}");
                return false;
            }

            ClearLayer(_boardLayer);
            _moduleInstance = scene.Instantiate();

            if (_moduleInstance == null)
            {
                GD.PushWarning("Instantiated module scene returned null instance.");
                return false;
            }

            Node parent = _boardLayer != null ? _boardLayer : this;
            parent.AddChild(_moduleInstance);

            _gameModule = _moduleInstance as IGameModule;
            if (_gameModule == null)
            {
                GD.PushWarning($"Module root does not implement IGameModule: {resourcePath}");
                return true; // visual content is present even without the interface
            }

            if (_hudLayer == null)
            {
                _hudLayer = new CanvasLayer { Name = "HUDLayer" };
                AddChild(_hudLayer);
            }
            else
            {
                ClearLayer(_hudLayer);
            }

            _services = new ModuleServices(Session!, GetTree(), _hudLayer, ReturnToLobby);
            _gameModule.Initialize(Session!, _services);
            return true;
        }

        private static string? ResolveEntryScenePath(ModuleDescriptor descriptor)
        {
            try
            {
                var entryPath = descriptor.EntryScenePath?.Replace('/', Path.DirectorySeparatorChar) ?? string.Empty;
                var combined = Path.Combine(descriptor.ModulePath ?? string.Empty, entryPath);
                return Path.GetFullPath(combined);
            }
            catch
            {
                return null;
            }
        }

        private static string? LocalizePath(string absolutePath)
        {
            try
            {
                return ProjectSettings.LocalizePath(absolutePath);
            }
            catch
            {
                return null;
            }
        }

        private void ReturnToLobby()
        {
            if (!string.IsNullOrWhiteSpace(LobbyScenePath))
            {
                GetTree().ChangeSceneToFile(LobbyScenePath);
            }
        }

        private void ClearLayer(Node? layer)
        {
            if (layer == null)
            {
                return;
            }

            foreach (var child in layer.GetChildren())
            {
                if (child is Node node)
                {
                    node.QueueFree();
                }
            }
        }

        private sealed class ModuleServices : IModuleServices
        {
            private readonly SessionState _session;
            private readonly SceneTree _sceneTree;
            private readonly CanvasLayer _hudLayer;
            private readonly Action _returnToLobby;
            private readonly TurnManager _turnManager = new();
            private readonly DiceService _diceService = new();
            private readonly CurrencyBank _bank = new();
            private readonly CardService _cardService;
            private readonly HUDService _hudService;

            public ModuleServices(SessionState session, SceneTree sceneTree, CanvasLayer hudLayer, Action returnToLobby)
            {
                _session = session;
                _sceneTree = sceneTree;
                _hudLayer = hudLayer ?? throw new ArgumentNullException(nameof(hudLayer));
                _returnToLobby = returnToLobby;
                _hudService = new HUDService(_hudLayer, _session);
                _cardService = new CardService(_hudService);
            }

            public IReadOnlyList<PlayerProfile> GetPlayers() => _session.PlayerProfiles;

            public TurnManager GetTurnManager() => _turnManager;

            public DiceService GetDiceService() => _diceService;

            public CurrencyBank GetBank() => _bank;

            public CardService GetCardService() => _cardService;

            public IHUDService GetHUDService() => _hudService;

            public SessionState GetSessionState() => _session;

            public void ReturnToLobby() => _returnToLobby();
        }
    }
}
