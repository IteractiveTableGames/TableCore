using Godot;

namespace TableCore.Lobby
{
    /// <summary>
    /// Represents a selectable avatar option for player customization.
    /// </summary>
    public sealed class AvatarOption
    {
        public AvatarOption(string id, Texture2D? texture)
        {
            Id = id;
            Texture = texture;
        }

        /// <summary>
        /// Gets the unique identifier for the avatar option.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the texture preview used by the avatar option.
        /// </summary>
        public Texture2D? Texture { get; }
    }
}
