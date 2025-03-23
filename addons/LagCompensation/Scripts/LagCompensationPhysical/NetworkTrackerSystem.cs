using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

namespace PG.LagCompensation.Physical
{

    /// <summary>
    /// Move all NetworkTracker components to a position/rotation in the past for raycasting
    /// </summary>
    public static class NetworkTrackerSystem
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
        /// List of all trackes in scene
        /// </summary>
        private static List<NetworkTracker> _simulationObjects = new List<NetworkTracker>();

        /// <summary>
        /// Register nodes upon instantiating them
        /// </summary>
        public static void Add(NetworkTracker item)
        {
            _simulationObjects.Add(item);
        }

        /// <summary>
        /// Un-register nodes when deleting them
        /// </summary>
        public static void Remove(NetworkTracker item)
        {
            _simulationObjects.Remove(item);
        }

        #region Lag Compensation

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
                _simulationObjects[i].SetStateTransform(simulationTime, true);
            }

            action.Call(); // do stuff

            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].ResetStateTransform();
            }
        }

        /// <summary>
        /// Must call SimulateReset() at end of frame!
        /// </summary>
        /// <param name="simulationTime"></param>
        /// <param name="storeTransforms">Store position/rotation in local variable for reversal.</param>
        public static void SimulateStart(double simulationTime, bool storeTransforms = true)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].SetStateTransform(simulationTime, storeTransforms);

                // Important: Colliders for the purpose of Raycasts and Physics are only updated at the regualar process update.
                // To allow us immediately casting, we need to update the transforms on the PhysicsServer
                // Maybe one day it will be possible to update all with a singel function call.
                //
                // Also, might not even work as espected, see: https://github.com/godotengine/godot-proposals/issues/5181
                // Personal testing showed: With standard Godot Physics, no matter if we call this, it will not update. With Jolt Physics, will always update, no matter if we call this.
                //_simulationObjects[i].GetTargetNode.ForceUpdateTransform();

                //(simulationObjects[i].GetTargetNode as CollisionShape3D).Disabled = true; // TEST: This disables the collider from being hit even without calling ForceUpdateTransform()
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

        #endregion

        #region Debug Drawing

        /// <summary>
        /// Draw the colliders at their current positions
        /// </summary>
        public static void DebugDrawColliders(float duration = 5f)
        {
            for (int i = 0; i < _simulationObjects.Count; i++)
            {
                _simulationObjects[i].DebugDrawColliders(duration);
            }

        }

        #endregion
    }


}