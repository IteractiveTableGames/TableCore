using System;
using Godot;
using TableCore.Core;
using TableCore.Core.Input;

namespace TableCore.Input
{
    /// <summary>
    /// Routes screen touch events to either the shared board or a player's HUD region based on the touch position.
    /// </summary>
    public partial class InputRouter : Node
    {
        [Signal]
        public delegate void PlayerHudTouchEventHandler(int playerIndex, InputEvent inputEvent);

        [Signal]
        public delegate void BoardTouchEventHandler(InputEvent inputEvent);

        public SessionState? Session { get; set; }

        /// <summary>
        /// Typed event for C# callers who prefer working with the resolved <see cref="PlayerProfile"/>.
        /// </summary>
        public event Action<PlayerProfile, InputEvent>? PlayerHudTouchTyped;

        public override void _UnhandledInput(InputEvent @event)
        {
            if (!IsScreenTouch(@event))
            {
                return;
            }

            var position = GetEventPosition(@event);
            var playerIndex = InputRoutingHelper.ResolvePlayerIndex(Session, position);
            var player = playerIndex.HasValue ? GetPlayerByIndex(playerIndex.Value) : null;

            if (player != null && playerIndex.HasValue)
            {
                EmitSignal(SignalName.PlayerHudTouch, playerIndex.Value, @event);
                PlayerHudTouchTyped?.Invoke(player, @event);
                return;
            }

            EmitSignal(SignalName.BoardTouch, @event);
        }

        /// <summary>
        /// Retrieves the <see cref="PlayerProfile"/> at the provided project-wide index.
        /// Returns null if the index is out of range or the session has not been assigned.
        /// </summary>
        public PlayerProfile? GetPlayerByIndex(int playerIndex)
        {
            if (Session?.PlayerProfiles == null)
            {
                return null;
            }

            if (playerIndex < 0 || playerIndex >= Session.PlayerProfiles.Count)
            {
                return null;
            }

            return Session.PlayerProfiles[playerIndex];
        }

        private static bool IsScreenTouch(InputEvent @event)
        {
            return @event is InputEventScreenTouch or InputEventScreenDrag;
        }

        private static Vector2? GetEventPosition(InputEvent @event)
        {
            return @event switch
            {
                InputEventScreenTouch touch => touch.Position,
                InputEventScreenDrag drag => drag.Position,
                _ => null
            };
        }
    }
}
