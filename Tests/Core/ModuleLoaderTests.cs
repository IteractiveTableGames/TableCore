using System;
using System.IO;
using NUnit.Framework;
using TableCore.Core.Modules;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class ModuleLoaderTests
    {
        private string _root = string.Empty;
        private ModuleLoader _loader = default!;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "TableCoreModuleTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_root);
            _loader = new ModuleLoader();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }

        [Test]
        public void LoadModules_ReturnsEmpty_WhenRootMissing()
        {
            var modules = _loader.LoadModules(Path.Combine(_root, "Missing"));
            Assert.That(modules, Is.Empty);
        }

        [Test]
        public void LoadModules_ParsesValidManifest()
        {
            var moduleDir = Path.Combine(_root, "SampleModule");
            Directory.CreateDirectory(moduleDir);

            File.WriteAllText(Path.Combine(moduleDir, "module.json"),
                """
                {
                  "moduleId": "sample.module",
                  "displayName": "Sample Module",
                  "summary": "An example module used for tests.",
                  "minPlayers": 2,
                  "maxPlayers": 4,
                  "icon": "icon.png",
                  "entryScene": "SampleModule.tscn",
                  "capabilities": {
                    "supportsHotSeat": true
                  }
                }
                """);

            File.WriteAllBytes(Path.Combine(moduleDir, "icon.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            File.WriteAllText(Path.Combine(moduleDir, "SampleModule.tscn"), string.Empty);

            var modules = _loader.LoadModules(_root);

            Assert.That(modules, Has.Count.EqualTo(1));
            var descriptor = modules[0];

            Assert.Multiple(() =>
            {
                Assert.That(descriptor.ModuleId, Is.EqualTo("sample.module"));
                Assert.That(descriptor.DisplayName, Is.EqualTo("Sample Module"));
                Assert.That(descriptor.MinPlayers, Is.EqualTo(2));
                Assert.That(descriptor.MaxPlayers, Is.EqualTo(4));
                Assert.That(descriptor.Summary, Does.Contain("example"));
                Assert.That(descriptor.ModulePath, Is.EqualTo(moduleDir));
                Assert.That(descriptor.IconPath, Is.EqualTo("icon.png"));
                Assert.That(descriptor.EntryScenePath, Is.EqualTo("SampleModule.tscn"));
                Assert.That(descriptor.Capabilities, Contains.Key("supportsHotSeat"));
            });
        }

        [Test]
        public void LoadModules_SkipsInvalidManifest()
        {
            var moduleDir = Path.Combine(_root, "InvalidModule");
            Directory.CreateDirectory(moduleDir);
            File.WriteAllText(Path.Combine(moduleDir, "module.json"), "{ \"displayName\": \"Missing id\" }");

            var modules = _loader.LoadModules(_root);

            Assert.That(modules, Is.Empty);
        }
    }
}
