using Godot;

namespace TableCore.Modules.Monopolyish
{
	public partial class MonopolyTileVisual : Node2D
	{
		private Label _nameLabel = null!;
		private Label _priceLabel = null!;
		private Label _rentLabel = null!;
		private Label _bonusLabel = null!;
		private Label _ownerLabel = null!;
		private ColorRect _ownerIndicator = null!;
		private bool _initialized;
		private MonopolyTileDefinition? _definition;

		public override void _Ready()
		{
			InitializeNodes();
			if (_definition != null)
			{
				ApplyDefinition(_definition);
			}
		}

		internal void SetDefinition(MonopolyTileDefinition definition)
		{
			_definition = definition;
			if (!_initialized)
			{
				return;
			}

			ApplyDefinition(definition);
		}

		internal void UpdateOwner(string ownerName, Color ownerColor, bool owned)
		{
			InitializeNodes();
			_ownerIndicator.Color = owned ? ownerColor : new Color(1f, 1f, 1f, 1f);
		}

		private void ApplyDefinition(MonopolyTileDefinition definition)
		{
			_nameLabel.Text = definition.DisplayName;

			_priceLabel.Visible = definition.PurchaseCost > 0;
			_priceLabel.Text = definition.PurchaseCost > 0 ? $"${definition.PurchaseCost}" : string.Empty;

			_rentLabel.Visible = definition.RentAmount > 0;
			_rentLabel.Text = definition.RentAmount > 0 ? $"${definition.RentAmount}" : string.Empty;

			_bonusLabel.Visible = false;

			switch (definition.Type)
			{
				case MonopolyTileType.Tax when definition.TaxAmount > 0:
					_bonusLabel.Visible = true;
					_bonusLabel.Text = $"Tax ${definition.TaxAmount}";
					break;
				case MonopolyTileType.Chance when definition.ChanceBonus > 0:
					_bonusLabel.Visible = true;
					_bonusLabel.Text = $"+${definition.ChanceBonus} Bonus";
					break;
				case MonopolyTileType.Start when definition.PassingBonus > 0:
					_bonusLabel.Visible = true;
					_bonusLabel.Text = $"+${definition.PassingBonus} / pass";
					break;
				case MonopolyTileType.FreeParking:
					_bonusLabel.Visible = true;
					_bonusLabel.Text = "Free Parking";
					break;
			}
		}

		private void InitializeNodes()
		{
			if (_initialized)
			{
				return;
			}

			_nameLabel = GetNode<Label>("Layout/NameLabel");
			_priceLabel = GetNode<Label>("Layout/PriceLabel");
			_rentLabel = GetNode<Label>("Layout/RentLabel");
			_bonusLabel = GetNode<Label>("Layout/BonusLabel");
			_ownerIndicator = GetNode<ColorRect>("Layout/OwnerIndicator");
			_initialized = true;
		}
	}
}
