using System.Diagnostics;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;
using static ProjectDawn.Mathematics.math2;
using static Unity.Mathematics.math;
using System;

namespace ProjectDawn.Geometry2D
{
    /// <summary>
    /// Helper class for finding intersection between 2d geometry shapes.
    /// </summary>
    public static partial class ShapeUtility
    {
        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapCircleAndPoint(Circle circle, float2 point)
        {
            return distancesq(circle.Center, point) < circle.Radius * circle.Radius;
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapCircleAndCircle(Circle a, Circle b)
        {
            return distancesq(a.Center, b.Center) < (a.Radius + b.Radius).sq();
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapCircleAndLine(Circle b, Line a)
        {
            return distancesq(a.ClosestPoint(b.Center), b.Center) < b.Radius * b.Radius;
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapRectangleAndPoint(Rectangle rectangle, float2 point)
        {
            return all(rectangle.Min < point & point < rectangle.Max);
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapRectangleAndRectangle(Rectangle a, Rectangle b)
        {
            return all((a.Max > b.Min) & (a.Min < b.Max));
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapRectangleAndCircle(Rectangle rectangle, Circle circle)
        {
            return distancesq(rectangle.ClosestPoint(circle.Center), circle.Center) < circle.Radius * circle.Radius;
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapRectangleAndLine(Rectangle b, Line a)
        {
            // Based on https://stackoverflow.com/questions/99353/how-to-test-if-a-line-segment-intersects-an-axis-aligned-rectange-in-2d
            float2 d = a.Towards;

            float2 min = b.Min;
            float2 max = b.Max;

            float4 h = new float4(
                d.y * min.x - d.x * min.y,
                d.y * min.x - d.x * max.y,
                d.y * max.x - d.x * max.y,
                d.y * max.x - d.x * min.y);

            float det = determinant(a.To, a.From);
            bool4 r = h + det > 0;

            // Are all of the same sign, then the segment definitely misses the box
            if (all(r) || all(!r))
                return false;

            // Check if ends overlap
            if (any(a.From >= max & a.To >= max) | any(a.From <= min & a.To <= min))
                return false;

            return true;
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        public static bool OverlapLineAndLine(Line a, Line b)
        {
            return Intersection(a.ToRay(), b.ToRay(), out float2 t) && all(t > 0 & t < 1);
        }

        /// <summary>
        /// Returns true if shapes surfaces overlap.
        /// </summary>
        [Obsolete("Obsolete from 1.3.0, use ConvexPolygonUtility.ContainsPoint")]
        public static bool OverlapConvexPolygonAndPoint<T>(NativeSlice<float2> points, T tranform, float2 point2)
    where T : unmanaged, ITransformFloat2
        {
            return ConvexPolygonUtility.ContainsPoint(points, tranform, point2);
        }

        public static bool Intersection(Ray ray, Circle circle, out float2 t)
        {
            float2 d = ray.Direction;
            float2 f = ray.Origin - circle.Center;

            // t2 * (d · d) + 2t*( f · d ) + ( f · f - r2 ) = 0

            // Check if has valid direction
            float a = dot(d, d);
            if (a < EPSILON)
            {
                t = 0;
                return false;
            }

            float b = 2 * dot(ray.Origin - circle.Center, d);
            float c = dot(f, f) - circle.Radius * circle.Radius;

            // We only care if discriminant is positive in that case there will be two intersections
            float discriminant = b * b - 4 * a * c;
            if (discriminant <= 0)
            {
                t = 0;
                return false;
            }

            discriminant = sqrt(discriminant);

            t = new float2(-b - discriminant, -b + discriminant) / (2 * a);

            return true;
        }

        public static bool Intersection(Ray ray, Rectangle rectangle, out Line intersection)
        {
            if (Intersection(ray, rectangle, out float2 t))
            {
                intersection = ray.ToLine(t);
                return true;
            }
            intersection = 0;
            return false;
        }

        public static bool Intersection(Ray ray, Rectangle rectangle, out float2 t)
        {
            RectangleLines(rectangle, out Line lineA, out Line lineB, out Line lineC, out Line lineD);

            if (Intersection(ray, lineA, out t.x))
            {
                if (Intersection(ray, lineB, out t.y))
                    return true;
                if (Intersection(ray, lineC, out t.y))
                    return true;
                if (Intersection(ray, lineD, out t.y))
                    return true;
                return true;
            }

            if (Intersection(ray, lineB, out t.x))
            {
                if (Intersection(ray, lineC, out t.y))
                    return true;
                if (Intersection(ray, lineD, out t.y))
                    return true;
                return true;
            }

            if (Intersection(ray, lineC, out t.y))
            {
                if (Intersection(ray, lineD, out t.y))
                    return true;
                return true;
            }

            t.y = 0;

            if (Intersection(ray, lineD, out t.x))
                return true;

            t.x = 0;

            return false;
        }

        public static bool Intersection(Ray ray, Circle circle, out Line intersection)
        {
            if (Intersection(ray, circle, out float2 t))
            {
                intersection = ray.ToLine(t);
                return true;
            }
            intersection = 0;
            return false;
        }

        public static bool Intersection(Line line, Rectangle rectangle, out float2 pointA, out float2 pointB)
        {
            float2 position = rectangle.Position;
            float2 size = rectangle.Size;

            float2 a = position;
            float2 b = position + new float2(size.x, 0);
            float2 c = position + size;
            float2 d = position + new float2(0, size.y);

            Line lineA = new Line(a, b);
            Line lineB = new Line(b, c);
            Line lineC = new Line(c, d);
            Line lineD = new Line(d, a);

            if (Intersection(line, lineA, out pointA))
            {
                if (Intersection(line, lineB, out pointB))
                    return true;
                if (Intersection(line, lineC, out pointB))
                    return true;
                if (Intersection(line, lineD, out pointB))
                    return true;
                return true;
            }

            if (Intersection(line, lineB, out pointA))
            {
                if (Intersection(line, lineC, out pointB))
                    return true;
                if (Intersection(line, lineD, out pointB))
                    return true;
                return true;
            }

            if (Intersection(line, lineC, out pointA))
            {
                if (Intersection(line, lineD, out pointB))
                    return true;
                return true;
            }

            pointB = 0;

            if (Intersection(line, lineD, out pointA))
                return true;

            pointA = 0;

            return false;
        }

        public static bool IntersectionPolygonAndLine<T>(NativeSlice<float2> points, T tranform, Line line, out float2 point)
            where T : unmanaged, ITransformFloat2
        {
            for (int pointIndex = 0; pointIndex < points.Length; ++pointIndex)
            {
                float2 currentPoint = tranform.Transform(points[pointIndex]);
                float2 nextPoint = tranform.Transform(points[(pointIndex + 1) % points.Length]);

                var polygonLine = new Line(currentPoint, nextPoint);
                if (Intersection(polygonLine, line, out point))
                    return true;
            }
            point = 0;
            return false;
        }

        /// <summary>
        /// Finds intersection times of two rays.
        /// Based on https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection.
        /// </summary>
        public static bool Intersection(Ray a, Ray b, out float2 t)
        {
            float2 d = b.Origin - a.Origin;

            // Check if lines are not parallel
            float det = determinant(a.Direction, b.Direction);
            if (abs(det) < math.EPSILON)
            {
                t = 0;
                return false;
            }

            t = new float2(determinant(d, b.Direction), determinant(d, a.Direction)) / det;
            return true;
        }

        public static bool Intersection(Ray a, Line b, out float t)
        {
            if (Intersection(a, b.ToRay(), out float2 u) && u.y >= 0 && u.y <= 1)
            {
                t = u.x;
                return true;
            }
            t = 0;
            return false;
        }

        public static bool Intersection(Line a, Line b, out float2 point)
        {
            Ray rayA = a.ToRay();
            if (Intersection(rayA, b.ToRay(), out float2 t) && all(t > 0 & t < 1))
            {
                point = rayA.GetPoint(t.x);
                return true;
            }
            point = 0;
            return false;
        }

        /// <summary>
        /// Returns minimum distance between shapes.
        /// </summary>
        public static float DistanceRectangleAndRectangle(Rectangle a, Rectangle b)
        {
            float2 halfSizeA = a.Size * 0.5f;
            float2 halfSizeB = b.Size * 0.5f;
            float2 centerA = (a.Position + halfSizeA);
            float2 centerB = (b.Position + halfSizeB);
            float2 distance = abs(centerA - centerB);
            return length(max(distance - (halfSizeA + halfSizeB), 0));
        }

        /// <summary>
        /// Returns minimum distance between shapes.
        /// </summary>
        public static float DistanceCircleAndCircle(Circle a, Circle b)
        {
            return max(distancesq(a.Center, b.Center) - (a.Radius + b.Radius).sq(), 0);
        }

        /// <summary>
        /// Returns minimum distance between shapes.
        /// </summary>
        public static float DistanceRectangleAndCircle(Rectangle a, Circle b)
        {
            return distance(a.ClosestPoint(b.Center), b.Center);
        }

        /// <summary>
        /// Returns rectangle lines.
        /// </summary>
        public static void RectangleLines(Rectangle rectangle, out Line a, out Line b, out Line c, out Line d)
        {
            float2 position = rectangle.Position;
            float2 size = rectangle.Size;

            float2 pointA = position;
            float2 pointB = position + new float2(size.x, 0);
            float2 pointC = position + size;
            float2 pointD = position + new float2(0, size.y);

            a = new Line(pointA, pointB);
            b = new Line(pointB, pointC);
            c = new Line(pointC, pointD);
            d = new Line(pointD, pointA);
        }
    }
}
