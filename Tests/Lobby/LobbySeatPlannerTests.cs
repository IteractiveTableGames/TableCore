using System.Collections.Generic;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Lobby;

namespace TableCore.Tests.Lobby
{
    [TestFixture]
    public class LobbySeatPlannerTests
    {
        private static readonly Rect2 Viewport = new Rect2(0, 0, 1920, 1080);

        [Test]
        public void GetNearestEdge_ReturnsBottom_WhenPointNearBottomEdge()
        {
            var edge = LobbySeatPlanner.GetNearestEdge(new Vector2(1000, 1075), Viewport);
            Assert.That(edge, Is.EqualTo(TableEdge.Bottom));
        }

        [Test]
        public void GetNearestEdge_ReturnsRight_WhenPointNearRightEdge()
        {
            var edge = LobbySeatPlanner.GetNearestEdge(new Vector2(1905, 400), Viewport);
            Assert.That(edge, Is.EqualTo(TableEdge.Right));
        }

        [Test]
        public void GetNearestEdge_ReturnsTop_WhenPointNearTopEdge()
        {
            var edge = LobbySeatPlanner.GetNearestEdge(new Vector2(960, 10), Viewport);
            Assert.That(edge, Is.EqualTo(TableEdge.Top));
        }

        [Test]
        public void GetNearestEdge_ReturnsLeft_WhenPointNearLeftEdge()
        {
            var edge = LobbySeatPlanner.GetNearestEdge(new Vector2(5, 540), Viewport);
            Assert.That(edge, Is.EqualTo(TableEdge.Left));
        }

        [Test]
        public void GetDistanceToNearestEdge_CalculatesMinimumDistance()
        {
            var distance = LobbySeatPlanner.GetDistanceToNearestEdge(new Vector2(100, 200), Viewport);
            Assert.That(distance, Is.EqualTo(100f));
        }

        [Test]
        public void TryArrangeSeatCenters_MovesNewSeatOnly_WhenTargetsOverlap()
        {
            var desired = new List<float> { 960f, 960f };
            var lengths = new List<float> { 520f, 520f };
            var success = LobbySeatPlanner.TryArrangeSeatCenters(
                TableEdge.Bottom,
                Viewport,
                lengths,
                desired,
                out var arranged);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(arranged, Has.Count.EqualTo(2));
                Assert.That(arranged[0], Is.EqualTo(960f).Within(0.5f));
                Assert.That(arranged[1], Is.EqualTo(1480f).Within(0.5f));
            });
        }

        [Test]
        public void TryArrangeSeatCenters_PreservesSpacing_WhenSeatsDoNotOverlap()
        {
            var desired = new List<float> { 400f, 1500f };
            var lengths = new List<float> { 520f, 520f };
            var success = LobbySeatPlanner.TryArrangeSeatCenters(
                TableEdge.Bottom,
                Viewport,
                lengths,
                desired,
                out var arranged);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(arranged, Has.Count.EqualTo(2));
                Assert.That(arranged[0], Is.EqualTo(400f).Within(0.5f));
                Assert.That(arranged[1], Is.EqualTo(1500f).Within(0.5f));
            });
        }

        [Test]
        public void TryArrangeSeatCenters_MaintainsOrder_WhenCompactionRequired()
        {
            var desired = new List<float> { 960f, 960f };
            var lengths = new List<float> { 520f, 520f };
            var success = LobbySeatPlanner.TryArrangeSeatCenters(
                TableEdge.Bottom,
                Viewport,
                lengths,
                desired,
                out var arranged);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(arranged[0], Is.EqualTo(960f).Within(0.5f));
                Assert.That(arranged[1], Is.EqualTo(1480f).Within(0.5f));
            });
        }

        [Test]
        public void TryArrangeSeatCenters_AllowsMixedSeatLengths()
        {
            var desired = new List<float> { 300f, 900f, 1200f, 1600f };
            var lengths = new List<float> { 520f, 260f, 260f, 520f };
            var success = LobbySeatPlanner.TryArrangeSeatCenters(
                TableEdge.Bottom,
                Viewport,
                lengths,
                desired,
                out var arranged);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(arranged, Has.Count.EqualTo(4));
                Assert.That(arranged[0], Is.EqualTo(300f).Within(0.5f));
                Assert.That(arranged[1], Is.EqualTo(900f).Within(0.5f));
                Assert.That(arranged[2], Is.EqualTo(1200f).Within(0.5f));
                Assert.That(arranged[3], Is.EqualTo(1600f).Within(0.5f));
            });
        }

        [Test]
        public void TryArrangeSeatCenters_Fails_WhenInsufficientSpace()
        {
            var desired = new List<float> { 200f, 600f, 1000f, 1400f };
            var lengths = new List<float> { 600f, 600f, 600f, 600f };
            var success = LobbySeatPlanner.TryArrangeSeatCenters(
                TableEdge.Bottom,
                Viewport,
                lengths,
                desired,
                out _);

            Assert.That(success, Is.False);
        }

        [Test]
        public void TryArrangeSeatCenters_SupportsVerticalEdges()
        {
            var desired = new List<float> { 100f, 800f };
            var lengths = new List<float> { 400f, 400f };
            var success = LobbySeatPlanner.TryArrangeSeatCenters(
                TableEdge.Left,
                Viewport,
                lengths,
                desired,
                out var arranged);

            Assert.Multiple(() =>
            {
                Assert.That(success, Is.True);
                Assert.That(arranged[0], Is.EqualTo(200f).Within(0.5f));
                Assert.That(arranged[1], Is.EqualTo(800f).Within(0.5f));
            });
        }

        [Test]
        public void CreateSeatZone_ProducesCorrectRegionForBottomEdge()
        {
            var seatZone = LobbySeatPlanner.CreateSeatZone(
                TableEdge.Bottom,
                Viewport,
                300f,
                600f,
                new Vector2(100, 900));

            Assert.Multiple(() =>
            {
                Assert.That(seatZone.Edge, Is.EqualTo(TableEdge.Bottom));
                Assert.That(seatZone.RotationDegrees, Is.EqualTo(0f));
                Assert.That(seatZone.ScreenRegion.Position.Y, Is.EqualTo(Viewport.End.Y - 300f).Within(0.001f));
                Assert.That(seatZone.ScreenRegion.Size.Y, Is.EqualTo(300f));
                Assert.That(seatZone.ScreenRegion.Size.X, Is.EqualTo(600f));
            });
        }

        [Test]
        public void CreateSeatZone_ClampsThickness_WhenLargerThanViewport()
        {
            var seatZone = LobbySeatPlanner.CreateSeatZone(
                TableEdge.Top,
                Viewport,
                2000f,
                400f,
                new Vector2(50, 40));
            Assert.That(seatZone.ScreenRegion.Size.Y, Is.EqualTo(Viewport.Size.Y));
        }

        [Test]
        public void CreateSeatZone_ClampsLength_WhenLongerThanViewport()
        {
            var seatZone = LobbySeatPlanner.CreateSeatZone(
                TableEdge.Bottom,
                Viewport,
                200f,
                5000f,
                new Vector2(960, 900));

            Assert.That(seatZone.ScreenRegion.Size.X, Is.EqualTo(Viewport.Size.X));
        }

        [Test]
        public void CreateSeatZone_SetsAnchorPoint()
        {
            var anchor = new Vector2(800, 1060);
            var seatZone = LobbySeatPlanner.CreateSeatZone(
                TableEdge.Bottom,
                Viewport,
                250f,
                400f,
                anchor);

            Assert.Multiple(() =>
            {
                Assert.That(seatZone.AnchorPoint.Y, Is.EqualTo(Viewport.End.Y));
                Assert.That(seatZone.AnchorPoint.X, Is.EqualTo(anchor.X));
            });
        }

        [Test]
        public void CreateSeatZone_CentersStripAroundAnchor()
        {
            var anchor = new Vector2(1500, 400);
            var seatZone = LobbySeatPlanner.CreateSeatZone(
                TableEdge.Right,
                Viewport,
                200f,
                500f,
                anchor);

            Assert.Multiple(() =>
            {
                Assert.That(seatZone.ScreenRegion.Position.X, Is.EqualTo(Viewport.End.X - 200f));
                Assert.That(seatZone.ScreenRegion.Position.Y + 250f, Is.EqualTo(anchor.Y).Within(0.001f));
                Assert.That(seatZone.ScreenRegion.Size.Y, Is.EqualTo(500f));
                Assert.That(seatZone.AnchorPoint.X, Is.EqualTo(Viewport.End.X));
            });
        }
    }
}
