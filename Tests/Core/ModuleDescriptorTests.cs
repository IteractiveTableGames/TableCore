using NUnit.Framework;
using TableCore.Core;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class ModuleDescriptorTests
    {
        [Test]
        public void ModuleDescriptor_AllowsPropertyAssignment()
        {
            var descriptor = new ModuleDescriptor
            {
                ModuleId = "sample.module",
                DisplayName = "Sample Module",
                Summary = "Example summary",
                MinPlayers = 2,
                MaxPlayers = 6,
                ModulePath = "C:/Modules/Sample",
                IconPath = "res://Modules/Sample/icon.png",
                EntryScenePath = "res://Modules/Sample/Main.tscn"
            };

            descriptor.Capabilities["supportsHotSeat"] = true;

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.ModuleId, Is.EqualTo("sample.module"));
                Assert.That(descriptor.DisplayName, Is.EqualTo("Sample Module"));
                Assert.That(descriptor.Summary, Is.EqualTo("Example summary"));
                Assert.That(descriptor.MinPlayers, Is.EqualTo(2));
                Assert.That(descriptor.MaxPlayers, Is.EqualTo(6));
                Assert.That(descriptor.ModulePath, Contains.Substring("Sample"));
                Assert.That(descriptor.IconPath, Contains.Substring("icon.png"));
                Assert.That(descriptor.EntryScenePath, Contains.Substring("Main.tscn"));
                Assert.That(descriptor.Capabilities.ContainsKey("supportsHotSeat"), Is.True);
                Assert.That(descriptor.SupportsPlayerCount(4), Is.True);
                Assert.That(descriptor.SupportsPlayerCount(7), Is.False);
            });
        }

        [Test]
        public void Clone_CopiesCapabilitiesAndMetadata()
        {
            var descriptor = new ModuleDescriptor
            {
                ModuleId = "mod.runtime.test",
                DisplayName = "Runtime Test",
                Summary = "Testing module for runtime handoff",
                MinPlayers = 2,
                MaxPlayers = 5,
                ModulePath = "C:/Games/RuntimeTest",
                IconPath = "icon.png",
                EntryScenePath = "Main.tscn"
            };

            descriptor.Capabilities["dice"] = true;
            descriptor.Capabilities["currency"] = "enabled";

            var clone = descriptor.Clone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.SameAs(descriptor));
                Assert.That(clone.ModuleId, Is.EqualTo(descriptor.ModuleId));
                Assert.That(clone.Capabilities, Is.Not.SameAs(descriptor.Capabilities));
                Assert.That(clone.Capabilities["dice"], Is.EqualTo(true));
                Assert.That(clone.Capabilities["currency"], Is.EqualTo("enabled"));
            });

            clone.Capabilities["dice"] = false;
            Assert.That(descriptor.Capabilities["dice"], Is.EqualTo(true));
        }
    }
}
