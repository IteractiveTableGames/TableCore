using System.Collections.Generic;
using NUnit.Framework;
using TableCore.Core.UI;

namespace TableCore.Tests.Core.UI
{
    [TestFixture]
    public class FloatingKeyboardModelTests
    {
        [Test]
        public void ApplyKey_AppendsCharacters_AndNotifiesListeners()
        {
            var model = new FloatingKeyboardModel();
            var pressed = new List<string>();
            var changes = new List<string>();
            model.KeyPressed += pressed.Add;
            model.TextChanged += changes.Add;

            model.ApplyKey("A");
            model.ApplyKey("B");

            Assert.Multiple(() =>
            {
                Assert.That(model.Text, Is.EqualTo("AB"));
                Assert.That(pressed, Is.EqualTo(new[] { "A", "B" }));
                Assert.That(changes, Is.EqualTo(new[] { "A", "AB" }));
            });
        }

        [Test]
        public void ApplyKey_Backspace_RemovesLastCharacter()
        {
            var model = new FloatingKeyboardModel();
            model.SetText("AB");
            var changes = new List<string>();
            model.TextChanged += changes.Add;

            model.ApplyKey(FloatingKeyboardSpecialKeys.Backspace);

            Assert.Multiple(() =>
            {
                Assert.That(model.Text, Is.EqualTo("A"));
                Assert.That(changes, Is.EqualTo(new[] { "A" }));
            });
        }

        [Test]
        public void ApplyKey_Clear_EmitsEmptyText()
        {
            var model = new FloatingKeyboardModel();
            model.SetText("Player");
            var changes = new List<string>();
            model.TextChanged += changes.Add;

            model.ApplyKey(FloatingKeyboardSpecialKeys.Clear);

            Assert.Multiple(() =>
            {
                Assert.That(model.Text, Is.EqualTo(string.Empty));
                Assert.That(changes, Is.EqualTo(new[] { string.Empty }));
            });
        }

        [Test]
        public void ApplyKey_Space_AppendsWhitespace()
        {
            var model = new FloatingKeyboardModel();
            model.SetText("Player");

            model.ApplyKey(FloatingKeyboardSpecialKeys.Space);
            model.ApplyKey("2");

            Assert.That(model.Text, Is.EqualTo("Player 2"));
        }

        [Test]
        public void ApplyKey_Enter_CommitsCurrentText()
        {
            var model = new FloatingKeyboardModel();
            model.SetText("Ready");
            var commits = new List<string>();
            model.TextCommitted += commits.Add;

            model.ApplyKey(FloatingKeyboardSpecialKeys.Enter);

            Assert.Multiple(() =>
            {
                Assert.That(commits, Is.EqualTo(new[] { "Ready" }));
                Assert.That(model.Text, Is.EqualTo("Ready"));
            });
        }

        [Test]
        public void SetText_ReplacesExistingBuffer()
        {
            var model = new FloatingKeyboardModel();
            var changes = new List<string>();
            model.TextChanged += changes.Add;

            model.SetText("Alpha");
            model.SetText("Alpha"); // duplicate should not emit
            model.SetText("Beta");

            Assert.Multiple(() =>
            {
                Assert.That(model.Text, Is.EqualTo("Beta"));
                Assert.That(changes, Is.EqualTo(new[] { "Alpha", "Beta" }));
            });
        }
    }
}
