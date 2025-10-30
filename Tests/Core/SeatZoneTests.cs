using NUnit.Framework;
using TableCore.Core;
using Godot;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class SeatZoneTests
    {
        [Test]
        public void SeatZone_Properties_CanBeSetAndGet()
        {
            var seatZone = new SeatZone();
            var screenRegion = new Rect2(0, 0, 100, 100);
            var edge = TableEdge.Bottom;
            var rotationDegrees = 180.0f;
            var anchorPoint = new Vector2(50, 50);

            seatZone.ScreenRegion = screenRegion;
            seatZone.Edge = edge;
            seatZone.RotationDegrees = rotationDegrees;
            seatZone.AnchorPoint = anchorPoint;

            Assert.That(seatZone.ScreenRegion, Is.EqualTo(screenRegion));
            Assert.That(seatZone.Edge, Is.EqualTo(edge));
            Assert.That(seatZone.RotationDegrees, Is.EqualTo(rotationDegrees));
            Assert.That(seatZone.AnchorPoint, Is.EqualTo(anchorPoint));
        }
    }
}
