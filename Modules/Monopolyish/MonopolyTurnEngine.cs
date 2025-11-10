using System;
using System.Collections.Generic;
using TableCore.Core;

namespace TableCore.Modules.Monopolyish
{
    internal sealed class MonopolyTurnEngine
    {
        private readonly CurrencyBank _bank;
        private readonly CardService _cardService;
        private readonly IReadOnlyList<MonopolyTileDefinition> _tiles;
        private readonly Dictionary<string, Guid> _propertyOwners = new();
        private readonly int _tileCount;
        private readonly int _passingBonus;

        public MonopolyTurnEngine(
            CurrencyBank bank,
            CardService cardService,
            IReadOnlyList<MonopolyTileDefinition> tiles)
        {
            _bank = bank ?? throw new ArgumentNullException(nameof(bank));
            _cardService = cardService ?? throw new ArgumentNullException(nameof(cardService));
            _tiles = tiles ?? throw new ArgumentNullException(nameof(tiles));
            _tileCount = Math.Max(_tiles.Count, 1);
            _passingBonus = ResolvePassingBonus();
        }

        public MonopolyTurnOutcome ResolveTurn(Guid playerId, int startIndex, int steps)
        {
            if (playerId == Guid.Empty)
            {
                throw new ArgumentException("Player identifier must be provided.", nameof(playerId));
            }

            var clampedSteps = Math.Max(0, steps);
            var finalIndex = startIndex + clampedSteps;
            var outcome = new MonopolyTurnOutcome(finalIndex);

            AwardPassingBonus(playerId, startIndex, finalIndex, outcome);

            var tile = _tiles[Normalize(finalIndex)];
            outcome.Tile = tile;

            switch (tile.Type)
            {
                case MonopolyTileType.Start:
                    AwardLandingBonus(playerId, tile, outcome);
                    break;
                case MonopolyTileType.Property:
                    ResolveProperty(tile, playerId, outcome);
                    break;
                case MonopolyTileType.Tax:
                    ResolveTax(tile, playerId, outcome);
                    break;
                case MonopolyTileType.Chance:
                    ResolveChance(tile, playerId, outcome);
                    break;
                case MonopolyTileType.FreeParking:
                    outcome.Events.Add(new NoActionEvent("Enjoying a moment at Free Parking."));
                    break;
            }

            return outcome;
        }

        private void AwardPassingBonus(Guid playerId, int startIndex, int finalIndex, MonopolyTurnOutcome outcome)
        {
            if (_passingBonus <= 0)
            {
                return;
            }

            var passes = Math.Max(0, finalIndex / _tileCount - startIndex / _tileCount);
            if (passes == 0)
            {
                return;
            }

            var total = passes * _passingBonus;
            _bank.Add(playerId, total);
            var reason = passes == 1 ? "Passed Go." : $"Passed Go {passes} times.";
            outcome.Events.Add(new BonusCollectedEvent(reason, total));
        }

        private void AwardLandingBonus(Guid playerId, MonopolyTileDefinition tile, MonopolyTurnOutcome outcome)
        {
            if (tile.PassingBonus <= 0)
            {
                return;
            }

            _bank.Add(playerId, tile.PassingBonus);
            outcome.Events.Add(new BonusCollectedEvent("Landed on Go.", tile.PassingBonus));
        }

        private void ResolveProperty(MonopolyTileDefinition tile, Guid playerId, MonopolyTurnOutcome outcome)
        {
            if (tile.Card is null)
            {
                outcome.Events.Add(new NoActionEvent($"Nothing to do on {tile.DisplayName}."));
                return;
            }

            if (_propertyOwners.TryGetValue(tile.Card.CardId, out var ownerId))
            {
                if (ownerId == playerId)
                {
                    outcome.Events.Add(new NoActionEvent($"You already own {tile.DisplayName}."));
                    return;
                }

                var rent = tile.RentAmount;
                var paidInFull = _bank.Transfer(playerId, ownerId, rent);
                if (!paidInFull)
                {
                    var balance = _bank.GetBalance(playerId);
                    if (balance > 0)
                    {
                        _bank.Transfer(playerId, ownerId, balance);
                        rent = balance;
                    }
                    else
                    {
                        rent = 0;
                    }
                }

                outcome.Events.Add(new RentPaidEvent(tile.DisplayName, ownerId, rent, paidInFull));
                return;
            }

            if (tile.PurchaseCost <= 0)
            {
                outcome.Events.Add(new NoActionEvent($"{tile.DisplayName} is not for sale."));
                return;
            }

            var available = _bank.GetBalance(playerId);
            if (available < tile.PurchaseCost)
            {
                outcome.Events.Add(new NoActionEvent($"Insufficient funds to buy {tile.DisplayName}."));
                return;
            }

            _bank.Add(playerId, -tile.PurchaseCost);
            _cardService.GiveCardToPlayer(playerId, tile.Card);
            _propertyOwners[tile.Card.CardId] = playerId;
            outcome.Events.Add(new PropertyPurchasedEvent(tile.DisplayName, tile.PurchaseCost));
        }

        private void ResolveTax(MonopolyTileDefinition tile, Guid playerId, MonopolyTurnOutcome outcome)
        {
            var amount = Math.Min(tile.TaxAmount, _bank.GetBalance(playerId));
            if (amount <= 0)
            {
                outcome.Events.Add(new NoActionEvent("No tax paid."));
                return;
            }

            _bank.Add(playerId, -amount);
            outcome.Events.Add(new TaxPaidEvent(amount));
        }

        private void ResolveChance(MonopolyTileDefinition tile, Guid playerId, MonopolyTurnOutcome outcome)
        {
            if (tile.ChanceBonus <= 0)
            {
                outcome.Events.Add(new NoActionEvent("Chance card has no effect."));
                return;
            }

            _bank.Add(playerId, tile.ChanceBonus);
            outcome.Events.Add(new BonusCollectedEvent("Chance bonus awarded.", tile.ChanceBonus));
        }

        private int Normalize(int index)
        {
            if (_tileCount == 0)
            {
                return 0;
            }

            var wrapped = index % _tileCount;
            return wrapped < 0 ? wrapped + _tileCount : wrapped;
        }

        private int ResolvePassingBonus()
        {
            foreach (var tile in _tiles)
            {
                if (tile.Type == MonopolyTileType.Start && tile.PassingBonus > 0)
                {
                    return tile.PassingBonus;
                }
            }

            return 0;
        }

        public Guid? GetOwner(MonopolyTileDefinition? tile)
        {
            if (tile?.Card?.CardId is not { Length: > 0 } cardId)
            {
                return null;
            }

            return _propertyOwners.TryGetValue(cardId, out var owner) ? owner : null;
        }
    }

    internal sealed class MonopolyTurnOutcome
    {
        public MonopolyTurnOutcome(int finalIndex)
        {
            FinalIndex = finalIndex;
        }

        public int FinalIndex { get; }
        public MonopolyTileDefinition? Tile { get; internal set; }
        public List<MonopolyTurnEvent> Events { get; } = new();
    }

    internal abstract record MonopolyTurnEvent;

    internal sealed record BonusCollectedEvent(string Reason, int Amount) : MonopolyTurnEvent;

    internal sealed record PropertyPurchasedEvent(string PropertyName, int Cost) : MonopolyTurnEvent;

    internal sealed record RentPaidEvent(string PropertyName, Guid OwnerId, int Amount, bool PaidInFull) : MonopolyTurnEvent;

    internal sealed record TaxPaidEvent(int Amount) : MonopolyTurnEvent;

    internal sealed record NoActionEvent(string Message) : MonopolyTurnEvent;
}
