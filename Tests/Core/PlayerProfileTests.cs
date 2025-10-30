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
    }
}
