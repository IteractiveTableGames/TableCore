using System.Collections.Generic;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Lobby;

namespace TableCore.Tests.Lobby
{
    [TestFixture]
    public class ModuleSelectionModelTests
    {
        [Test]
        public void SetModules_SelectsFirstModule_WhenSessionEmpty()
        {
            var session = new SessionState();
            var model = new ModuleSelectionModel(session);

            model.SetModules(new[]
            {
                new ModuleDescriptor { ModuleId = "alpha", DisplayName = "Alpha" },
                new ModuleDescriptor { ModuleId = "beta", DisplayName = "Beta" }
            });

            Assert.That(session.SelectedModule, Is.Not.Null);
            Assert.That(session.SelectedModule!.ModuleId, Is.EqualTo("alpha"));
        }

        [Test]
        public void SelectModuleByIndex_UpdatesSessionState()
        {
            var session = new SessionState();
            var model = new ModuleSelectionModel(session);

            model.SetModules(new[]
            {
                new ModuleDescriptor { ModuleId = "alpha", DisplayName = "Alpha" },
                new ModuleDescriptor { ModuleId = "beta", DisplayName = "Beta" }
            });

            var changed = model.SelectModuleByIndex(1);

            Assert.Multiple(() =>
            {
                Assert.That(changed, Is.True);
                Assert.That(session.SelectedModule, Is.Not.Null);
                Assert.That(session.SelectedModule!.ModuleId, Is.EqualTo("beta"));
            });
        }

        [Test]
        public void SetModules_PreservesExistingSelection()
        {
            var selected = new ModuleDescriptor { ModuleId = "beta", DisplayName = "Beta" };
            var session = new SessionState { SelectedModule = selected };
            var model = new ModuleSelectionModel(session);

            model.SetModules(new List<ModuleDescriptor>
            {
                new ModuleDescriptor { ModuleId = "alpha", DisplayName = "Alpha" },
                selected
            });

            Assert.That(session.SelectedModule, Is.Not.Null);
            Assert.That(session.SelectedModule!.ModuleId, Is.EqualTo("beta"));
        }

        [Test]
        public void SelectModule_ReturnsFalse_WhenModuleMissing()
        {
            var session = new SessionState();
            var model = new ModuleSelectionModel(session);
            model.SetModules(new[] { new ModuleDescriptor { ModuleId = "alpha", DisplayName = "Alpha" } });

            var result = model.SelectModule("gamma");

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.False);
                Assert.That(session.SelectedModule!.ModuleId, Is.EqualTo("alpha"));
            });
        }
    }
}
