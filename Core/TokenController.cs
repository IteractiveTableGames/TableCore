using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

namespace TableCore.Core
{
    /// <summary>
    /// Represents an animated token that can move, bounce, and highlight on the board.
    /// </summary>
    public partial class TokenController : Node2D
    {
        private AnimatedSprite2D? _animatedSprite;

        public Guid OwnerPlayerId { get; set; }

        [Export]
        public string TokenName { get; set; } = "Token";

        [Export]
        public string MoveAnimationName { get; set; } = "walk";

        [Export]
        public string IdleAnimationName { get; set; } = "idle";

        public AnimationService? AnimationService { get; set; }

        public override void _Ready()
        {
            _animatedSprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        }

        /// <summary>
        /// Plays the supplied path of world-space points, animating between each step.
        /// </summary>
        public async Task PlayMovePath(IEnumerable<Vector2> worldPoints, double stepDurationMs = 250d)
        {
            if (worldPoints is null)
            {
                throw new ArgumentNullException(nameof(worldPoints));
            }

            var points = worldPoints.Where(p => p != Position).ToList();
            if (points.Count == 0)
            {
                return;
            }

            PlayAnimation(MoveAnimationName);

            var animator = AnimationService;
            var current = Position;

            foreach (var target in points)
            {
                if (animator != null)
                {
                    await animator.AnimateMove(this, current, target, stepDurationMs);
                }
                else
                {
                    Position = target;
                }

                current = target;
            }

            PlayAnimation(IdleAnimationName);
        }

        /// <summary>
        /// Performs a quick bounce animation.
        /// </summary>
        public Task BounceAsync(float height = 18f, double durationMs = 180d)
        {
            return AnimationService?.AnimateBounce(this, height, durationMs) ?? Task.CompletedTask;
        }

        /// <summary>
        /// Highlights the token temporarily using the provided color.
        /// </summary>
        public Task HighlightAsync(Color color, double durationMs = 280d)
        {
            return AnimationService?.AnimateHighlight(this, color, durationMs) ?? Task.CompletedTask;
        }

        private void PlayAnimation(string? animationName)
        {
            if (_animatedSprite?.SpriteFrames == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            if (_animatedSprite.SpriteFrames.HasAnimation(animationName))
            {
                _animatedSprite.Play(animationName);
            }
        }
    }
}
