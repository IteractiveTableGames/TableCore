using System;
using System.Collections.Generic;
using Godot;
using TableCore.Core;

namespace TableCore.Lobby
{
    /// <summary>
    /// Helper methods used by the lobby to convert touch positions into canonical seat data.
    /// </summary>
    public static class LobbySeatPlanner
    {
        /// <summary>
        /// Returns the table edge that is closest to the provided point.
        /// </summary>
        public static TableEdge GetNearestEdge(Vector2 point, Rect2 viewportRect)
        {
            var distances = new (TableEdge Edge, float Distance)[]
            {
                (TableEdge.Bottom, viewportRect.End.Y - point.Y),
                (TableEdge.Right, viewportRect.End.X - point.X),
                (TableEdge.Top, point.Y - viewportRect.Position.Y),
                (TableEdge.Left, point.X - viewportRect.Position.X)
            };

            var best = distances[0];

            for (var index = 1; index < distances.Length; index++)
            {
                var candidate = distances[index];

                if (candidate.Distance < best.Distance)
                {
                    best = candidate;
                    continue;
                }

                if (Mathf.IsEqualApprox(candidate.Distance, best.Distance) && candidate.Edge < best.Edge)
                {
                    best = candidate;
                }
            }

            return best.Edge;
        }

        /// <summary>
        /// Calculates the distance from the provided point to the nearest screen edge.
        /// </summary>
        public static float GetDistanceToNearestEdge(Vector2 point, Rect2 viewportRect)
        {
            var top = point.Y - viewportRect.Position.Y;
            var bottom = viewportRect.End.Y - point.Y;
            var left = point.X - viewportRect.Position.X;
            var right = viewportRect.End.X - point.X;

            return Mathf.Min(Mathf.Min(top, bottom), Mathf.Min(left, right));
        }

        /// <summary>
        /// Returns a <see cref="SeatZone"/> configured for the requested edge.
        /// The resulting HUD strip hugs the requested edge and is centered near the provided anchor point.
        /// </summary>
        public static SeatZone CreateSeatZone(
            TableEdge edge,
            Rect2 viewportRect,
            float stripThickness,
            float stripLength,
            Vector2 anchorPoint)
        {
            var clampedThickness = GetClampedThickness(edge, viewportRect, stripThickness);
            var clampedLength = GetClampedLength(edge, viewportRect, stripLength);
            var projectedAnchor = ProjectPointToEdge(anchorPoint, edge, viewportRect);

            var screenRegion = CreateScreenRegion(edge, viewportRect, clampedThickness, clampedLength, projectedAnchor);

            var rotationDegrees = edge switch
            {
                TableEdge.Bottom => 0f,
                TableEdge.Right => 270f,
                TableEdge.Top => 180f,
                TableEdge.Left => 90f,
                _ => 0f
            };

            return new SeatZone
            {
                Edge = edge,
                ScreenRegion = screenRegion,
                RotationDegrees = rotationDegrees,
                AnchorPoint = projectedAnchor
            };
        }

        private static float GetClampedThickness(TableEdge edge, Rect2 viewportRect, float requestedThickness)
        {
            var maxThickness = edge is TableEdge.Left or TableEdge.Right
                ? viewportRect.Size.X
                : viewportRect.Size.Y;

            return Mathf.Clamp(requestedThickness, 1f, maxThickness);
        }

        private static float GetClampedLength(TableEdge edge, Rect2 viewportRect, float requestedLength)
        {
            var maxLength = edge is TableEdge.Top or TableEdge.Bottom
                ? viewportRect.Size.X
                : viewportRect.Size.Y;

            return Mathf.Clamp(requestedLength, 1f, maxLength);
        }

        private static Vector2 ProjectPointToEdge(Vector2 point, TableEdge edge, Rect2 viewportRect)
        {
            var clampedX = Mathf.Clamp(point.X, viewportRect.Position.X, viewportRect.End.X);
            var clampedY = Mathf.Clamp(point.Y, viewportRect.Position.Y, viewportRect.End.Y);

            return edge switch
            {
                TableEdge.Bottom => new Vector2(clampedX, viewportRect.End.Y),
                TableEdge.Top => new Vector2(clampedX, viewportRect.Position.Y),
                TableEdge.Left => new Vector2(viewportRect.Position.X, clampedY),
                TableEdge.Right => new Vector2(viewportRect.End.X, clampedY),
                _ => point
            };
        }

        private static Rect2 CreateScreenRegion(
            TableEdge edge,
            Rect2 viewportRect,
            float thickness,
            float length,
            Vector2 projectedAnchor)
        {
            return edge switch
            {
                TableEdge.Bottom => CreateHorizontalStrip(
                    viewportRect.Position.X,
                    viewportRect.End.X,
                    viewportRect.End.Y - thickness,
                    thickness,
                    length,
                    projectedAnchor.X),
                TableEdge.Top => CreateHorizontalStrip(
                    viewportRect.Position.X,
                    viewportRect.End.X,
                    viewportRect.Position.Y,
                    thickness,
                    length,
                    projectedAnchor.X),
                TableEdge.Left => CreateVerticalStrip(
                    viewportRect.Position.Y,
                    viewportRect.End.Y,
                    viewportRect.Position.X,
                    thickness,
                    length,
                    projectedAnchor.Y),
                TableEdge.Right => CreateVerticalStrip(
                    viewportRect.Position.Y,
                    viewportRect.End.Y,
                    viewportRect.End.X - thickness,
                    thickness,
                    length,
                    projectedAnchor.Y),
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unsupported table edge value.")
            };
        }

        private static Rect2 CreateHorizontalStrip(
            float viewportStartX,
            float viewportEndX,
            float stripY,
            float thickness,
            float length,
            float anchorX)
        {
            var minX = viewportStartX;
            var maxX = viewportEndX - length;
            var clampedStart = Mathf.Clamp(anchorX - (length / 2f), minX, maxX);

            return new Rect2(clampedStart, stripY, length, thickness);
        }

        private static Rect2 CreateVerticalStrip(
            float viewportStartY,
            float viewportEndY,
            float stripX,
            float thickness,
            float length,
            float anchorY)
        {
            var minY = viewportStartY;
            var maxY = viewportEndY - length;
            var clampedStart = Mathf.Clamp(anchorY - (length / 2f), minY, maxY);

            return new Rect2(stripX, clampedStart, thickness, length);
        }

        public static bool TryArrangeSeatCenters(
            TableEdge edge,
            Rect2 viewportRect,
            IReadOnlyList<float> seatLengths,
            IReadOnlyList<float> desiredCenters,
            out List<float> arrangedCenters)
        {
            arrangedCenters = new List<float>(desiredCenters.Count);

            if (desiredCenters.Count == 0)
            {
                return true;
            }

            if (seatLengths.Count != desiredCenters.Count)
            {
                arrangedCenters.Clear();
                return false;
            }

            var axisStart = edge is TableEdge.Top or TableEdge.Bottom
                ? viewportRect.Position.X
                : viewportRect.Position.Y;
            var axisEnd = edge is TableEdge.Top or TableEdge.Bottom
                ? viewportRect.End.X
                : viewportRect.End.Y;
            var axisLength = axisEnd - axisStart;

            if (axisLength <= 0f)
            {
                arrangedCenters.Clear();
                return false;
            }

            var totalRequired = 0f;
            for (var index = 0; index < seatLengths.Count; index++)
            {
                totalRequired += Mathf.Max(1f, seatLengths[index]);
            }

            if (totalRequired > axisLength + 0.001f)
            {
                arrangedCenters.Clear();
                return false;
            }

            var candidates = new List<SeatCenterCandidate>(desiredCenters.Count);

            for (var index = 0; index < desiredCenters.Count; index++)
            {
                var length = Mathf.Max(1f, seatLengths[index]);
                var half = length / 2f;
                var minCenter = axisStart + half;
                var maxCenter = axisEnd - half;
                if (minCenter > maxCenter)
                {
                    arrangedCenters.Clear();
                    return false;
                }

                var clampedCenter = Mathf.Clamp(desiredCenters[index], minCenter, maxCenter);
                candidates.Add(new SeatCenterCandidate(index, clampedCenter, length, half, minCenter, maxCenter));
            }

            candidates.Sort((a, b) =>
            {
                var comparison = a.DesiredCenter.CompareTo(b.DesiredCenter);
                return comparison != 0 ? comparison : a.OriginalIndex.CompareTo(b.OriginalIndex);
            });

            var count = candidates.Count;
            var centers = new float[count];
            var minCenters = new float[count];
            var maxCenters = new float[count];
            var halfLengths = new float[count];

            for (var i = 0; i < count; i++)
            {
                minCenters[i] = candidates[i].MinCenter;
                maxCenters[i] = candidates[i].MaxCenter;
                halfLengths[i] = candidates[i].HalfLength;
                centers[i] = Mathf.Clamp(candidates[i].DesiredCenter, minCenters[i], maxCenters[i]);
            }

            AdjustCentersForward(centers, minCenters, maxCenters, halfLengths);
            AdjustCentersBackward(centers, minCenters, maxCenters, halfLengths);
            AdjustCentersForward(centers, minCenters, maxCenters, halfLengths);
            AdjustCentersBackward(centers, minCenters, maxCenters, halfLengths);

            if (centers[^1] > maxCenters[^1])
            {
                var shift = centers[^1] - maxCenters[^1];
                for (var i = 0; i < centers.Length; i++)
                {
                    centers[i] -= shift;
                }
            }

            if (centers[0] < minCenters[0])
            {
                var shift = minCenters[0] - centers[0];
                for (var i = 0; i < centers.Length; i++)
                {
                    centers[i] += shift;
                }
            }

            for (var i = 1; i < centers.Length; i++)
            {
                var required = halfLengths[i - 1] + halfLengths[i];
                if (centers[i] < centers[i - 1] + required - 0.001f)
                {
                    arrangedCenters.Clear();
                    return false;
                }
            }

            var centersByOriginalOrder = new float[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                centersByOriginalOrder[candidates[i].OriginalIndex] = centers[i];
            }

            arrangedCenters = new List<float>(centersByOriginalOrder);
            return true;
        }

        public static SeatZone CreateSeatZoneFromAxisCenter(
            TableEdge edge,
            Rect2 viewportRect,
            float stripThickness,
            float stripLength,
            float axisCenter)
        {
            var axisStart = edge is TableEdge.Top or TableEdge.Bottom
                ? viewportRect.Position.X
                : viewportRect.Position.Y;
            var axisEnd = edge is TableEdge.Top or TableEdge.Bottom
                ? viewportRect.End.X
                : viewportRect.End.Y;
            var halfLength = stripLength / 2f;
            var minCenter = axisStart + halfLength;
            var maxCenter = axisEnd - halfLength;
            var clampedCenter = Mathf.Clamp(axisCenter, minCenter, maxCenter);

            var anchor = edge switch
            {
                TableEdge.Bottom => new Vector2(clampedCenter, viewportRect.End.Y),
                TableEdge.Top => new Vector2(clampedCenter, viewportRect.Position.Y),
                TableEdge.Left => new Vector2(viewportRect.Position.X, clampedCenter),
                TableEdge.Right => new Vector2(viewportRect.End.X, clampedCenter),
                _ => throw new ArgumentOutOfRangeException(nameof(edge), edge, "Unsupported table edge value.")
            };

            return CreateSeatZone(edge, viewportRect, stripThickness, stripLength, anchor);
        }

        private static void AdjustCentersForward(
            float[] centers,
            float[] minCenters,
            float[] maxCenters,
            float[] halfLengths)
        {
            centers[0] = Mathf.Clamp(centers[0], minCenters[0], maxCenters[0]);

            for (var i = 1; i < centers.Length; i++)
            {
                var minPosition = centers[i - 1] + halfLengths[i - 1] + halfLengths[i];
                if (centers[i] < minPosition)
                {
                    centers[i] = minPosition;
                }

                centers[i] = Mathf.Clamp(centers[i], minCenters[i], maxCenters[i]);
            }
        }

        private static void AdjustCentersBackward(
            float[] centers,
            float[] minCenters,
            float[] maxCenters,
            float[] halfLengths)
        {
            centers[^1] = Mathf.Clamp(centers[^1], minCenters[^1], maxCenters[^1]);

            for (var i = centers.Length - 2; i >= 0; i--)
            {
                var maxPosition = centers[i + 1] - (halfLengths[i + 1] + halfLengths[i]);
                if (centers[i] > maxPosition)
                {
                    centers[i] = maxPosition;
                }

                centers[i] = Mathf.Clamp(centers[i], minCenters[i], maxCenters[i]);
            }
        }

        private readonly struct SeatCenterCandidate
        {
            public SeatCenterCandidate(int originalIndex, float desiredCenter, float length, float halfLength, float minCenter, float maxCenter)
            {
                OriginalIndex = originalIndex;
                DesiredCenter = desiredCenter;
                Length = length;
                HalfLength = halfLength;
                MinCenter = minCenter;
                MaxCenter = maxCenter;
            }

            public int OriginalIndex { get; }
            public float DesiredCenter { get; }
            public float Length { get; }
            public float HalfLength { get; }
            public float MinCenter { get; }
            public float MaxCenter { get; }
        }
    }
}
