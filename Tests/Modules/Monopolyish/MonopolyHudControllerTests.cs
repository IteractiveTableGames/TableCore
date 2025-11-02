using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NUnit.Framework;
using TableCore.Core;
using TableCore.Core.Modules.Monopolyish;

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
            var currencyBank = new CurrencyBank();
            currencyBank.SetBalance(playerOne.PlayerId, 1500);
            currencyBank.SetBalance(playerTwo.PlayerId, 1500);

            var cardService = new CardService();
            cardService.GiveCardToPlayer(playerOne.PlayerId, CreateCard("boardwalk"));
            cardService.GiveCardToPlayer(playerTwo.PlayerId, CreateCard("park-place"));

            var turnManager = new TurnManager();
            turnManager.SetOrder(new[] { playerOne.PlayerId, playerTwo.PlayerId });

            var diceService = new DiceService(new FixedRandom(4, 3));

            using var controller = new MonopolyHudController(session, hudService, currencyBank, cardService, turnManager, diceService);
            controller.Initialize();

            Assert.That(hudService.CreatedPlayerIds, Is.EquivalentTo(new[] { playerOne.PlayerId, playerTwo.PlayerId }));
            Assert.That(hudService.Funds[playerOne.PlayerId], Is.EqualTo(1500));
            Assert.That(hudService.Funds[playerTwo.PlayerId], Is.EqualTo(1500));
            Assert.That(hudService.Hands[playerOne.PlayerId], Has.Count.EqualTo(1));
            Assert.That(hudService.Hands[playerTwo.PlayerId], Has.Count.EqualTo(1));
            Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain("Roll the dice"));
            Assert.That(hudService.Prompts[playerTwo.PlayerId], Does.Contain("Alice"));

            var buttons = FindActionButtons(hudService.GetHud(playerOne.PlayerId));
            Assert.That(buttons.roll.Text, Is.EqualTo("Roll Dice"));
            Assert.That(buttons.end.Text, Is.EqualTo("End Turn"));
        }

        [Test]
        public void BalanceChanged_UpdatesFundsLabel()
        {
            var (controller, hudService, currencyBank, _, _, playerOne, playerTwo) = BuildControllerWithPlayers();

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
            var (controller, hudService, _, cardService, _, playerOne, _) = BuildControllerWithPlayers();

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
            var (controller, hudService, _, _, turnManager, playerOne, playerTwo) = BuildControllerWithPlayers();

            using (controller)
            {
                controller.Initialize();

                var playerOneHud = hudService.GetHud(playerOne.PlayerId);
                var playerTwoHud = hudService.GetHud(playerTwo.PlayerId);

                var playerOneButtons = FindActionButtons(playerOneHud);
                var playerTwoButtons = FindActionButtons(playerTwoHud);

                Assert.That(playerOneButtons.roll.Disabled, Is.False);
                Assert.That(playerOneButtons.end.Disabled, Is.False);
                Assert.That(playerTwoButtons.roll.Disabled, Is.True);
                Assert.That(playerTwoButtons.end.Disabled, Is.True);

                turnManager.AdvanceTurn();

                Assert.That(playerOneButtons.roll.Disabled, Is.True);
                Assert.That(playerOneButtons.end.Disabled, Is.True);
                Assert.That(playerTwoButtons.roll.Disabled, Is.False);
                Assert.That(playerTwoButtons.end.Disabled, Is.False);
                Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain(playerTwo.DisplayName));
            }
        }

        [Test]
        public void RollDiceButton_FiresEventAndUpdatesPrompts()
        {
            var (controller, hudService, _, _, _, playerOne, playerTwo) = BuildControllerWithPlayers(diceValues: new[] { 6, 2 });

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

                var buttons = FindActionButtons(hudService.GetHud(playerOne.PlayerId));
                buttons.roll.EmitSignal(BaseButton.SignalName.Pressed);

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
            var (controller, hudService, _, _, _, playerOne, _) = BuildControllerWithPlayers();

            Guid ended = Guid.Empty;

            using (controller)
            {
                controller.EndTurnRequested += playerId => ended = playerId;
                controller.Initialize();

                var buttons = FindActionButtons(hudService.GetHud(playerOne.PlayerId));
                buttons.end.EmitSignal(BaseButton.SignalName.Pressed);

                Assert.That(ended, Is.EqualTo(playerOne.PlayerId));
                Assert.That(hudService.Prompts[playerOne.PlayerId], Does.Contain("Turn complete"));
            }
        }

        private static (MonopolyHudController controller, FakeHudService hudService, CurrencyBank currencyBank, CardService cardService, TurnManager turnManager, PlayerProfile playerOne, PlayerProfile playerTwo) BuildControllerWithPlayers(int[]? diceValues = null)
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

            var controller = new MonopolyHudController(session, hudService, currencyBank, cardService, turnManager, diceService);
            return (controller, hudService, currencyBank, cardService, turnManager, playerOne, playerTwo);
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

        private static (Button roll, Button end) FindActionButtons(FakeHudService.FakePlayerHud hud)
        {
            var queue = new Queue<Node>();
            foreach (Node child in hud.Root.GetChildren())
            {
                queue.Enqueue(child);
            }

            var foundButtons = new List<Button>();
            while (queue.Count > 0)
            {
                var node = queue.Dequeue();
                if (node is Button button)
                {
                    foundButtons.Add(button);
                }

                foreach (Node child in node.GetChildren())
                {
                    queue.Enqueue(child);
                }
            }

            Assert.That(foundButtons, Has.Count.EqualTo(2), "Expected Roll and End buttons to be present.");

            var roll = foundButtons.Single(button => button.Name == "RollDiceButton");
            var end = foundButtons.Single(button => button.Name == "EndTurnButton");
            return (roll, end);
        }

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

            public FakePlayerHud GetHud(Guid playerId) => _huds[playerId];

            public sealed class FakePlayerHud : IPlayerHUD
            {
                private readonly Control _root = new Control();

                public FakePlayerHud(Guid playerId)
                {
                    PlayerId = playerId;
                }

                public Guid PlayerId { get; }

                public Control Root => _root;

                public List<Node> AddedNodes { get; } = new();

                public Control GetRootControl() => _root;

                public void AddControl(Node controlNode)
                {
                    AddedNodes.Add(controlNode);
                    _root.AddChild(controlNode);
                }
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
