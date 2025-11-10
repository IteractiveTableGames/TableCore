using System;
using System.Collections.Generic;
using TableCore.Core;

namespace TableCore.Modules.Monopolyish
{
	internal enum MonopolyTileType
	{
		Start,
		Property,
		Tax,
		FreeParking,
		Chance
	}

	internal sealed record MonopolyTileDefinition
	{
		public int Index { get; init; }
		public MonopolyTileType Type { get; init; }
		public string DisplayName { get; init; } = string.Empty;
		public CardData? Card { get; init; }
		public int PurchaseCost { get; init; }
		public int RentAmount { get; init; }
		public int PassingBonus { get; init; }
		public int TaxAmount { get; init; }
		public int ChanceBonus { get; init; }
	}

	internal static class MonopolyTileLibrary
	{
		private static readonly (string Name, int Cost, int Rent)[] PropertyData =
		{
			("Maple Street", 120, 12),
			("Oak Avenue", 140, 14),
			("Railway Station", 160, 18),
			("Harbor View", 180, 20),
			("Sunset Boulevard", 200, 22),
			("Hillcrest Drive", 220, 24),
			("Museum Row", 240, 26),
			("Lakeside Park", 260, 28),
			("Tech Campus", 280, 30),
			("Skyline Plaza", 300, 32)
		};

		private static readonly int[] TaxValues = { 100, 150, 75 };

		public static IReadOnlyList<MonopolyTileDefinition> CreateDefaultTrack(int tileCount)
		{
			if (tileCount <= 0)
			{
				throw new ArgumentOutOfRangeException(nameof(tileCount));
			}

			var tiles = new List<MonopolyTileDefinition>(tileCount);
			var propertyIndex = 0;
			var taxIndex = 0;

			for (var index = 0; index < tileCount; index++)
			{
				MonopolyTileDefinition definition = index switch
				{
					0 => CreateStartTile(index),
					7 => CreateFreeParking(index),
					5 or 11 => CreateChanceTile(index),
					3 or 9 or 14 => CreateTaxTile(index, TaxValues[taxIndex++ % TaxValues.Length]),
					_ => CreatePropertyTile(index, propertyIndex++)
				};

				tiles.Add(definition);
			}

			return tiles;
		}

		private static MonopolyTileDefinition CreateStartTile(int index)
		{
			return new MonopolyTileDefinition
			{
				Index = index,
				Type = MonopolyTileType.Start,
				DisplayName = "Go",
				PassingBonus = 200
			};
		}

		private static MonopolyTileDefinition CreateChanceTile(int index)
		{
			return new MonopolyTileDefinition
			{
				Index = index,
				Type = MonopolyTileType.Chance,
				DisplayName = "Community Bonus",
				ChanceBonus = 50
			};
		}

		private static MonopolyTileDefinition CreateFreeParking(int index)
		{
			return new MonopolyTileDefinition
			{
				Index = index,
				Type = MonopolyTileType.FreeParking,
				DisplayName = "Free Parking"
			};
		}

		private static MonopolyTileDefinition CreateTaxTile(int index, int amount)
		{
			return new MonopolyTileDefinition
			{
				Index = index,
				Type = MonopolyTileType.Tax,
				DisplayName = "City Tax",
				TaxAmount = amount
			};
		}

		private static MonopolyTileDefinition CreatePropertyTile(int tileIndex, int propertyIndex)
		{
			var data = PropertyData[propertyIndex % PropertyData.Length];
			var cardId = $"monopolyish.property.{propertyIndex:D2}";

			return new MonopolyTileDefinition
			{
				Index = tileIndex,
				Type = MonopolyTileType.Property,
				DisplayName = data.Name,
				PurchaseCost = data.Cost,
				RentAmount = data.Rent,
				Card = new CardData
				{
					CardId = cardId,
					Title = data.Name,
					Description = $"Cost ${data.Cost}, Rent ${data.Rent}"
				}
			};
		}
	}
}
