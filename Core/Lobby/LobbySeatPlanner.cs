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
            float stripLength,
            float maxShiftFraction,
            IReadOnlyList<float> desiredCenters,
            out List<float> arrangedCenters)
        {
            arrangedCenters = new List<float>(desiredCenters.Count);

            if (desiredCenters.Count == 0)
            {
                return true;
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

            var seatLength = stripLength;
            var totalRequired = seatLength * desiredCenters.Count;

            if (totalRequired > axisLength + 0.001f)
            {
                arrangedCenters.Clear();
                return false;
            }

            var halfLength = seatLength / 2f;
            var minCenter = axisStart + halfLength;
            var maxCenter = axisEnd - halfLength;

            if (minCenter > maxCenter)
            {
                arrangedCenters.Clear();
                return false;
            }

            var candidates = new List<SeatCenterCandidate>(desiredCenters.Count);

            for (var index = 0; index < desiredCenters.Count; index++)
            {
                var clamped = Mathf.Clamp(desiredCenters[index], minCenter, maxCenter);
                candidates.Add(new SeatCenterCandidate(index, clamped));
            }

            candidates.Sort((a, b) =>
            {
                var comparison = a.DesiredCenter.CompareTo(b.DesiredCenter);
                return comparison != 0 ? comparison : a.OriginalIndex.CompareTo(b.OriginalIndex);
            });

            double sum = 0;
            for (var i = 0; i < candidates.Count; i++)
            {
                sum += candidates[i].DesiredCenter - (i * seatLength);
            }

            var baseCenter = (float)(sum / candidates.Count);
            var minBase = minCenter;
            var maxBase = maxCenter - (seatLength * (candidates.Count - 1));

            if (maxBase < minBase)
            {
                arrangedCenters.Clear();
                return false;
            }

            baseCenter = Mathf.Clamp(baseCenter, minBase, maxBase);

            var centersSorted = new float[candidates.Count];
            for (var i = 0; i < candidates.Count; i++)
            {
                centersSorted[i] = baseCenter + (i * seatLength);
            }

            var maxShift = axisLength * Mathf.Clamp(maxShiftFraction, 0f, 1f);

            for (var i = 0; i < candidates.Count; i++)
            {
                var shift = Mathf.Abs(centersSorted[i] - candidates[i].DesiredCenter);
                if (shift > maxShift + 0.001f)
                {
                    arrangedCenters.Clear();
                    return false;
                }
            }

            var centersByOriginalOrder = new float[candidates.Count];

            for (var i = 0; i < candidates.Count; i++)
            {
                centersByOriginalOrder[candidates[i].OriginalIndex] = centersSorted[i];
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

        private readonly struct SeatCenterCandidate
        {
            public SeatCenterCandidate(int originalIndex, float desiredCenter)
            {
                OriginalIndex = originalIndex;
                DesiredCenter = desiredCenter;
            }

            public int OriginalIndex { get; }
            public float DesiredCenter { get; }
        }
    }
}
