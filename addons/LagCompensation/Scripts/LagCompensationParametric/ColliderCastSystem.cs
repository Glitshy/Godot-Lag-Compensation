using Godot;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace PG.LagCompensation.Parametric
{

    /// <summary>
    /// Used to simulate casting a ray against HitCollider nodes either at the current position or at a position and rotation in the past
    /// </summary>
    public static class ColliderCastSystem
    {
        /// <summary>
        /// Number of frames to store before removing oldest frames from buffer
        /// </summary>
        private static int _frameHistoryLength = 40;
        public static int GetFrameHistoryLength => _frameHistoryLength;
        /// <summary>
        /// Set the maximum number of frame to store before removing the oldest frame from the buffer.
        /// <br></br>
        /// Setting this will re-initlaized all buffers, loosing all stored data
        /// </summary>
        public static int SetFrameHistoryLength
        {
            set
            {
                _frameHistoryLength = value;

                for (int i = 0; i < _simulationObjects.Count; i++)
                {
                    _simulationObjects[i].InitializeBuffers();
                }
            }
        }

        /// <summary>
        /// Interval (in seconds) between adding frames to list
        /// </summary>
        private static float _storeInterval = 0.2f;
        public static float GetStoreInterval => _storeInterval;
        public static float SetStoreInterval { set => _storeInterval = value; }

        /// <summary>
        /// This is called by <see cref="HitColliderGeneric.GetCurrentTime"/>. By default simply calculates <c>Time.GetTicksUsec() * 1e-6</c>.
        /// <br></br>
        /// Overriding this allows changing the current-time logic. Useful when e.g. using a tick system independent of time since statup.
        /// </summary>
        public static Func<double> GetCurrentTime = () => { return Time.GetTicksUsec() * 1e-6; };

        /// <summary>
        /// List of all collections in scene
        /// </summary>
        private static List<HitColliderCollection> _simulationObjects = new List<HitColliderCollection>();

        /// <summary>
        /// Register nodes upon instantiating them
        /// </summary>
        public static void Add(HitColliderCollection item)
        {
            _simulationObjects.Add(item);
        }

        /// <summary>
        /// Un-register nodes when deleting them
        /// </summary>
        public static void Remove(HitColliderCollection item)
        {
            _simulationObjects.Remove(item);
        }


        #region Raycasting

        /// <summary>
        /// Check live/cached postion/rotation. Call <c>Simulate()</c> first. Cast against all HitColliders in the scene, except excluded one (if given).
        /// </summary>
        /// <param name="useCached">false = use live transform / true = use cached transform</param>
        /// <param name="origin">Origin of raycast.</param>
        /// <param name="direction">Direction of raycast.</param>
        /// <param name="range">Range of raycast.</param>
        /// <param name="hit">Hit entry and exit.</param>
        /// <param name="collection">Collection which has been hit. <c>null</c> if nothing has been hit.</param>
        /// <param name="hitColliderIndex">Index of collider in collection. <c>-1</c> if nothing has been hit.</param>
        /// <param name="exclude">Exclude this collection from being checked.</param>
        /// <param name="includeInternal">Include hits where the origin is within the collider</param>
        /// <param name="layerMask">Only considers HitColliders on layers included on this mask. Default value includes all layers.</param>
        /// <returns>Has anything been hit?</returns>
        public static bool ColliderCast(bool useCached, Vector3 origin, Vector3 direction, float range, out ColliderCastHit hit, out HitColliderCollection collection, out int hitColliderIndex, HitColliderCollection[] exclude = null, bool includeInternal = false, uint layerMask = uint.MaxValue)
        {
            hit = ColliderCastHit.Zero;
            ColliderCastHit newHit;
            collection = null;
            hitColliderIndex = -1;
            int newHitColliderIndex;

            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                var simulationObject = _simulationObjects[i];

                if (exclude != null)
                {
                    if (exclude.Contains(simulationObject))
                    {
                        continue; // skip this one
                    }
                }

                // check if any layer of the collection is also on the mask
                uint layers = useCached ? simulationObject.GetCachedLayers : simulationObject.layers;
                if ((layers & layerMask) == 0)
                {
                    continue;
                }

                if (simulationObject.CheckBoundingSphere(useCached, origin, direction))
                {
                    if (simulationObject.CheckBoundingSphereDistance(useCached, origin, direction, range))
                    {
                        if (useCached)
                        {
                            simulationObject.SimulateFully(); // cache the locations/rotations of all managed hitColliders (if it hasn't been done already)
                        }

                        if (simulationObject.ColliderCast(useCached, origin, direction, range, out newHit, out newHitColliderIndex, includeInternal))
                        {
                            if (newHit.entryDistance < hit.entryDistance)
                            {
                                collection = simulationObject;
                                hit = newHit;
                                hitColliderIndex = newHitColliderIndex;
                            }
                        }
                    }
                }

            }


            return hit.entryDistance != Mathf.Inf;
        }

        /// <summary>
        /// Check current transform. Cast against all HitColliders in the scene, except excluded one (if given).
        /// </summary>
        /// <param name="origin">Origin of raycast.</param>
        /// <param name="direction">Direction of raycast.</param>
        /// <param name="range">Range of raycast.</param>
        /// <param name="hit">Hit entry and exit.</param>
        /// <param name="collection">Collection which has been hit. <c>null</c> if nothing has been hit.</param>
        /// <param name="hitColliderIndex">Index of collider in collection. <c>-1</c> if nothing has been hit.</param>
        /// <param name="exclude">Exclude this collection from being checked.</param>
        /// <param name="includeInternal">Include hits where the origin is within the collider</param>
        /// <param name="layerMask">Only considers HitColliders on layers included on this mask. Default value includes all layers.</param>
        /// <returns>Has anything been hit?</returns>
        [ObsoleteAttribute("Use 'ColliderCast' instead.", false)]
        public static bool ColliderCastLive(Vector3 origin, Vector3 direction, float range, out ColliderCastHit hit, out HitColliderCollection collection, out int hitColliderIndex, HitColliderCollection[] exclude = null, bool includeInternal = false, uint layerMask = uint.MaxValue)
        {
            return ColliderCast(false, origin, direction, range, out hit, out collection, out hitColliderIndex, exclude, includeInternal, layerMask);
        }

        /// <summary>
        /// Check cached postion/rotation. Call <c>Simulate()</c> first. Cast against all HitColliders in the scene, except excluded one (if given).
        /// </summary>
        /// <param name="origin">Origin of raycast.</param>
        /// <param name="direction">Direction of raycast.</param>
        /// <param name="range">Range of raycast.</param>
        /// <param name="hit">Hit entry and exit.</param>
        /// <param name="collection">Collection which has been hit. <c>null</c> if nothing has been hit.</param>
        /// <param name="hitColliderIndex">Index of collider in collection. <c>-1</c> if nothing has been hit.</param>
        /// <param name="exclude">Exclude this collection from being checked.</param>
        /// <param name="includeInternal">Include hits where the origin is within the collider</param>
        /// <param name="layerMask">Only considers HitColliders on layers included on this mask. Default value includes all layers.</param>
        /// <returns>Has anything been hit?</returns>
        [ObsoleteAttribute("Use 'ColliderCast' instead.", false)]
        public static bool ColliderCastCached(Vector3 origin, Vector3 direction, float range, out ColliderCastHit hit, out HitColliderCollection collection, out int hitColliderIndex, HitColliderCollection[] exclude = null, bool includeInternal = false, uint layerMask = uint.MaxValue)
        {
            return ColliderCast(true, origin, direction, range, out hit, out collection, out hitColliderIndex, exclude, includeInternal, layerMask);
        }

        #endregion

        #region Lag Compensation

        /// <summary>
        /// Globally add postion/rotation with timestamp to all collections and managed colliders. Call this after doing movement updates.
        /// </summary>
        public static void AddFrameGlobal(double time)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].AddFrame(time);
            }
        }

        /// <summary>
        /// At first only simulate the collection (which is a large sphere collider acting as the bounding sphere for all managed colliders)
        /// </summary>
        /// <param name="simulationTime"></param>
        public static void Simulate(double simulationTime)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].CalculateAndCacheInterpolatedPositionRotation(simulationTime);
            }

        }

        #endregion

        #region Debug Draw

        /// <summary>
        /// Draw the colliders at their current positions
        /// </summary>
        public static void DebugDrawCollidersLive(float duration = 5f)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].DebugDrawColliderLive(duration, new Color("blue"));

                _simulationObjects[i].DebugDrawBoundingSphereLive(duration, new Color("cyan"));
            }
        }

        /// <summary>
        /// Draw the colliders at their cached positions
        /// </summary>
        public static void DebugDrawCollidersCached(float duration = 5f)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].DebugDrawColliderCached(duration, new Color("blue"));

                _simulationObjects[i].DebugDrawBoundingSphereCached(duration, new Color("cyan"));
            }
        }

        #endregion





    }


}