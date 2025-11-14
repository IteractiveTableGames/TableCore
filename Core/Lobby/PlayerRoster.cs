using System;
using System.Collections.Generic;
using TableCore.Core;

namespace TableCore.Lobby
{
	internal static class PlayerRoster
	{
		/// <summary>
		/// Removes the specified player from the session roster and clears any completion flags.
		/// </summary>
		public static bool RemovePlayer(SessionState session, Guid playerId, ISet<Guid> completedCustomizations, out PlayerProfile? removedProfile)
		{
			if (session == null)
			{
				throw new ArgumentNullException(nameof(session));
			}

			if (completedCustomizations == null)
			{
				throw new ArgumentNullException(nameof(completedCustomizations));
			}

			for (var index = 0; index < session.PlayerProfiles.Count; index++)
			{
				var profile = session.PlayerProfiles[index];

				if (profile?.PlayerId != playerId)
				{
					continue;
				}

				session.PlayerProfiles.RemoveAt(index);
				completedCustomizations.Remove(playerId);
				removedProfile = profile;
				return true;
			}

			completedCustomizations.Remove(playerId);
			removedProfile = null;
			return false;
		}
	}
}
