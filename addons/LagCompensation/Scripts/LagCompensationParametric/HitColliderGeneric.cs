using System.Collections;
using System.Collections.Generic;
using Godot;
using System;
using PG.LagCompensation.Base;


namespace PG.LagCompensation.Parametric
{

    /// <summary>
    /// Abstract parent class for colliders of parametric raycasts (no physics) with different shapes
    /// </summary>
    public abstract partial class HitColliderGeneric : TrackerBase
    {
        public override Node3D GetTargetNode => this;
        public override int GetHistoryLength => ColliderCastSystem.GetFrameHistoryLength;


        #region Raycasting

        /// <summary>
        /// Cast at current transform. Return true if hit. Outpout hit entry and exit point, normal and distance.
        /// </summary>
        /// <param name="rayOrigin">Ray origin.</param>
        /// <param name="rayDirection">Normalized ray direction.</param>
        /// <param name="range">Maximum range of ray.</param>
        /// <param name="hit">Resulting hit.</param>
        /// <returns>Successfully hit collider?</returns>
        public abstract bool ColliderCastLive(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit);

		/// <summary>
		/// Cast at cached location/rotation. Return true if hit. Outpout hit entry and exit point, normal and distance.
		/// </summary>
		/// <param name="rayOrigin">Ray origin.</param>
		/// <param name="rayDirection">Normalized ray direction.</param>
		/// <param name="range">Maximum range of ray.</param>
		/// <param name="hit">Resulting hit.</param>
		/// <returns>Successfully hit collider?</returns>
		public abstract bool ColliderCastCached(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit);

        /// <summary>
        /// Calculate exit positon and normal vector of intersection of ray with sphere.
        /// </summary>
        /// <param name="c">Sphere center.</param>
        /// <param name="r">Sphere radius.</param>
        /// <param name="o">Ray origin.</param>
        /// <param name="d">Ray direction.</param>
        /// <param name="entryPoint">Intersection entry position.</param>
        /// <param name="entryNormal">Intersection entry normal.</param>
        /// <param name="entryDistance">Intersection entry distance from origin.</param>
        /// <returns>Found intersection?</returns>
        protected static bool ParametricRaycastSphereEntry(Vector3 c, float r, Vector3 o, Vector3 d, out Vector3 entryPoint, out Vector3 entryNormal, out float entryDistance)
        {
            float sol_a = d.X * d.X + d.Y * d.Y + d.Z * d.Z;                                                                                                        // quadratic
            float sol_b = (2 * o.X * d.X - 2 * c.X * d.X) + (2 * o.Y * d.Y - 2 * c.Y * d.Y) + (2 * o.Z * d.Z - 2 * c.Z * d.Z);                                      // linear
            float sol_c = (o.X * o.X + c.X * c.X - 2 * c.X * o.X) + (o.Y * o.Y + c.Y * c.Y - 2 * c.Y * o.Y) + (o.Z * o.Z + c.Z * c.Z - 2 * c.Z * o.Z) - r * r;      // constant

            //float exitDistance = quadForm(sol_a, sol_b, sol_c, true);
            entryDistance = ColliderMath.QuadForm(sol_a, sol_b, sol_c, false);


            if (!float.IsNaN(entryDistance))
            {
                entryPoint = o + entryDistance * d;
                entryNormal = (entryPoint - c).Normalized();

                return true;
            }
            else
            {
                entryPoint = Vector3.Zero;
                entryNormal = Vector3.Forward;

                return false;
            }

        }

        /// <summary>
        /// Calculate exit positon and normal vector of intersection of ray with sphere.
        /// </summary>
        /// <param name="c">Sphere center.</param>
        /// <param name="r">Sphere radius.</param>
        /// <param name="o">Ray origin.</param>
        /// <param name="d">Ray direction.</param>
        /// <param name="exitPoint">Intersection exit position.</param>
        /// <param name="exitNormal">Intersection exit normal.</param>
        /// <param name="exitDistance">Intersection exit distance from origin.</param>
        /// <returns>Found intersection?</returns>
        protected static bool ParametricRaycastSphereExit(Vector3 c, float r, Vector3 o, Vector3 d, out Vector3 exitPoint, out Vector3 exitNormal, out float exitDistance)
        {
            float sol_a = d.X * d.X + d.Y * d.Y + d.Z * d.Z;                                                                                                        // quadratic
            float sol_b = (2 * o.X * d.X - 2 * c.X * d.X) + (2 * o.Y * d.Y - 2 * c.Y * d.Y) + (2 * o.Z * d.Z - 2 * c.Z * d.Z);                                      // linear
            float sol_c = (o.X * o.X + c.X * c.X - 2 * c.X * o.X) + (o.Y * o.Y + c.Y * c.Y - 2 * c.Y * o.Y) + (o.Z * o.Z + c.Z * c.Z - 2 * c.Z * o.Z) - r * r;      // constant

            exitDistance = ColliderMath.QuadForm(sol_a, sol_b, sol_c, true);
            //float entryDistance = quadForm(sol_a, sol_b, sol_c, false);


            if (!float.IsNaN(exitDistance))
            {
                exitPoint = o + exitDistance * d;
                exitNormal = (exitPoint - c).Normalized();

                //Debug.Log("exitDistance " + exitDistance + " entryDistance " + entryDistance);

                return true;
            }
            else
            {
                exitPoint = Vector3.Zero;
                exitNormal = Vector3.Forward;

                return false;
            }

        }

        /// <summary>
        /// Calculate entry and exit positons and normal vectors of intersection of ray with sphere.
        /// </summary>
        /// <param name="c">Sphere center.</param>
        /// <param name="r">Sphere radius.</param>
        /// <param name="o">Ray origin.</param>
        /// <param name="d">Ray direction.</param>
        /// <param name="entryPoint">Intersection entry position.</param>
        /// <param name="entryNormal">Intersection entry normal.</param>
        /// <param name="entryDistance">Intersection entry distance from origin.</param>
        /// <param name="exitPoint">Intersection exit position.</param>
        /// <param name="exitNormal">Intersection exit normal.</param>
        /// <param name="exitDistance">Intersection exit distance from origin.</param>
        /// <returns>Found intersection?</returns>
        protected static bool ParametricRaycastSphereBothSided(Vector3 c, float r, Vector3 o, Vector3 d, out Vector3 entryPoint, out Vector3 entryNormal, out float entryDistance, out Vector3 exitPoint, out Vector3 exitNormal, out float exitDistance)
        {
            float sol_a = d.X * d.X + d.Y * d.Y + d.Z * d.Z;                                                                                                        // quadratic
            float sol_b = (2 * o.X * d.X - 2 * c.X * d.X) + (2 * o.Y * d.Y - 2 * c.Y * d.Y) + (2 * o.Z * d.Z - 2 * c.Z * d.Z);                                      // linear
            float sol_c = (o.X * o.X + c.X * c.X - 2 * c.X * o.X) + (o.Y * o.Y + c.Y * c.Y - 2 * c.Y * o.Y) + (o.Z * o.Z + c.Z * c.Z - 2 * c.Z * o.Z) - r * r;      // constant


            // alternative writing of same formula
            //float sol_a = d.sqrMagnitude; // quadratic
            //Vector3 _helper = 2 * Vector3.Scale(o, d) - 2 * Vector3.Scale(c, d);
            //float sol_b = _helper.x + _helper.y + _helper.z; // linear
            //float sol_c = o.sqrMagnitude + c.sqrMagnitude  - 2 * c.x * o.x - 2 * c.y * o.y - 2 * c.z * o.z - r * r;      // constant


            exitDistance = ColliderMath.QuadForm(sol_a, sol_b, sol_c, true);
            entryDistance = ColliderMath.QuadForm(sol_a, sol_b, sol_c, false);


            if (!float.IsNaN(entryDistance))
            {
                entryPoint = o + entryDistance * d;
                entryNormal = (entryPoint - c) / r; // divide by radius to normalize

                exitPoint = o + exitDistance * d;
                exitNormal = (exitPoint - c) / r;

                //Debug.DrawLine(o, entryPoint, Color.green);
                //Debug.Log("exitDistance " + exitDistance + " entryDistance " + entryDistance);

                return true;
            }
            else
            {
                entryPoint = Vector3.Zero;
                entryNormal = Vector3.Forward;

                exitPoint = Vector3.Zero;
                exitNormal = Vector3.Forward;

                //Debug.DrawLine(o, o + d * 10f, Color.red);

                return false;
            }

        }






        #endregion

        #region Tools

        /// <summary>
        /// Try to set the collider dimensions based on the parent node of type <c>CollisionShape3D</c>
        /// </summary>
        public abstract void TryGetParametersFromPhysicsCollider();


        #endregion


    }

}