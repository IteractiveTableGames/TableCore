using System;
using System.Collections.Generic;
using Godot;
using TableCore.Core.Board;
using TableCore.Core.UI;

namespace TableCore.Core.Modules
{
    /// <summary>
    /// Concrete implementation that wires together framework services for modules.
    /// </summary>
    public sealed class ModuleServices : IModuleServices
    {
        private readonly SessionState _session;
        private readonly CanvasLayer _hudLayer;
        private readonly Action _returnToLobby;
        private readonly TurnManager _turnManager = new();
        private readonly DiceService _diceService = new();
        private readonly CurrencyBank _currencyBank = new();
        private readonly CardService _cardService;
        private readonly HUDService _hudService;
        private readonly AnimationService _animationService;
        private readonly BoardManager _boardManager;

        public ModuleServices(SessionState session, SceneTree sceneTree, CanvasLayer hudLayer, Action returnToLobby)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _hudLayer = hudLayer ?? throw new ArgumentNullException(nameof(hudLayer));
            _returnToLobby = returnToLobby ?? throw new ArgumentNullException(nameof(returnToLobby));

            _hudService = new HUDService(_hudLayer, _session);
            _cardService = new CardService(_hudService);
            _animationService = new AnimationService(sceneTree ?? throw new ArgumentNullException(nameof(sceneTree)));
            _boardManager = new BoardManager(_animationService);
        }

        public IReadOnlyList<PlayerProfile> GetPlayers() => _session.PlayerProfiles;

        public TurnManager GetTurnManager() => _turnManager;

        public DiceService GetDiceService() => _diceService;

        public CurrencyBank GetBank() => _currencyBank;

        public CardService GetCardService() => _cardService;

        public IHUDService GetHUDService() => _hudService;

        public AnimationService GetAnimationService() => _animationService;

        public IBoardManager GetBoardManager() => _boardManager;

        public SessionState GetSessionState() => _session;

        public void ReturnToLobby() => _returnToLobby();
    }
}
