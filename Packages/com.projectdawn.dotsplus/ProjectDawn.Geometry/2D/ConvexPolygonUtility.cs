using System;
using Unity.Mathematics;
using Unity.Collections;
using System.Collections.Generic;
using math2 = ProjectDawn.Mathematics.math2;
using System.Diagnostics;

namespace ProjectDawn.Geometry2D
{
    public interface ITransformFloat2
    {
        /// <summary>
        /// Returns transformed point.
        /// </summary>
        float2 Transform(float2 point);
    }

    /// <summary>
    /// A static class to contain various convex polygon functions.
    /// All operations that uses points requires points to form convex polygon and be sorted counter clockwise order.
    /// </summary>
    public static partial class ConvexPolygonUtility
    {
        /// <summary>
        /// Returns true if points form counter clockwise convex polygon.
        /// </summary>
        public static bool IsCounterClockwiseConvexPolygon(NativeSlice<float2> points)
        {
            if (points.Length < 3)
                return false;

            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = points[pointIndex];
                float2 nextPoint = points[(pointIndex + 1) % points.Length];
                float2 nextPoint2 = points[(pointIndex + 2) % points.Length];

                float determinant = math2.determinant(nextPoint2 - nextPoint, point - nextPoint);

                if (determinant < 0)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sorts point in counter clockwise.
        /// Polygon points must be convex.
        /// </summary>
        public static void SortCounterClockwise(NativeSlice<float2> points)
        {
            var sortPoint = GetSortPoint(points);
            points.Sort(new PointWithCenterClockwiseSort(sortPoint));
        }

        /// <summary>
        /// Returns centroid of convex polygon.
        /// Polygon points must be convex and sorted counter clockwise.
        /// Based on https://en.wikipedia.org/wiki/Centroid.
        /// </summary>
        public static float2 GetCentroid<TTranform>(NativeSlice<float2> points, TTranform tranform)
            where TTranform : unmanaged, ITransformFloat2
        {
            CheckIsCounterClockwiseConvexPolygon(points);
            int numPoints = points.Length;

            float2 centroid = 0;
            float signedArea = 0;
            for (int pointIndex = 0; pointIndex < numPoints; ++pointIndex)
            {
                float2 point = tranform.Transform(points[pointIndex]);
                float2 nextPoint = tranform.Transform(points[(pointIndex + 1) % points.Length]);

                float determinant = math2.determinant(point, nextPoint);

                signedArea += determinant;
                centroid += (point + nextPoint) * determinant;
            }
            signedArea *= 0.5f;
            centroid *= 1f / (6f * signedArea);

            return centroid;
        }

        /// <summary>
        /// Returns area of convex polygon.
        /// Polygon points must be convex and sorted counter clockwise.
        /// </summary>
        public static float GetArea<TTranform>(NativeSlice<float2> points, TTranform tranform)
            where TTranform : unmanaged, ITransformFloat2
        {
            CheckIsCounterClockwiseConvexPolygon(points);
            float area = 0;
            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = tranform.Transform(points[pointIndex]);
                float2 nextPoint = tranform.Transform(points[(pointIndex + 1) % points.Length]);
                area += (nextPoint.x - point.x) * (nextPoint.y + point.y);
            }
            return -area * 0.5f;
        }

        /// <summary>
        /// Returns minimum rectangle that fully covers shape.
        /// </summary>
        public static Rectangle BoundingRectangle<TTranform>(NativeSlice<float2> points, TTranform tranform)
            where TTranform : unmanaged, ITransformFloat2
        {
            CheckIsCounterClockwiseConvexPolygon(points);
            float2 min = float.MaxValue;
            float2 max = float.MinValue;

            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = tranform.Transform(points[pointIndex]);
                min = math.min(min, point);
                max = math.max(max, point);
            }

            return new Rectangle(min, max - min);
        }

        /// <summary>
        /// Returns maximum circle that is inside the shape.
        /// </summary>
        public static Circle InscribedCircle<TTranform>(NativeSlice<float2> points, TTranform tranform)
            where TTranform : unmanaged, ITransformFloat2
        {
            CheckIsCounterClockwiseConvexPolygon(points);
            float2 centroid = GetCentroid(points, tranform);
            return MaxInscribedCircle(points, tranform, centroid);
        }

        /// <summary>
        /// Returns minimum distance between two convex polygons.
        /// Polygon points must be convex and sorted counter clockwise.
        /// </summary>
        public static bool ContainsPoint<TTranform>(NativeSlice<float2> points, TTranform tranform, float2 point)
            where TTranform : unmanaged, ITransformFloat2
        {
            CheckIsCounterClockwiseConvexPolygon(points);
            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 currentPoint = tranform.Transform(points[pointIndex]);
                float2 nextPoint = tranform.Transform(points[(pointIndex + 1) % points.Length]);

                float determinant = math2.determinant(math.normalizesafe(point - currentPoint), math.normalizesafe(nextPoint - currentPoint));

                if (determinant >= 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns minimum distance between two convex polygons.
        /// Polygon points must be convex and sorted counter clockwise.
        /// </summary>
        public static float Distance<TTranform>(NativeSlice<float2> pointsA, TTranform tranformA, NativeSlice<float2> pointsB, TTranform transformB)
            where TTranform : unmanaged, ITransformFloat2
        {
            CheckIsCounterClockwiseConvexPolygon(pointsA);
            CheckIsCounterClockwiseConvexPolygon(pointsB);
            return math.max(
                GetDistanceFromAToB(pointsA, tranformA, pointsB, transformB),
                GetDistanceFromAToB(pointsB, transformB, pointsA, tranformA));
        }

        static float GetDistanceFromAToB<TTranform>(NativeSlice<float2> pointsA, TTranform transformA, NativeSlice<float2> pointsB, TTranform transformB)
            where TTranform : unmanaged, ITransformFloat2
        {
            // This algorithm based on idea if two polygons projection on any 1D line never intersect.
            // Then they will not intersect in 2D space too.
            // Also it is enough to project on perpendicular lines to polygon.

            float maxDistance = float.MinValue;
            for (int pointIndex = 0; pointIndex < pointsA.Length; ++pointIndex)
            {
                float2 point = transformA.Transform(pointsA[pointIndex]);
                float2 nextPoint = transformA.Transform(pointsA[(pointIndex + 1) % pointsA.Length]);

                float2 direction = math.normalizesafe(nextPoint - point);
                float2 right = math2.perpendicularright(direction);

                float angle = -math2.angle(right);

                float2 rangeA = GetMinMaxXAxisAligned(pointsA, transformA, angle);
                float2 rangeB = GetMinMaxXAxisAligned(pointsB, transformB, angle);

                maxDistance = math.max(maxDistance, Distance(rangeA, rangeB));
            }

            return maxDistance;
        }

        static float2 GetMinMaxXAxisAligned<TTranform>(NativeSlice<float2> points, TTranform tranform, float angle)
            where TTranform : unmanaged, ITransformFloat2
        {
            float min = float.MaxValue;
            float max = float.MinValue;

            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 point = math2.rotate(tranform.Transform(points[pointIndex]), angle);
                min = math.min(min, point.x);
                max = math.max(max, point.x);
            }

            return new float2(min, max);
        }

        static float2 GetSortPoint(NativeSlice<float2> points)
        {
            float2 sum = 0;
            for (int i = 0; i < points.Length; ++i)
            {
                sum += points[i];
            }
            return sum / points.Length;
        }

        static float Distance(float2 rangeA, float2 rangeB)
        {
            float distance = math.abs((rangeA.y + rangeA.x) * 0.5f - (rangeB.y + rangeB.x) * 0.5f);
            float radius = (rangeA.y - rangeA.x) * 0.5f + (rangeB.y - rangeB.x) * 0.5f;
            return math.max(0, distance - radius);
        }

        /// <summary>
        /// Comparer used to sort convext polygon points into counter clockwise.
        /// </summary>
        struct PointWithCenterClockwiseSort : IComparer<float2>
        {
            public float2 Center;

            public PointWithCenterClockwiseSort(float2 center)
            {
                Center = center;
            }

            public int Compare(float2 a, float2 b)
            {
                float theta_a = math2.angle(a - Center);
                float theta_b = math2.angle(b - Center);
                return theta_a < theta_b ? -1 : theta_a > theta_b ? 1 : 0;
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        public static void CheckIsCounterClockwiseConvexPolygon(NativeSlice<float2> points)
        {
            if (!IsCounterClockwiseConvexPolygon(points))
                throw new ArgumentException("Points do not form counter clockwise sorted convex polygon!");
        }
    }
}
