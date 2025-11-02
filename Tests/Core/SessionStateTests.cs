using System;
using System.Collections.Generic;
using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class SessionStateTests
    {
        [Test]
        public void SessionState_Properties_CanBeSetAndGet()
        {
            var sessionState = new SessionState();
            var playerProfiles = new List<PlayerProfile>();
            var selectedModule = new ModuleDescriptor();
            var sessionOptions = new Dictionary<string, object>();

            sessionState.PlayerProfiles = playerProfiles;
            sessionState.SelectedModule = selectedModule;
            sessionState.SessionOptions = sessionOptions;

            Assert.That(sessionState.PlayerProfiles, Is.EqualTo(playerProfiles));
            Assert.That(sessionState.SelectedModule, Is.EqualTo(selectedModule));
            Assert.That(sessionState.SessionOptions, Is.EqualTo(sessionOptions));
        }

        [Test]
        public void SessionState_InitializesLists()
        {
            var sessionState = new SessionState();
            Assert.That(sessionState.PlayerProfiles, Is.Not.Null);
            Assert.That(sessionState.SessionOptions, Is.Not.Null);
        }

        [Test]
        public void Clone_CreatesSnapshotWithIndependentCollections()
        {
            var sessionState = new SessionState();
            sessionState.PlayerProfiles.Add(new PlayerProfile
            {
                PlayerId = Guid.NewGuid(),
                DisplayName = "Alice",
                Seat = new SeatZone { Edge = TableEdge.Bottom }
            });
            sessionState.SelectedModule = new ModuleDescriptor
            {
                ModuleId = "sample.module",
                DisplayName = "Sample",
                MinPlayers = 1,
                MaxPlayers = 4
            };
            sessionState.SessionOptions["difficulty"] = "hard";

            var snapshot = sessionState.Clone();

            sessionState.PlayerProfiles[0].DisplayName = "Changed";
            sessionState.PlayerProfiles.Add(new PlayerProfile { PlayerId = Guid.NewGuid() });
            sessionState.SessionOptions["difficulty"] = "easy";
            sessionState.SelectedModule!.DisplayName = "Other";

            Assert.Multiple(() =>
            {
                Assert.That(snapshot.PlayerProfiles, Has.Count.EqualTo(1));
                Assert.That(snapshot.PlayerProfiles[0].DisplayName, Is.EqualTo("Alice"));
                Assert.That(snapshot.SessionOptions["difficulty"], Is.EqualTo("hard"));
                Assert.That(snapshot.SelectedModule?.DisplayName, Is.EqualTo("Sample"));
            });
        }
    }
}
