using System.Collections.Generic;
using Godot;

namespace TableCore.Core
{
    /// <summary>
    /// Represents the display and metadata payload for a single card definition.
    /// </summary>
    public class CardData
    {
        /// <summary>
        /// Gets or sets the unique identifier for the card.
        /// </summary>
        public string CardId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the card title shown to players.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the descriptive text associated with the card.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the texture displayed on the front face of the card.
        /// </summary>
        public Texture2D? FrontImage { get; set; }

        /// <summary>
        /// Gets or sets the texture displayed on the back face of the card.
        /// </summary>
        public Texture2D? BackImage { get; set; }

        /// <summary>
        /// Gets or sets arbitrary metadata that modules can attach to the card definition.
        /// </summary>
        public Dictionary<string, Variant> Metadata { get; set; } = new();
    }
}
