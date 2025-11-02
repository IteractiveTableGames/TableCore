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

        [Test]
        public void Clone_CreatesIndependentCopy()
        {
            var original = new SeatZone
            {
                ScreenRegion = new Rect2(10, 20, 300, 120),
                Edge = TableEdge.Right,
                RotationDegrees = 90f,
                AnchorPoint = new Vector2(16, 48)
            };

            var clone = original.Clone();

            Assert.Multiple(() =>
            {
                Assert.That(clone, Is.Not.SameAs(original));
                Assert.That(clone.Edge, Is.EqualTo(original.Edge));
                Assert.That(clone.RotationDegrees, Is.EqualTo(original.RotationDegrees));
                Assert.That(clone.ScreenRegion, Is.EqualTo(original.ScreenRegion));
                Assert.That(clone.AnchorPoint, Is.EqualTo(original.AnchorPoint));
            });

            clone.ScreenRegion = new Rect2(0, 0, 1, 1);
            Assert.That(original.ScreenRegion, Is.Not.EqualTo(clone.ScreenRegion));
        }
    }
}
