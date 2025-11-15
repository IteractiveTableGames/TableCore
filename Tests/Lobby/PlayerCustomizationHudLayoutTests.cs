#nullable enable

using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Lobby;

namespace TableCore.Tests.Lobby
{
    [TestFixture]
    public class PlayerCustomizationHudLayoutTests
    {
        [Test]
        public void CalculateHudSize_ReturnsDesignSize_WhenSeatIsGenerous()
        {
            var seat = new SeatZone
            {
                Edge = TableEdge.Bottom,
                ScreenRegion = new Rect2(0f, 0f, 600f, 400f)
            };

            var result = PlayerCustomizationHud.CalculateHudSize(seat, waiting: false);

            Assert.Multiple(() =>
            {
                Assert.That(result.X, Is.EqualTo(492f));
                Assert.That(result.Y, Is.EqualTo(258f));
            });
        }

        [Test]
        public void CalculateHudSize_ClampsToSeat_WhenSpaceIsLimited()
        {
            var seat = new SeatZone
            {
                Edge = TableEdge.Top,
                ScreenRegion = new Rect2(100f, 0f, 300f, 220f)
            };

            var result = PlayerCustomizationHud.CalculateHudSize(seat, waiting: false);

            Assert.Multiple(() =>
            {
                Assert.That(result.X, Is.EqualTo(492f));
                Assert.That(result.Y, Is.EqualTo(258f));
            });
        }
    }
}
