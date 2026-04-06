using Godot;
using System;
using System.Collections.Generic;

namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Abstract clss for all trackers, can store position/oration data and perform bounding sphere parametric raycasts as an initial hit check before using parametic or physical raycasts against the actual shape
    /// </summary>
    [GlobalClass]
    public abstract partial class TrackerBase : Node3D
    {
        /// <summary>
        /// Target node, typically the parent node. Should be CollisionShape3D.
        /// </summary>
        public abstract Node3D GetTargetNode { get; }

        // Using ring buffers instead of a List<(double, TransformFrameData)>, as a list becomes less performant the more items there are when calling RemoveAt(0)
        // intilaize and update these buffers inside of AddFrame()
        protected RingBuffer<double> _bufferTime;
        protected RingBuffer<TransformFrameData> _bufferTransform;

        /// <summary>
        /// Maximum length of FrameData List
        /// </summary>
        public abstract int GetHistoryLength { get; }

        /// <summary>
        /// Assigned by <see cref="InterpolateAndCacheAtIndex"/>.
        /// Use this position/rotation when doing the bounding sphere check and collider cast check.
        /// This means we don't need to override the transform position/rotation for lag compensation.
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
        /// When interpolating to a target time where there is a older frame but no newer frame --> interpolate between this 'newest older' frame and the current position/rotation. 
        /// For that interolation, this time will be used.
        /// By default, this uses <c>Time.GetTicksUsec() * 1e-6</c>.
        /// Depending on the specific implementation, overring this might be neccessary (e.g. when using a tick system independent of time since statup).
        /// </summary>
        protected virtual double GetCurrentTime => Time.GetTicksUsec() * 1e-6;

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
        /// Check if a line defined by origin and direction intersects with the bounding sphere of this collider. Uses live/cached transform (for cached, this is more performant than overriding transforms).
        /// </summary>
        /// <returns>True --> intersects, False --> No intersection</returns>
        public bool CheckBoundingSphere(bool useCached, Vector3 origin, Vector3 direction)
        {
            return GetBoundingSphereRadiusSquared >= ColliderMath.GetSquaredMinimumDistanceBetwenPointAndLine(useCached ? GetCachedPosRot.position : GetTargetNode.GlobalPosition, origin, direction);
        }

        /// <summary>
        /// Check if a line defined by origin and direction intersects with the bounding sphere of this collider.
        /// </summary>
        /// <returns>True --> intersects, False --> No intersection</returns>
        [ObsoleteAttribute("Use 'CheckBoundingSphere' instead.", false)]
        public bool CheckBoundingSphereLive(Vector3 origin, Vector3 direction)
        {
            return CheckBoundingSphere(false, origin, direction);
        }

        /// <summary>
        /// Check if a line defined by origin and direction intersects with the bounding sphere of this collider. Uses cached transform, more performant than overriding transforms.
        /// </summary>
        /// <returns>True --> intersects, False --> No intersection</returns>
        [ObsoleteAttribute("Use 'CheckBoundingSphere' instead.", false)]
        public bool CheckBoundingSphereCached(Vector3 origin, Vector3 direction)
        {
            return CheckBoundingSphere(true, origin, direction);
        }

        /// <summary>
        /// Check if bounding sphere at current/cached transform is within range
        /// </summary>
        /// <param name="enlargeRadius">Add this value to the bounding sphere radius. Useful for doing capsule-sphere-overlap checks.</param>
        /// <returns></returns>
        public bool CheckBoundingSphereDistance(bool useCached, Vector3 origin, Vector3 direction, float range, float enlargeRadius = 0)
        {
            /*
            // old system, which incorrectly warped the bounding sphere into a kind of bounding 'cylinder'
            // while this behaves identically when the sphere center lies exactly on the ray line or the range was much greater than the distance, 
            // but it was giving false positives when the range was approximately the distance to the sphere surface, especially when the ray was approxximately tangential

            float closestDistance = ColliderMath.GetTValueAlongLine(origin, origin + direction, useCached ? GetCachedPosRot.position : GetTargetNode.GlobalPosition);

            return closestDistance >= -GetBoundingSphereRadius && closestDistance <= range + GetBoundingSphereRadius; // minimum distance larger than negative radius! This allows casts which start within the bounding sphere
            */


            Vector3 center = useCached ? GetCachedPosRot.position : GetTargetNode.GlobalPosition;

            float closestDistance = ColliderMath.GetTValueAlongLine(origin, origin + direction, center);

            // calculate the closest point to the sphere along the 'direction' vector, but clamped to the maximum range
            Vector3 closestPoint = origin + direction * Mathf.Clamp(closestDistance, 0f, range);

            // check if the closest point is inside the sphere radius. This allows casts which start within the bounding sphere.
            return (closestPoint - center).LengthSquared() <= (GetBoundingSphereRadius + enlargeRadius) * (GetBoundingSphereRadius + enlargeRadius);
        }

        /// <summary>
        /// Check if bounding sphere at current transform is within range
        /// </summary>
        [ObsoleteAttribute("Use 'CheckBoundingSphereDistance' instead.", false)]
        public bool CheckBoundingSphereDistanceLive(Vector3 origin, Vector3 direction, float range)
        {
            return CheckBoundingSphereDistance(false, origin, direction, range);
        }

        /// <summary>
        /// Check if bounding sphere at cached transform is within range
        /// </summary>
        [ObsoleteAttribute("Use 'CheckBoundingSphereDistance' instead.", false)]
        public bool CheckBoundingSphereDistanceCached(Vector3 origin, Vector3 direction, float range)
        {
            return CheckBoundingSphereDistance(true, origin, direction, range);
        }

        #endregion

        #region Lag Compensation

        /// <summary>
        /// Add postion/rotation with timestamp to list. Call this after doing movement updates!
        /// </summary>
        public virtual void AddFrame(double time)
        {
            // initialize buffers if it hasn't happened yet
            if (_bufferTime == null)
            {
                _bufferTime = new RingBuffer<double>(GetHistoryLength);
            }

            if (_bufferTransform == null)
            {
                _bufferTransform = new RingBuffer<TransformFrameData>(GetHistoryLength);
            }

            // unlike with List<T>, buffer will automatically take care of limiting the length

            if (GetTargetNode == null)
            {
                GD.PrintErr("Target of Tracker is null");
                return;
            }

            _bufferTime.Add(time);
            _bufferTransform.Add(new TransformFrameData(GetTargetNode));
        }


        /// <summary>
        /// Caches interpolated position and transform at the given time. Use "useCached" parameter / "..Cached" method-suffix to use this cached pos/rot.
        /// </summary>
        /// <param name="simulationTime"></param>
        public void CalculateAndCacheInterpolatedPositionRotation(double simulationTime)
        {
            if (simulationTime == _cachedTime)
            {
                // already cached this time
                return;
            }

            for (int i = _bufferTime.Count - 1; i >= 0; i--)
            {
                if (_bufferTime[i] <= simulationTime) // if the data at [i] is older than the desired simulation time
                {
                    double fraction;
                    if (i < _bufferTime.Count - 1) // there is a newer frame --> interpolate between these two
                    {
                        fraction = Math.Clamp((simulationTime - _bufferTime[i]) / (_bufferTime[i + 1] - _bufferTime[i]), 0d, 1d);
                    }
                    else // there is no newer frame --> interpolate between this 'newest' frame and the current time
                    {
                        fraction = Math.Clamp((simulationTime - _bufferTime[i]) / (GetCurrentTime - _bufferTime[i]), 0d, 1d);
                    }

                    InterpolateAndCacheAtIndex(i, fraction);

                    _cachedTime = simulationTime;
                    _cachedIsUpToDate = false;
                    return;
                }
            }

            GD.PrintErr("Tracker interpolation failed. Target Node " + GetTargetNode.Name + ", Tracker Node " + this.Name + " and buffer count " + _bufferTime.Count);
        }

        /// <summary>
        /// Caches interpolated position and transform between the given index and index+1. Will use live transform if index+1 would exceed length of list. 
        /// Can be overridden to interpolate additional parameters, e.g. layers or scale
        /// </summary>
        protected virtual void InterpolateAndCacheAtIndex(int olderIndex, double t)
        {
            if (olderIndex < _bufferTransform.Count - 1) // if there is a newer frame
            {
                _cachedPosRot = TransformFrameData.Interpolate(_bufferTransform[olderIndex], _bufferTransform[olderIndex + 1], t);
            }
            else // there is no newer frame --> interpolate between this 'newest' frame and the current position!
            {
                // Note: getting current transform and rotation is more performance intensive than cached frame data
                _cachedPosRot = TransformFrameData.Interpolate(_bufferTransform[olderIndex], new TransformFrameData(GetTargetNode), t);
            }
        }

        /// <summary>
        /// (Re)Initialize ring buffers, neccessary after e.g. changing the <see cref="GetHistoryLength"/> value.
        /// Note: This will also clear all values.
        /// </summary>
        public virtual void InitializeBuffers()
        {
            _bufferTime = new RingBuffer<double>(GetHistoryLength);
            _bufferTransform = new RingBuffer<TransformFrameData>(GetHistoryLength);
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