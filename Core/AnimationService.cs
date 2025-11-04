using System;
using System.Threading.Tasks;
using Godot;

namespace TableCore.Core
{
    /// <summary>
    /// Provides helper routines for common node animations such as tweens and highlights.
    /// </summary>
    public class AnimationService
    {
        private readonly SceneTree? _sceneTree;

        public AnimationService(SceneTree? sceneTree)
        {
            _sceneTree = sceneTree;
        }

        /// <summary>
        /// Smoothly moves a node between two positions over the requested duration.
        /// Falls back to immediate positioning when tween creation is not possible.
        /// </summary>
        public async Task AnimateMove(Node2D node, Vector2 from, Vector2 to, double durationMs)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            node.Position = from;

            if (!TryCreateTween(out var tween) || durationMs <= 0)
            {
                node.Position = to;
                await Task.CompletedTask;
                return;
            }

            var tweenInstance = tween!;

            tweenInstance.TweenProperty(node, "position", to, durationMs / 1000.0)
                .SetEase(Tween.EaseType.InOut)
                .SetTrans(Tween.TransitionType.Sine);
            await AwaitTween(tweenInstance);
        }

        /// <summary>
        /// Applies a simple bounce effect to the supplied node.
        /// </summary>
        public async Task AnimateBounce(Node2D node, float height = 18f, double durationMs = 180d)
        {
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!TryCreateTween(out var tween) || durationMs <= 0)
            {
                await Task.CompletedTask;
                return;
            }

            var original = node.Position;
            var apex = original + new Vector2(0f, -Mathf.Abs(height));
            var halfDuration = Math.Max(0.01f, (float)(durationMs / 2000.0));

            var tweenInstance = tween!;

            tweenInstance.TweenProperty(node, "position", apex, halfDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            tweenInstance.TweenProperty(node, "position", original, halfDuration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);

            await AwaitTween(tweenInstance);
        }

        /// <summary>
        /// Temporarily highlights a node by animating its modulate color.
        /// </summary>
        public async Task AnimateHighlight(CanvasItem item, Color color, double durationMs)
        {
            if (item is null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var original = item.Modulate;

            if (!TryCreateTween(out var tween) || durationMs <= 0)
            {
                item.Modulate = color;
                item.Modulate = original;
                await Task.CompletedTask;
                return;
            }

            var halfDuration = Math.Max(0.01f, durationMs / 2000.0);
            var tweenInstance = tween!;

            tweenInstance.TweenProperty(item, "modulate", color, halfDuration)
                .SetEase(Tween.EaseType.Out)
                .SetTrans(Tween.TransitionType.Sine);
            tweenInstance.TweenProperty(item, "modulate", original, halfDuration)
                .SetEase(Tween.EaseType.In)
                .SetTrans(Tween.TransitionType.Sine);

            await AwaitTween(tweenInstance);
        }

        private bool TryCreateTween(out Tween? tween)
        {
            tween = _sceneTree?.CreateTween();
            return tween != null;
        }

        private static Task AwaitTween(Tween tween)
        {
            var tcs = new TaskCompletionSource();

            void OnFinished()
            {
                tween.Finished -= OnFinished;
                tcs.TrySetResult();
            }

            tween.Finished += OnFinished;
            return tcs.Task;
        }
    }
}
