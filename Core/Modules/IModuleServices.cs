using System.Collections.Generic;
using TableCore.Core.Board;

namespace TableCore.Core.Modules
{
    /// <summary>
    /// Surface of framework functionality available to runtime modules.
    /// Future tasks will expand this interface as additional systems come online.
    /// </summary>
    public interface IModuleServices
    {
        IReadOnlyList<PlayerProfile> GetPlayers();
        TurnManager GetTurnManager();
        DiceService GetDiceService();
        CurrencyBank GetBank();
        CardService GetCardService();
        IHUDService GetHUDService();
        AnimationService GetAnimationService();
        IBoardManager GetBoardManager();
        SessionState GetSessionState();
        void ReturnToLobby();
    }
}
