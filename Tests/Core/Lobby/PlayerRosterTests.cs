using System;
using System.Collections.Generic;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Lobby;

namespace TableCore.Tests.Core.Lobby
{
	public class PlayerRosterTests
	{
		[Test]
		public void RemovePlayer_RemovesMatchingProfile_AndClearsCompletion()
		{
			var session = new SessionState();
			var targetId = Guid.NewGuid();
			var extraId = Guid.NewGuid();

			session.PlayerProfiles.Add(new PlayerProfile { PlayerId = targetId });
			session.PlayerProfiles.Add(new PlayerProfile { PlayerId = extraId });

			var completed = new HashSet<Guid> { targetId, Guid.NewGuid() };

			var removed = PlayerRoster.RemovePlayer(session, targetId, completed, out var removedProfile);

			Assert.That(removed, Is.True);
			Assert.That(removedProfile, Is.Not.Null);
			Assert.That(removedProfile!.PlayerId, Is.EqualTo(targetId));
			Assert.That(session.PlayerProfiles, Has.All.Matches<PlayerProfile>(p => p.PlayerId != targetId));
			Assert.That(completed.Contains(targetId), Is.False);
			Assert.That(completed.Count, Is.EqualTo(1));
		}

		[Test]
		public void RemovePlayer_WhenMissing_ReturnsFalseAndCleansCompletedFlags()
		{
			var session = new SessionState();
			var existingId = Guid.NewGuid();
			session.PlayerProfiles.Add(new PlayerProfile { PlayerId = existingId });

			var missingId = Guid.NewGuid();
			var completed = new HashSet<Guid> { missingId };

			var removed = PlayerRoster.RemovePlayer(session, missingId, completed, out var removedProfile);

			Assert.That(removed, Is.False);
			Assert.That(removedProfile, Is.Null);
			Assert.That(session.PlayerProfiles.Count, Is.EqualTo(1));
			Assert.That(completed.Contains(missingId), Is.False);
		}
	}
}
