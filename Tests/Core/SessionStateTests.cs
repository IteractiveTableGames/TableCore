using NUnit.Framework;
using TableCore.Core;
using System.Collections.Generic;

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
    }
}