using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Godot;
using PG.LagCompensation.Base;
using PG.LagCompensation.Parametric;

namespace PG.LagCompensation.Hybrid
{

    /// <summary>
    /// Do a parametric racast against bounding spheres and then move all HybridTracker components to a position/rotation in the past for raycasting
    /// </summary>
    public static class HybridTrackerSystem
    {
        /// <summary>
        /// Number of frames to save before removing oldest frames from list
        /// </summary>
        private static int _frameHistoryLength = 40;
        public static int GetFrameHistoryLength => _frameHistoryLength;
        public static int SetFrameHistoryLength { set => _frameHistoryLength = value; }

        /// <summary>
        /// Interval (in seconds) between adding frames to list
        /// </summary>
        private static float _storeInterval = 0.2f;
        public static float GetStoreInterval => _storeInterval;
        public static float SetStoreInterval { set => _storeInterval = value; }


        /// <summary>
        /// List of all trackers in scene
        /// </summary>
        private static List<HybridTrackerCollection> _simulationObjects = new List<HybridTrackerCollection>();

        /// <summary>
        /// Register nodes upon instantiating them
        /// </summary>
        public static void Add(HybridTrackerCollection item)
        {
            _simulationObjects.Add(item);
        }

        /// <summary>
        /// Un-register nodes when deleting them
        /// </summary>
        public static void Remove(HybridTrackerCollection item)
        {
            _simulationObjects.Remove(item);
        }

        #region Lag Compensation

        /// <summary>
        /// Globally add postion/rotation with timestamp to all collections and managed colliders. Call this after doing movement updates.
        /// </summary>
        public static void AddFrameGlobal(double time)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].AddFrameAll(time);
            }
        }

        /// <summary>
        /// Revert position/rotation to the values at the given time, execute the given action and reset position/rotation back to live values.
        /// TODO: Allow passing a node reference and a function handle to execute between moving and reverting the nodes.
        /// </summary>
        /// <param name="simulationTime"></param>
        /// <param name="action">Action linked to method to execute while colliders are at the position/rotation in the past. Must take no arguments and return void.</param>
        public static void SimulateExecuteReset(double simulationTime, Callable action)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].CalculateAndCacheInterpolatedPositionRotation(simulationTime);
                _simulationObjects[i].InterpolateFully();
                _simulationObjects[i].SetStateTransformToCached();
            }

            action.Call(); // do stuff

            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].ResetStateTransform();
            }
        }

        /// <summary>
        /// At first only interpolate position/rotation for bounding sphere tests. Also disables all collision shapes.
        /// Must call SimulateReset() at end of frame!
        /// </summary>
        /// <param name="simulationTime"></param>
        public static void SimulateStart(double simulationTime)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].CalculateAndCacheInterpolatedPositionRotation(simulationTime);

                _simulationObjects[i].SetCollisionShapeDisabled(true); // make all collision shapes disabled. They will be re-enabled if RaycastPrepare finds a bounding sphere intersection
            }
        }

        /// <summary>
        /// Interpolate and set all transforms. Less performant than working with <c>SimulateStart</c> + <c>RaycastPrepare</c>.
        /// Must call SimulateReset() at end of frame!
        /// </summary>
        /// <param name="simulationTime"></param>
        public static void SimulateFully(double simulationTime)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].CalculateAndCacheInterpolatedPositionRotation(simulationTime);
                _simulationObjects[i].InterpolateFully();
                _simulationObjects[i].SetStateTransformToCached();
            }
        }

        /// <summary>
        /// Reset after having used SimlateStart() this frame
        /// </summary>
        public static void SimulateReset()
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].ResetStateTransform();
            }
        }

        /// <summary>
        /// Check if bounding spheres of any collider intersects and only then update the physical collision shape transform. Check cached postion/rotation. Call <c>Simulate()</c> first. 
        /// </summary>
        /// <param name="origin">Origin of raycast.</param>
        /// <param name="direction">Direction of raycast.</param>
        /// <param name="range">Range of raycast.</param>
        /// <param name="exclude">Exclude this collection from being checked.</param>
        /// <returns>Has anything been hit?</returns>
        public static void RaycastPrepare(Vector3 origin, Vector3 direction, float range, HybridTrackerCollection[] exclude = null)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                if (exclude != null)
                {
                    if (exclude.Contains(_simulationObjects[i]))
                    {
                        continue; // skip this one
                    }
                }

                if (_simulationObjects[i].GetCachedIsUpToDate)
                {
                    // InterpolateFully() and SetStateTransformToCached() already happended for the given time
                    continue;
                }

                if (_simulationObjects[i].CheckBoundingSphereCached(origin, direction))
                {
                    if (_simulationObjects[i].CheckBoundingSphereDistanceCached(origin, direction, range))
                    {
                        // the collection bounding sphere has been intersected --> must do detailed checking of the children
                        _simulationObjects[i].InterpolateFully();
                        _simulationObjects[i].SetStateTransformToCached();
                    }
                }

            }
        }

        #endregion

        #region Debug Drawing

        /// <summary>
        /// Draw the colliders at their current positions
        /// </summary>
        public static void DebugDrawCollidersLive(float duration = 5f)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].DebugDrawColliderLive(duration, new Color("yellow"));

                _simulationObjects[i].DebugDrawBoundingSphereLive(duration, new Color("orange"));
            }

        }

        /// <summary>
        /// Draw the colliders at their cached positions
        /// </summary>
        public static void DebugDrawCollidersCached(float duration = 5f)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].DebugDrawColliderCached(duration, new Color("yellow"));

                _simulationObjects[i].DebugDrawBoundingSphereCached(duration, new Color("orange"));
            }
        }

        #endregion
    }


}