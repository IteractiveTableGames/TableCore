#nullable enable

using System;
using System.Collections.Generic;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Lobby;

namespace TableCore.Tests.Lobby
{
    [TestFixture]
    public class PlayerCustomizationModelTests
    {
        [Test]
        public void Constructor_NormalizesProfile_WhenNullValuesProvided()
        {
            var profile = new PlayerProfile();
            var model = CreateModel(profile);

            Assert.Multiple(() =>
            {
                Assert.That(profile.DisplayName, Is.EqualTo("Player"));
                Assert.That(profile.DisplayColor.HasValue, Is.True);
                Assert.That(model.SelectedColorIndex, Is.EqualTo(0));
                Assert.That(model.SelectedAvatarIndex, Is.EqualTo(0));
            });
        }

        [Test]
        public void SetDisplayName_UpdatesProfile_AndRaisesEvent()
        {
            var profile = new PlayerProfile { DisplayName = "Player 1" };
            var model = CreateModel(profile);
            PlayerProfile? observedProfile = null;
            model.ProfileChanged += p => observedProfile = p;

            model.SetDisplayName("Alice");

            Assert.Multiple(() =>
            {
                Assert.That(profile.DisplayName, Is.EqualTo("Alice"));
                Assert.That(observedProfile, Is.SameAs(profile));
            });
        }

        [Test]
        public void SetDisplayName_AllowsEmptyDuringEditing()
        {
            var profile = new PlayerProfile { DisplayName = "Original" };
            var model = CreateModel(profile);

            model.SetDisplayName("   ");

            Assert.That(profile.DisplayName, Is.EqualTo(string.Empty));
        }

        [Test]
        public void BaselineName_ReturnsInitialDisplayName()
        {
            var profile = new PlayerProfile { DisplayName = "Alpha" };
            var model = CreateModel(profile);

            Assert.That(model.BaselineName, Is.EqualTo("Alpha"));
        }

        [Test]
        public void SetDisplayName_DoesNotRaiseEvent_WhenValueUnchanged()
        {
            var profile = new PlayerProfile { DisplayName = "Static" };
            var model = CreateModel(profile);
            var raised = false;
            model.ProfileChanged += _ => raised = true;

            model.SetDisplayName("Static");

            Assert.That(raised, Is.False);
        }

        [Test]
        public void SelectColor_RaisesEvent_WhenSelectionChanges()
        {
            var profile = new PlayerProfile();
            var model = CreateModel(profile);
            var eventCount = 0;
            model.ProfileChanged += _ => eventCount++;

            model.SelectColor(1);

            Assert.Multiple(() =>
            {
                Assert.That(eventCount, Is.EqualTo(1));
                Assert.That(profile.DisplayColor, Is.EqualTo(GetColors()[1]));
            });
        }

        [Test]
        public void SetDisplayName_TruncatesToMaxLength()
        {
            var profile = new PlayerProfile { DisplayName = "Player 1" };
            var model = CreateModel(profile);
            var longName = new string('X', 60);

            model.SetDisplayName(longName);

            Assert.That(profile.DisplayName, Has.Length.EqualTo(24));
        }

        [Test]
        public void SelectColor_UpdatesProfile_AndSelectionIndex()
        {
            var profile = new PlayerProfile();
            var model = CreateModel(profile);
            PlayerProfile? observed = null;
            model.ProfileChanged += p => observed = p;

            model.SelectColor(2);

            Assert.Multiple(() =>
            {
                Assert.That(model.SelectedColorIndex, Is.EqualTo(2));
                Assert.That(profile.DisplayColor, Is.EqualTo(GetColors()[2]));
                Assert.That(observed, Is.SameAs(profile));
            });
        }

        [Test]
        public void SelectColor_InvalidIndex_Throws()
        {
            var profile = new PlayerProfile();
            var model = CreateModel(profile);

            Assert.That(() => model.SelectColor(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => model.SelectColor(99), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        [Test]
        public void SelectAvatar_UpdatesProfile_AndSelectionIndex()
        {
            var profile = new PlayerProfile();
            var model = CreateModel(profile);

            model.SelectAvatar(1);

            Assert.Multiple(() =>
            {
                Assert.That(model.SelectedAvatarIndex, Is.EqualTo(1));
                Assert.That(profile.Avatar, Is.SameAs(GetAvatars()[1].Texture));
            });
        }

        [Test]
        public void SelectAvatar_InvalidIndex_Throws()
        {
            var profile = new PlayerProfile();
            var model = CreateModel(profile);

            Assert.That(() => model.SelectAvatar(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
            Assert.That(() => model.SelectAvatar(10), Throws.TypeOf<ArgumentOutOfRangeException>());
        }

        private static PlayerCustomizationModel CreateModel(PlayerProfile profile)
        {
            return new PlayerCustomizationModel(profile, GetColors(), GetAvatars());
        }

        private static IReadOnlyList<Color> GetColors()
        {
            return new[]
            {
                new Color(1f, 0f, 0f),
                new Color(0f, 1f, 0f),
                new Color(0f, 0f, 1f)
            };
        }

        private static IReadOnlyList<AvatarOption> GetAvatars()
        {
            return new[]
            {
                new AvatarOption("A", null),
                new AvatarOption("B", null),
                new AvatarOption("C", null)
            };
        }
    }
}
