using System;
using System.Collections.Generic;
using Godot;
using TableCore.Core.UI;

namespace TableCore.Core
{
    /// <summary>
    /// Describes the contract for the per-player HUD management service.
    /// </summary>
    public interface IHUDService
    {
        /// <summary>
        /// Creates or returns the HUD container for the specified player.
        /// </summary>
        /// <param name="player">The player profile.</param>
        /// <returns>An object that allows modules to customize the player's HUD.</returns>
        IPlayerHUD CreatePlayerHUD(PlayerProfile player);

        /// <summary>
        /// Updates the displayed funds for the specified player.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <param name="newAmount">The new funds total.</param>
        void UpdateFunds(Guid playerId, int newAmount);

        /// <summary>
        /// Updates the visual representation of the player's hand.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <param name="cards">The cards currently held by the player.</param>
        void UpdateHand(Guid playerId, IReadOnlyList<CardData> cards);

        /// <summary>
        /// Displays an informational prompt to the specified player.
        /// </summary>
        /// <param name="playerId">The player identifier.</param>
        /// <param name="message">The prompt message.</param>
        void SetPrompt(Guid playerId, string message);

        /// <summary>
        /// Configures default HUD placement behaviour within each seat zone.
        /// </summary>
        /// <param name="options">Placement options to apply.</param>
        void ConfigureHudPlacement(HudPlacementOptions options);

        /// <summary>
        /// Sets a custom resolver that can override the computed seat region.
        /// </summary>
        /// <param name="resolver">Resolver invoked for each seat, or null to restore default behaviour.</param>
        void SetSeatRegionResolver(Func<SeatZone, Rect2>? resolver);
    }

    /// <summary>
    /// Represents a handle to a player's HUD root control.
    /// </summary>
    public interface IPlayerHUD
    {
        /// <summary>
        /// Gets the identifier of the player that owns this HUD.
        /// </summary>
        Guid PlayerId { get; }

        /// <summary>
        /// Gets the Godot control node that acts as the HUD root.
        /// </summary>
        /// <returns>The root control node.</returns>
        Control GetRootControl();

        /// <summary>
        /// Adds a child control node under the HUD root.
        /// </summary>
        /// <param name="controlNode">The node to attach.</param>
        void AddControl(Node controlNode);
    }
}
