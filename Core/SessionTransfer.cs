using System;

namespace TableCore.Core
{
    /// <summary>
    /// Temporary storage used to move a captured <see cref="SessionState"/> from the lobby scene
    /// into the runtime scene when changing the active scene tree.
    /// </summary>
    public static class SessionTransfer
    {
        private static SessionState? _pendingSession;

        /// <summary>
        /// Stores a snapshot of the provided session so it can be retrieved after the next scene loads.
        /// </summary>
        public static void Store(SessionState sessionState)
        {
            if (sessionState is null)
            {
                throw new ArgumentNullException(nameof(sessionState));
            }

            _pendingSession = sessionState.Clone();
        }

        /// <summary>
        /// Retrieves the pending session and clears the stored snapshot.
        /// </summary>
        public static SessionState? Consume()
        {
            var snapshot = _pendingSession;
            _pendingSession = null;
            return snapshot;
        }

        /// <summary>
        /// Returns true when a session snapshot has been stored and not yet consumed.
        /// </summary>
        public static bool HasPendingSession => _pendingSession != null;
    }
}
