using System;
using Godot;
using NUnit.Framework;
using TableCore.Core.Board;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class BoardPrimitivesTests
    {
        [Test]
        public void BoardLocation_Equals_IgnoresCaseAndOffset()
        {
            var first = new BoardLocation("Markers/TileA", new Vector2(1, 2));
            var second = new BoardLocation("markers/tilea", new Vector2(1, 2));

            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.GetHashCode(), Is.EqualTo(second.GetHashCode()));
        }

        [Test]
        public void BoardPath_PreservesInsertionOrder()
        {
            var locations = new[]
            {
                new BoardLocation("A"),
                new BoardLocation("B"),
                new BoardLocation("C")
            };

            var path = new BoardPath(locations);

            Assert.Multiple(() =>
            {
                Assert.That(path.Count, Is.EqualTo(3));
                Assert.That(path[0], Is.EqualTo(locations[0]));
                Assert.That(path[1], Is.EqualTo(locations[1]));
                Assert.That(path[2], Is.EqualTo(locations[2]));
            });
        }

        [Test]
        public void BoardPath_IsEmpty_WhenConstructedWithNoSteps()
        {
            var path = new BoardPath(Array.Empty<BoardLocation>());
            Assert.That(path.IsEmpty, Is.True);
            Assert.That(path.Count, Is.EqualTo(0));
        }
    }
}
