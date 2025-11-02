using NUnit.Framework;
using TableCore.Core;
using Godot;
using System;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class PlayerProfileTests
    {
        [Test]
        public void PlayerProfile_Properties_CanBeSetAndGet()
        {
            var playerProfile = new PlayerProfile();
            var playerId = Guid.NewGuid();
            var displayName = "Player1";
            var displayColor = new Color(1,0,0,1); // red
            Texture2D avatar = null; // avoid creating Godot native objects in unit tests
            var seat = new SeatZone();
            var isGameMaster = true;

            playerProfile.PlayerId = playerId;
            playerProfile.DisplayName = displayName;
            playerProfile.DisplayColor = displayColor;
            playerProfile.Avatar = avatar;
            playerProfile.Seat = seat;
            playerProfile.IsGameMaster = isGameMaster;

            Assert.That(playerProfile.PlayerId, Is.EqualTo(playerId));
            Assert.That(playerProfile.DisplayName, Is.EqualTo(displayName));
            Assert.That(playerProfile.DisplayColor, Is.EqualTo(displayColor));
            Assert.That(playerProfile.Avatar, Is.EqualTo(avatar));
            Assert.That(playerProfile.Seat, Is.EqualTo(seat));
            Assert.That(playerProfile.IsGameMaster, Is.EqualTo(isGameMaster));
        }

        [Test]
        public void Clone_ProducesNewInstanceWithSameValues()
        {
            var profile = new PlayerProfile
            {
                PlayerId = Guid.NewGuid(),
                DisplayName = "Explorer",
                DisplayColor = new Color(0.25f, 0.75f, 0.9f),
                Seat = new SeatZone
                {
                    Edge = TableEdge.Left,
                    RotationDegrees = 270f,
                    ScreenRegion = new Rect2(5, 5, 200, 180),
                    AnchorPoint = new Vector2(15, 40)
                },
                IsGameMaster = true
            };

            var clone = profile.Clone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.SameAs(profile));
                Assert.That(clone.PlayerId, Is.EqualTo(profile.PlayerId));
                Assert.That(clone.DisplayName, Is.EqualTo(profile.DisplayName));
                Assert.That(clone.DisplayColor, Is.EqualTo(profile.DisplayColor));
                Assert.That(clone.Seat, Is.Not.SameAs(profile.Seat));
                Assert.That(clone.Seat?.RotationDegrees, Is.EqualTo(profile.Seat?.RotationDegrees));
                Assert.That(clone.IsGameMaster, Is.True);
            });

            if (clone.Seat != null)
            {
                clone.Seat.RotationDegrees = 45f;
            }

            Assert.That(profile.Seat?.RotationDegrees, Is.EqualTo(270f));
        }
    }
}
