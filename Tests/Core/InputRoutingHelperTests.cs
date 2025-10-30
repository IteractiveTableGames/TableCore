using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.Input;

namespace TableCore.Tests.Core
{
    [TestFixture]
    public class InputRoutingHelperTests
    {
        [Test]
        public void ResolvePlayerIndex_ReturnsMatchingIndex_ForPointInsideSeat()
        {
            var session = new SessionState();
            session.PlayerProfiles.Add(new PlayerProfile
            {
                Seat = new SeatZone
                {
                    ScreenRegion = new Rect2(0, 0, 200, 100)
                }
            });

            var result = InputRoutingHelper.ResolvePlayerIndex(session, new Vector2(50, 50));

            Assert.That(result, Is.EqualTo(0));
        }

        [Test]
        public void ResolvePlayerIndex_ReturnsNull_WhenPointOutsideAllSeats()
        {
            var session = new SessionState();
            session.PlayerProfiles.Add(new PlayerProfile
            {
                Seat = new SeatZone
                {
                    ScreenRegion = new Rect2(0, 0, 200, 100)
                }
            });

            var result = InputRoutingHelper.ResolvePlayerIndex(session, new Vector2(250, 150));

            Assert.That(result, Is.Null);
        }

        [Test]
        public void ResolvePlayerIndex_ReturnsCorrectIndex_WhenMultiplePlayersPresent()
        {
            var session = new SessionState();
            session.PlayerProfiles.Add(new PlayerProfile
            {
                Seat = new SeatZone
                {
                    ScreenRegion = new Rect2(0, 0, 100, 200)
                }
            });
            session.PlayerProfiles.Add(new PlayerProfile
            {
                Seat = new SeatZone
                {
                    ScreenRegion = new Rect2(150, 0, 100, 200)
                }
            });

            var resultFirst = InputRoutingHelper.ResolvePlayerIndex(session, new Vector2(50, 100));
            var resultSecond = InputRoutingHelper.ResolvePlayerIndex(session, new Vector2(175, 100));

            Assert.Multiple(() =>
            {
                Assert.That(resultFirst, Is.EqualTo(0));
                Assert.That(resultSecond, Is.EqualTo(1));
            });
        }
    }
}
