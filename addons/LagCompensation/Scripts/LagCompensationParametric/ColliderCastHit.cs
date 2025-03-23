using Godot;
using System;

namespace PG.LagCompensation.Parametric
{

    /// <summary>
    /// Describes position, normal vecotor and distance of a collider cast entry and exit hit
    /// </summary>
    public struct ColliderCastHit
    {
        /// <summary>
        /// Position where the cast enters the collider
        /// </summary>
        public Vector3 entryPoint;
        /// <summary>
        /// Normal vector of the entry
        /// </summary>
        public Vector3 entryNormal;
        /// <summary>
        /// Distance between origin and entry point
        /// </summary>
        public float entryDistance;


        /// <summary>
        /// Position where the cast exits the collider
        /// </summary>
        public Vector3 exitPoint;
        /// <summary>
        /// Normal vector of the exit
        /// </summary>
        public Vector3 exitNormal;
        /// <summary>
        /// Distance between origin and exit point
        /// </summary>
        public float exitDistance;


        /// <summary>
        /// No hit, entryDistance = Mathf.Infinity, exitDistance = Mathf.Infinity
        /// </summary>
        public static ColliderCastHit Zero
        {
            get { return new ColliderCastHit { entryPoint = Vector3.Zero, entryNormal = Vector3.Zero, entryDistance = Mathf.Inf, exitPoint = Vector3.Zero, exitNormal = Vector3.Zero, exitDistance = Mathf.Inf }; }
        }
    }

}