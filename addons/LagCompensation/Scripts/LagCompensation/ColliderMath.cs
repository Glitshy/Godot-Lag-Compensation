using Godot;
using System;


namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Math utilities for calulcations involving raycasting.
    /// </summary>
    public static class ColliderMath
    {
        /// <summary>
        /// Get t-value along line defined by point "a" and "b" where the point "p" is closest to the line
        /// </summary>
        /// <returns>t-value which can be used to interpolate between vectors a and b</returns>
        public static float GetTValueAlongLine(Vector3 a, Vector3 b, Vector3 p)
        {
            return -(a - p).Dot(b - a) / (b - a).LengthSquared();
        }

        /// <summary>
        /// Get position on line 1 which is closest to line 2
        /// </summary>
        /// <param name="a1">First point on first line.</param>
        /// <param name="b1">Second point on first line.</param>
        /// <param name="a2">First point on second line.</param>
        /// <param name="b2">Second point on second line.</param>
        /// <returns>Closest position</returns>
        public static Vector3 GetClosestPointAlongTwoLines(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2)
        {
            Vector3 line1 = b1 - a1;
            Vector3 line2 = b2 - a2;
            Vector3 a1ToA2 = a2 - a1;

            float line1LengthSq = line1.LengthSquared();
            float line2LengthSq = line2.LengthSquared();

            if (line1LengthSq < Mathf.Epsilon || line2LengthSq < Mathf.Epsilon)
            {
                // One of the lines is a point
                return a1;
            }

            // Compute the closest points on the infinite lines
            float t = a1ToA2.Dot(line1) / line1LengthSq;

            // Compute closest point on Line 1
            return a1 + t * line1;
        }

        public static void GetClosestPointsBetweenLines(Vector3 P1, Vector3 D1, Vector3 P2, Vector3 D2, out Vector3 closestPointOnLine1, out Vector3 closestPointOnLine2)
        {
            // Vector from P1 to P2
            Vector3 V = P2 - P1;

            // Dot products
            float D1DotD2 = D1.Dot(D2);
            float VDotD1 = V.Dot(D1);
            float VDotD2 = V.Dot(D2);

            // Denominator
            float denominator = 1 - D1DotD2 * D1DotD2;

            // Parameters t and s
            float t = (VDotD2 - D1DotD2 * VDotD1) / denominator;
            float s = (VDotD1 - D1DotD2 * VDotD2) / denominator;

            // Closest points
            closestPointOnLine1 = P1 + t * D1;
            closestPointOnLine2 = P2 + s * D2;
        }

        public static void ClosestPointsOnTwoLines(Vector3 p1, Vector3 d1, Vector3 p2, Vector3 d2, out Vector3 closestPoint1, out Vector3 closestPoint2)
        {
            // Normalize direction vectors
            d1 = d1.Normalized();
            d2 = d2.Normalized();

            Vector3 r = p1 - p2;
            float a = d1.Dot(d1);
            float b = d1.Dot(d2);
            float c = d2.Dot(d2);
            float d = d1.Dot(r);
            float e = d2.Dot(r);
            float denominator = a * c - b * b;

            float s, t;

            if (Math.Abs(denominator) > 1e-6f)
            {
                s = (b * e - c * d) / denominator;
                t = (a * e - b * d) / denominator;
            }
            else
            {
                // Lines are nearly parallel, pick arbitrary values
                s = 0f;
                t = e / c;
            }

            closestPoint1 = p1 + s * d1;
            closestPoint2 = p2 + t * d2;
        }


        /// <summary>
        /// Get square of the closest distance between a point "p" and a line defined by the origin "o" and direction "d"
        /// </summary>
        /// <param name="point"></param>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static float GetSquaredMinimumDistanceBetwenPointAndLine(Vector3 p, Vector3 o, Vector3 d)
        {
            // https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html


            return d.Cross(o - p).LengthSquared() / d.LengthSquared();
        }

        /// <summary>
        /// Return first or second solution to quadratic formula.
        /// </summary>
        /// <param name="a">Constant</param>
        /// <param name="b">Linear</param>
        /// <param name="c">Quadratic</param>
        /// <param name="pos">Positive (true) or negative (false) solution?</param>
        /// <returns></returns>
        public static float QuadForm(float a, float b, float c, bool pos)
        {
            var preRoot = b * b - 4 * a * c;
            if (preRoot < 0)
            {
                return float.NaN;
            }
            else
            {
                var sgn = pos ? 1.0f : -1.0f;
                return (sgn * Mathf.Sqrt(preRoot) - b) / (2.0f * a);
            }
        }
    }

}