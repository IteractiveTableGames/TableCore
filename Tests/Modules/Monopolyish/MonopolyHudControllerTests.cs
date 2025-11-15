using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.Modules.Monopolyish;
using TableCore.Core.UI;

#nullable enable

namespace TableCore.Tests.Modules.Monopolyish
{
    [TestFixture]
    public sealed class MonopolyHudControllerTests
    {
        private static readonly Rect2 DefaultSeatRegion = new Rect2(0, 0, 480, 220);

        [Test]
        public void Initialize_ConfiguresHudForAllPlayers()
        {
            var playerOne = CreatePlayer("Alice", TableEdge.Bottom);
            var playerTwo = CreatePlayer("Bob", TableEdge.Top);
            var session = new SessionState
            {
                PlayerProfiles = new List<PlayerProfile> { playerOne, playerTwo }
            };

            var hudService = new FakeHudService();
            var viewFactory = new TestHudViewFactory();
            var currencyBank = new CurrencyBank();
            currencyBank.SetBalance(playerOne.PlayerId, 1500);
            currencyBank.SetBalance(playerTwo.PlayerId, 1500);

            var cardService = new CardService();
            cardService.GiveCardToPlayer(playerOne.PlayerId, CreateCard("boardwalk"));
            cardService.GiveCardToPlayer(playerTwo.PlayerId, CreateCard("park-place"));

            var turnManager = new TurnManager();
            turnManager.SetOrder(new[] { playerOne.PlayerId, playerTwo.PlayerId });

            var diceService = new DiceService(new FixedRandom(4, 3));

            using var controller = new MonopolyHudController(session, hudService, currencyBank, cardService, turnManager, diceService, viewFactory);
            controller.Initialize();

            Assert.That(hudService.CreatedPlayerIds, Is.EquivalentTo(new[] { playerOne.PlayerId, playerTwo.PlayerId }));
            Assert.That(hudService.Funds[playerOne.PlayerId], Is.EqualTo(1500));
            Assert.That(hudService.Funds[playerTwo.PlayerId], Is.EqualTo(1500));
            Assert.That(hudService.Hands[playerOne.PlayerId], Has.Count.EqualTo(1));
            Assert.That(hudService.Hands[playerTwo.PlayerId], Has.Count.EqualTo(1));
            Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain("Roll the dice"));
            Assert.That(hudService.Prompts[playerTwo.PlayerId], Does.Contain("Alice"));
            Assert.That(viewFactory.Contexts[playerOne.PlayerId].IsEnabled, Is.True);
        }

        [Test]
        public void BalanceChanged_UpdatesFundsLabel()
        {
            var (controller, hudService, currencyBank, _, _, playerOne, playerTwo, viewFactory) = BuildControllerWithPlayers();

            using (controller)
            {
                controller.Initialize();
                currencyBank.Add(playerOne.PlayerId, 200);

                Assert.That(hudService.Funds[playerOne.PlayerId], Is.EqualTo(1700));
                Assert.That(hudService.Funds[playerTwo.PlayerId], Is.EqualTo(1500));
            }
        }

        [Test]
        public void HandUpdated_UpdatesHandSummary()
        {
            var (controller, hudService, _, cardService, _, playerOne, _, _) = BuildControllerWithPlayers();

            using (controller)
            {
                controller.Initialize();

                cardService.GiveCardToPlayer(playerOne.PlayerId, CreateCard("reading-railroad"));

                Assert.That(hudService.Hands[playerOne.PlayerId], Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void TurnChanges_EnableButtonsForActivePlayerOnly()
        {
            var (controller, hudService, _, _, turnManager, playerOne, playerTwo, viewFactory) = BuildControllerWithPlayers();

            using (controller)
            {
                controller.Initialize();

                var playerOneContext = viewFactory.Contexts[playerOne.PlayerId];
                var playerTwoContext = viewFactory.Contexts[playerTwo.PlayerId];

                Assert.That(playerOneContext.IsEnabled, Is.True);
                Assert.That(playerTwoContext.IsEnabled, Is.False);

                turnManager.AdvanceTurn();

                Assert.That(playerOneContext.IsEnabled, Is.False);
                Assert.That(playerTwoContext.IsEnabled, Is.True);
                Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain(playerTwo.DisplayName));
            }
        }

        [Test]
        public void RollDiceButton_FiresEventAndUpdatesPrompts()
        {
            var (controller, hudService, _, _, _, playerOne, playerTwo, viewFactory) = BuildControllerWithPlayers(diceValues: new[] { 6, 2 });

            IReadOnlyList<int>? rolled = null;
            Guid roller = Guid.Empty;

            using (controller)
            {
                controller.RollDiceRequested += (playerId, results) =>
                {
                    roller = playerId;
                    rolled = results;
                };

                controller.Initialize();

                viewFactory.Contexts[playerOne.PlayerId].InvokeRoll();

                Assert.That(roller, Is.EqualTo(playerOne.PlayerId));
                Assert.That(rolled, Is.Not.Null);
                Assert.That(rolled, Is.EquivalentTo(new[] { 6, 2 }));
                Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain("You rolled"));
                Assert.That(hudService.Prompts[playerTwo.PlayerId], Does.Contain("Alice rolled 8"));
            }
        }

        [Test]
        public void EndTurnButton_FiresEventForActivePlayer()
        {
            var (controller, hudService, _, _, _, playerOne, _, viewFactory) = BuildControllerWithPlayers();

            Guid ended = Guid.Empty;

            using (controller)
            {
                controller.EndTurnRequested += playerId => ended = playerId;
                controller.Initialize();

                viewFactory.Contexts[playerOne.PlayerId].InvokeEndTurn();

                Assert.That(ended, Is.EqualTo(playerOne.PlayerId));
                Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain("Turn complete"));
            }
        }

        private static (MonopolyHudController controller, FakeHudService hudService, CurrencyBank currencyBank, CardService cardService, TurnManager turnManager, PlayerProfile playerOne, PlayerProfile playerTwo, TestHudViewFactory viewFactory) BuildControllerWithPlayers(int[]? diceValues = null)
        {
            var playerOne = CreatePlayer("Alice", TableEdge.Bottom);
            var playerTwo = CreatePlayer("Bob", TableEdge.Top);
            var session = new SessionState
            {
                PlayerProfiles = new List<PlayerProfile> { playerOne, playerTwo }
            };

            var hudService = new FakeHudService();
            var currencyBank = new CurrencyBank();
            currencyBank.SetBalance(playerOne.PlayerId, 1500);
            currencyBank.SetBalance(playerTwo.PlayerId, 1500);

            var cardService = new CardService();
            cardService.GiveCardToPlayer(playerOne.PlayerId, CreateCard("boardwalk"));
            cardService.GiveCardToPlayer(playerTwo.PlayerId, CreateCard("park-place"));

            var turnManager = new TurnManager();
            turnManager.SetOrder(new[] { playerOne.PlayerId, playerTwo.PlayerId });

            var diceSequence = diceValues ?? new[] { 3, 4 };
            var diceService = new DiceService(new FixedRandom(diceSequence));

            var viewFactory = new TestHudViewFactory();
            var controller = new MonopolyHudController(session, hudService, currencyBank, cardService, turnManager, diceService, viewFactory);
            return (controller, hudService, currencyBank, cardService, turnManager, playerOne, playerTwo, viewFactory);
        }

        private static PlayerProfile CreatePlayer(string displayName, TableEdge edge)
        {
            var rotation = edge switch
            {
                TableEdge.Bottom => 0f,
                TableEdge.Right => 90f,
                TableEdge.Top => 180f,
                TableEdge.Left => 270f,
                _ => 0f
            };

            return new PlayerProfile
            {
                PlayerId = Guid.NewGuid(),
                DisplayName = displayName,
                Seat = new SeatZone
                {
                    Edge = edge,
                    RotationDegrees = rotation,
                    ScreenRegion = DefaultSeatRegion,
                    AnchorPoint = DefaultSeatRegion.Position
                }
            };
        }

        private static CardData CreateCard(string id) => new CardData { CardId = id, Title = id };

        private sealed class FakeHudService : IHUDService
        {
            private readonly Dictionary<Guid, FakePlayerHud> _huds = new();

            public List<Guid> CreatedPlayerIds { get; } = new();
            public Dictionary<Guid, int> Funds { get; } = new();
            public Dictionary<Guid, IReadOnlyList<CardData>> Hands { get; } = new();
            public Dictionary<Guid, string> Prompts { get; } = new();

            public IPlayerHUD CreatePlayerHUD(PlayerProfile player)
            {
                var hud = new FakePlayerHud(player.PlayerId);
                _huds[player.PlayerId] = hud;
                CreatedPlayerIds.Add(player.PlayerId);
                return hud;
            }

            public void UpdateFunds(Guid playerId, int newAmount) => Funds[playerId] = newAmount;

            public void UpdateHand(Guid playerId, IReadOnlyList<CardData> cards) => Hands[playerId] = cards ?? Array.Empty<CardData>();

            public void SetPrompt(Guid playerId, string message) => Prompts[playerId] = message;

            public void ConfigureHudPlacement(HudPlacementOptions options)
            {
            }

            public void SetSeatRegionResolver(Func<SeatZone, Rect2>? resolver)
            {
            }

            public FakePlayerHud GetHud(Guid playerId) => _huds[playerId];

            public sealed class FakePlayerHud : IPlayerHUD
            {
                public FakePlayerHud(Guid playerId)
                {
                    PlayerId = playerId;
                }

                public Guid PlayerId { get; }

                public Control GetRootControl() => default!;

                public void AddControl(Node controlNode)
                {
                    // No-op for tests
                }
            }
        }

        private sealed class TestHudViewFactory : IMonopolyHudViewFactory
        {
            public Dictionary<Guid, TestHudContext> Contexts { get; } = new();

            public IMonopolyHudViewContext Create(PlayerProfile player, IPlayerHUD hud, Action rollHandler, Action endHandler)
            {
                var context = new TestHudContext(player, rollHandler, endHandler);
                Contexts[player.PlayerId] = context;
                return context;
            }
        }

        private sealed class TestHudContext : IMonopolyHudViewContext
        {
            private readonly Action _rollHandler;
            private readonly Action _endHandler;

            public TestHudContext(PlayerProfile player, Action rollHandler, Action endHandler)
            {
                PlayerId = player.PlayerId;
                DisplayName = player.DisplayName ?? "Player";
                _rollHandler = rollHandler;
                _endHandler = endHandler;
            }

            public Guid PlayerId { get; }
            public string DisplayName { get; }
            public bool IsEnabled { get; private set; }

            public void SetInteractionEnabled(bool enabled)
            {
                IsEnabled = enabled;
            }

            public void InvokeRoll()
            {
                _rollHandler();
            }

            public void InvokeEndTurn()
            {
                _endHandler();
            }

            public void Dispose()
            {
            }
        }

        private sealed class FixedRandom : Random
        {
            private readonly Queue<int> _values;

            public FixedRandom(IEnumerable<int> values)
            {
                _values = new Queue<int>(values ?? Array.Empty<int>());
            }

            public FixedRandom(params int[] values)
            {
                _values = new Queue<int>(values ?? Array.Empty<int>());
            }

            public override int Next(int minValue, int maxValue)
            {
                if (_values.Count == 0)
                {
                    return minValue;
                }

                var value = _values.Dequeue();
                if (value < minValue)
                {
                    return minValue;
                }

                if (value >= maxValue)
                {
                    return maxValue - 1;
                }

                return value;
            }

            public override int Next(int maxValue) => Next(0, maxValue);
        }
    }
}
