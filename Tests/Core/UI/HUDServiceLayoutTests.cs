using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.UI;

namespace TableCore.Tests.Core.UI
{
    [TestFixture]
    public class HUDServiceLayoutTests
    {
        private static readonly Rect2 SeatRegion = new Rect2(128, 256, 320, 180);

        [Test]
        public void ComputeLayout_UsesSeatRegionAndRotation()
        {
            var seat = new SeatZone
            {
                ScreenRegion = SeatRegion,
                RotationDegrees = 180f
            };

            var layout = HUDService.ComputeLayout(seat);

            Assert.Multiple(() =>
            {
                Assert.That(layout.WrapperPosition, Is.EqualTo(SeatRegion.Position));
                Assert.That(layout.WrapperSize, Is.EqualTo(SeatRegion.Size));
                Assert.That(layout.RootPosition, Is.EqualTo(SeatRegion.Size / 2f));
                Assert.That(layout.RootPivot, Is.EqualTo(SeatRegion.Size / 2f));
                Assert.That(layout.RotationDegrees, Is.EqualTo(180f));
            });
        }

        [Test]
        public void FormatFundsLabel_WritesAmount()
        {
            Assert.That(HUDService.FormatFundsLabel(450), Is.EqualTo("Funds: 450"));
        }

        [Test]
        public void FormatHandLabel_ReturnsEmptyWhenNoCards()
        {
            Assert.That(HUDService.FormatHandLabel(null), Is.EqualTo("Hand: (empty)"));
            Assert.That(HUDService.FormatHandLabel(System.Array.Empty<CardData>()), Is.EqualTo("Hand: (empty)"));
        }

        [Test]
        public void FormatHandLabel_ReportsCardCount()
        {
            var cards = new[]
            {
                new CardData { CardId = "card-1" },
                new CardData { CardId = "card-2" },
                new CardData { CardId = "card-3" }
            };

            Assert.That(HUDService.FormatHandLabel(cards), Is.EqualTo("Hand: 3 card(s)"));
        }
    }
}
