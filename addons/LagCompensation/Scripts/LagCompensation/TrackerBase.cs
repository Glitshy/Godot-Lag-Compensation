using Godot;
using System;
using System.Collections.Generic;

namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Abstract clss for all trackers, can store position/oration data and perform bounding sphere parametric raycasts as an initial hit check before using parametic or physical raycasts against the actual shape
    /// </summary>
    public abstract partial class TrackerBase : Node3D
    {
        /// <summary>
        /// Target node, typically the parent node. Should be CollisionShape3D.
        /// </summary>
        public abstract Node3D GetTargetNode { get; }

        private List<(double, TransformFrameData)> _frameData = new List<(double, TransformFrameData)>(); // ValueTuple, which is a value type

        /// <summary>
        /// Maximum length of FrameData List
        /// </summary>
        public abstract int GetHistoryLength { get; }

        /// <summary>
        /// Assigned by 'SetStateTransform()'. Use this position/rotation when doing the bounding sphere check and collider cast check. This means we don't need to override the transform position/rotation for lag compensation.
        /// </summary>
        protected TransformFrameData _cachedPosRot;

        public TransformFrameData GetCachedPosRot => _cachedPosRot;

        /// <summary>
        /// Time corresponding to <c>_cachedPosRot</c>
        /// </summary>
        protected double _cachedTime;

        /// <summary>
        /// This is reset to <c>false</c> whenever <c>_cachedTime</c> and <c>_cachedPosRot</c> are set
        /// </summary>
        protected bool _cachedIsUpToDate;

        /// <summary>
        /// Get Radius of bounding sphere
        /// </summary>
        public abstract float GetBoundingSphereRadius { get; }

        /// <summary>
        /// Get squared Radius of bounding sphere (better performance in some cases)
        /// </summary>
        public abstract float GetBoundingSphereRadiusSquared { get; }


        /// <summary>
        /// Global Quaternion rotation of target node
        /// </summary>
        public Quaternion GlobalQuaternion => GetTargetNode.GlobalBasis.GetRotationQuaternion();

        #region Raycasting



        /// <summary>
        /// Check if a line defined by origin and direction intersects with the bounding sphere of this collider.
        /// </summary>
        /// <returns>True --> intersects, False --> No intersection</returns>
        public bool CheckBoundingSphereLive(Vector3 origin, Vector3 direction)
        {
            return GetBoundingSphereRadiusSquared >= ColliderMath.GetSquaredMinimumDistanceBetwenPointAndLine(GetTargetNode.GlobalPosition, origin, direction);
        }

        /// <summary>
        /// Check if a line defined by origin and direction intersects with the bounding sphere of this collider. Uses cached transform, more performant than overriding transforms.
        /// </summary>
        /// <returns>True --> intersects, False --> No intersection</returns>
        public bool CheckBoundingSphereCached(Vector3 origin, Vector3 direction)
        {
            return GetBoundingSphereRadiusSquared >= ColliderMath.GetSquaredMinimumDistanceBetwenPointAndLine(GetCachedPosRot.position, origin, direction);
        }

        /// <summary>
        /// Check if bounding sphere at current transform is within range
        /// </summary>
        public bool CheckBoundingSphereDistanceLive(Vector3 origin, Vector3 direction, float range)
        {
            float closestDistance = ColliderMath.GetTValueAlongLine(origin, origin + direction, GetTargetNode.GlobalPosition);

            return closestDistance >= -GetBoundingSphereRadius && closestDistance <= range + GetBoundingSphereRadius; // minimum distance larger than negative radius! This allows casts which start within the bounding sphere
        }

        /// <summary>
        /// Check if bounding sphere at cached transform is within range
        /// </summary>
        public bool CheckBoundingSphereDistanceCached(Vector3 origin, Vector3 direction, float range)
        {
            float closestDistance = ColliderMath.GetTValueAlongLine(origin, origin + direction, GetCachedPosRot.position);

            return closestDistance >= -GetBoundingSphereRadius && closestDistance <= range + GetBoundingSphereRadius; // minimum distance larger than negative radius! This allows casts which start within the bounding sphere
        }

        #endregion

        #region Lag Compensation

        /// <summary>
        /// Add postion/rotation with timestamp to list. Call this after doing movement updates!
        /// </summary>
        public void AddFrame(double time)
        {
            if (_frameData.Count >= GetHistoryLength) // remove oldest stored frame
            {
                _frameData.RemoveAt(0);
            }

            if (GetTargetNode == null)
            {
                GD.PrintErr("Target of Tracker is null");
                return;
            }

            _frameData.Add((time, new TransformFrameData(GetTargetNode)));
        }


        /// <summary>
        /// Caches interpolated position and transform at the given time. Call <c>ColliderCastAtCachedPositionRotation()</c> to use this cached pos/rot
        /// </summary>
        /// <param name="simulationTime"></param>
        public void CalculateAndCacheInterpolatedPositionRotation(double simulationTime)
        {
            if (simulationTime == _cachedTime)
            {
                // already cached this time
                return;
            }

            for (int i = _frameData.Count - 1; i >= 0; i--)
            {
                if (_frameData[i].Item1 <= simulationTime) // if the data at [i] is older than the desired simulation time
                {
                    if (i < _frameData.Count - 1) // if there is a newer frame
                    {
                        double fraction = Math.Clamp((simulationTime - _frameData[i].Item1) / (_frameData[i + 1].Item1 - _frameData[i].Item1), 0d, 1d);

                        _cachedPosRot = TransformFrameData.Interpolate(_frameData[i].Item2, _frameData[i + 1].Item2, fraction);
                    }
                    else // there is no newer frame --> interpolate between this 'newest' frame and the current position!
                    {
                        double currentTime = Time.GetTicksUsec() * 1e-6; // TODO: Check if replacing this time function with something else is required

                        double fraction = Math.Clamp((simulationTime - _frameData[i].Item1) / (currentTime - _frameData[i].Item1), 0d, 1d);

                        _cachedPosRot = TransformFrameData.Interpolate(_frameData[i].Item2, new TransformFrameData(GetTargetNode), fraction); // getting current transform and rotation is more performance intensive than cached frame data
                    }
                    _cachedTime = simulationTime;
                    _cachedIsUpToDate = false;
                    return;
                }
            }

            GD.PrintErr("Tracker interpolation failed. Target Node " + GetTargetNode.Name + ", Tracker Node " + this.Name + " and list count " + _frameData.Count);
        }


        #endregion

        #region Debug Draw

        /// <summary>
        /// Draw the collider at the given global position and rotation
        /// </summary>
        public abstract void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false);

        /// <summary>
        /// Draw collider with Debug.DrawLine, for given duration, at the current live position
        /// </summary>
        public virtual void DebugDrawColliderLive(float duration, Color col, bool editorGizmo = false)
        {
            DebugDraw(GetTargetNode.GlobalPosition, GlobalQuaternion, duration, col, editorGizmo);
        }

        /// <summary>
        /// Draw collider with Debug.DrawLine, for given duration, at the currently cached position
        /// </summary>
        public virtual void DebugDrawColliderCached(float duration, Color col)
        {
            DebugDraw(GetCachedPosRot.position, GetCachedPosRot.rotation, duration, col, false);
        }

        /// <summary>
        /// Draw bounding sphere at current transform
        /// </summary>
        public void DebugDrawBoundingSphereLive(float duration, Color col, bool editorGizmo = false)
        {
            ColliderDrawing.DebugDrawSphere(GetTargetNode.GlobalPosition, GlobalQuaternion, GetBoundingSphereRadius, duration, col, editorGizmo);
        }

        /// <summary>
        /// Draw bounding sphere at cached position/rotation
        /// </summary>
        public void DebugDrawBoundingSphereCached(float duration, Color col)
        {
            ColliderDrawing.DebugDrawSphere(GetCachedPosRot.position, GetCachedPosRot.rotation, GetBoundingSphereRadius, duration, col);
        }

        #endregion

    }


}