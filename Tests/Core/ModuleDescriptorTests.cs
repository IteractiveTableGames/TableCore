using NUnit.Framework;
using TableCore.Core;
using Godot;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class ModuleDescriptorTests
    {
        [Test]
        public void ModuleDescriptor_Properties_CanBeSetAndGet()
        {
            var moduleDescriptor = new ModuleDescriptor();
            var name = "Test Module";
            var description = "This is a test module.";
            Texture2D icon = null; // avoid Godot native object initialization in unit tests

            moduleDescriptor.Name = name;
            moduleDescriptor.Description = description;
            moduleDescriptor.Icon = icon;

            Assert.That(moduleDescriptor.Name, Is.EqualTo(name));
            Assert.That(moduleDescriptor.Description, Is.EqualTo(description));
            Assert.That(moduleDescriptor.Icon, Is.EqualTo(icon));
        }
    }
}
