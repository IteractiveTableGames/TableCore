using System.Collections.Generic;

namespace TableCore.Core.Modules
{
    /// <summary>
    /// Surface of framework functionality available to runtime modules.
    /// Future tasks will expand this interface as additional systems come online.
    /// </summary>
    public interface IModuleServices
    {
        /// <summary>
        /// Returns the active players participating in the session.
        /// </summary>
        IReadOnlyList<PlayerProfile> GetPlayers();

        /// <summary>
        /// Provides access to the shared turn manager.
        /// </summary>
        TurnManager GetTurnManager();

        /// <summary>
        /// Provides access to the shared dice service.
        /// </summary>
        DiceService GetDiceService();

        /// <summary>
        /// Provides access to the shared currency bank.
        /// </summary>
        CurrencyBank GetBank();

        /// <summary>
        /// Provides access to the shared card service.
        /// </summary>
        CardService GetCardService();

        /// <summary>
        /// Provides methods for creating and updating per-player HUDs.
        /// </summary>
        IHUDService GetHUDService();

        /// <summary>
        /// Returns the current session snapshot for convenience.
        /// </summary>
        SessionState GetSessionState();

        /// <summary>
        /// Requests a transition back to the lobby.
        /// </summary>
        void ReturnToLobby();
    }
}
