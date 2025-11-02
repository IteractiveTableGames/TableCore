using System;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class SessionTransferTests
    {
        [SetUp]
        public void SetUp()
        {
            SessionTransfer.Consume();
        }

        [Test]
        public void Store_ClonesSessionState()
        {
            var playerId = Guid.NewGuid();
            var session = new SessionState();
            session.PlayerProfiles.Add(new PlayerProfile
            {
                PlayerId = playerId,
                DisplayName = "Alice",
                Seat = new SeatZone { Edge = TableEdge.Bottom }
            });
            session.SelectedModule = new ModuleDescriptor
            {
                ModuleId = "mod.test",
                DisplayName = "Test Module",
                MinPlayers = 1,
                MaxPlayers = 4
            };

            SessionTransfer.Store(session);

            session.PlayerProfiles[0].DisplayName = "Changed";
            session.PlayerProfiles.Add(new PlayerProfile { PlayerId = Guid.NewGuid(), DisplayName = "Bob" });
            session.SelectedModule!.DisplayName = "Other";

            Assert.That(SessionTransfer.HasPendingSession, Is.True);

            var snapshot = SessionTransfer.Consume();

            Assert.Multiple(() =>
            {
                Assert.That(snapshot, Is.Not.Null);
                Assert.That(snapshot!.PlayerProfiles, Has.Count.EqualTo(1));
                Assert.That(snapshot.PlayerProfiles[0].DisplayName, Is.EqualTo("Alice"));
                Assert.That(snapshot.SelectedModule?.DisplayName, Is.EqualTo("Test Module"));
                Assert.That(SessionTransfer.HasPendingSession, Is.False);
            });
        }

        [Test]
        public void Consume_ReturnsNullWhenNoSessionStored()
        {
            var snapshot = SessionTransfer.Consume();
            Assert.That(snapshot, Is.Null);
            Assert.That(SessionTransfer.HasPendingSession, Is.False);
        }

        [Test]
        public void Store_WithNullSession_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SessionTransfer.Store(null!));
        }
    }
}
