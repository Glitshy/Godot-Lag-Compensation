using System;
using System.Collections.Generic;
using Godot;
using PG.LagCompensation.Base;


namespace PG.LagCompensation.Hybrid
{

    /// <summary>
    /// Track and store transforms of the parent <c>Node3D</c> in a list and rewind time for raycast calculations (parametric against the bounding sphere and then physical raycast).
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class HybridTracker : TrackerBase
    {
        /// <summary>
        /// Target node, typically the parent node. Should be CollisionShape3D.
        /// </summary>
        [Export]
        private Node3D _target;
        public override Node3D GetTargetNode => _target;
        public Node3D SetTargetNode { set => _target = value; }

        /// <summary>
        /// Cache this instead of casting every time
        /// </summary>
        private CollisionShape3D _targetCollisionShape;

        public override int GetHistoryLength => HybridTrackerSystem.GetFrameHistoryLength;

        /// <summary>
        /// Last postion and rotation before rewinding
        /// </summary>
        private TransformFrameData _savedFrameData = new TransformFrameData();

        /// <summary>
        /// Radius of bounding sphere. Must be set according to target node.
        /// </summary>
        private float _radius;

        /// <summary>
        /// Get Radius of bounding sphere
        /// </summary>
        public override float GetBoundingSphereRadius => _radius;

        /// <summary>
        /// Get squared Radius of bounding sphere (better performance in some cases)
        /// </summary>
        public override float GetBoundingSphereRadiusSquared => _radius * _radius;

        /// <summary>
        /// Is the target at the cached or live position? If true, it is at the cached position. If false, it is at the live position.
        /// </summary>
        private bool isAtCachedPosition;


        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            //HybridTrackerSystem.Add(this);

            if (_target == null)
            {
                _target = GetParent() as Node3D;
            }

            _targetCollisionShape = _target as CollisionShape3D; // cache only once for performance reasons

            // calculate radius if not already manually set
            if (_radius == 0f && _target != null)
            {
                CollisionShape3D collisionShape = _target as CollisionShape3D;

                if (collisionShape == null)
                {
                    // target needs to be CollisionShape3D for radius calculation
                    return;
                }

                if (collisionShape.Shape is CapsuleShape3D)
                {
                    CapsuleShape3D capsule = collisionShape.Shape as CapsuleShape3D;
                    _radius = capsule.Height * 0.5f;
                }
                else if (collisionShape.Shape is BoxShape3D)
                {
                    BoxShape3D box = collisionShape.Shape as BoxShape3D;
                    _radius = (box.Size * 0.5f).Length();
                }
                else if (collisionShape.Shape is SphereShape3D)
                {
                    SphereShape3D sphere = collisionShape.Shape as SphereShape3D;
                    _radius = sphere.Radius;
                }
            }
        }

        // OnDestroy
        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                //HybridTrackerSystem.Remove(this);
            }
        }

        #region Lag Compensation


        /// <summary>
        /// Set transform corresponding to cached value from <c>CalculateAndCacheInterpolatedPositionRotation</c>. 
        /// Can be called multiple times in a single frame with new interpolated cached transform in between, useful when simulating multiple positions in a single frame. 
        /// Always call ResetStateTransform() at the end of the frame
        /// </summary>
        public void SetStateTransformToCached()
        {
            // First simulation of the frame should always sore the transform, following simulations in the same frame shouldn't
            if (!isAtCachedPosition)
            {
                _savedFrameData = new TransformFrameData(_target); // store current position/rotation
            }
            else
            {
                //GD.Print("Moving " + this.Name + " from one cached position to another");
            }

            _cachedPosRot.Apply(_target);
            isAtCachedPosition = true;

            SetCollisionShapeDisabled(false); // make collision shape enabled for raycast hits

            // Colliders for the purpose of Raycasts and Physics are only updated at the regualar process update.
            // To allow us immediately casting, we need to update the transforms on the PhysicsServer
            // Maybe one day it will be possible to update all with a singel function call.
            //
            // Also, might not even work as espected, see: https://github.com/godotengine/godot-proposals/issues/5181
            // Personal testing showed: With standard Godot Physics, no matter if we call this, it will not update. With Jolt Physics, will always update, no matter if we call this.
            //_target.ForceUpdateTransform();
        }

        /// <summary>
        /// Re-apply previous position and rotation
        /// </summary>
        public void ResetStateTransform()
        {
            if (isAtCachedPosition)
            {
                _savedFrameData.Apply(_target);
            }
            isAtCachedPosition = false;

            SetCollisionShapeDisabled(false); // make collision shape enabled for raycast hits
        }

        /// <summary>
        /// Try to set the active state of the target collision shape.
        /// </summary>
        /// <param name="disabled">New value for <c>CollisionShape3D</c> Disabled property</param>
        public void SetCollisionShapeDisabled(bool disabled)
        {
            
            if (_targetCollisionShape == null)
            {
                return;
            }
            

            _targetCollisionShape.Disabled = disabled; // Tested: This disables the collider from being hit even without calling ForceUpdateTransform()
        }

        #endregion

        #region Debug Draw

        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            if (_target == null)
            {
                return;
            }

            CollisionShape3D collisionShape = _target as CollisionShape3D;

            if (collisionShape == null)
            {
                return;
            }

            if (collisionShape.Shape is CapsuleShape3D)
            {
                CapsuleShape3D capsule = collisionShape.Shape as CapsuleShape3D;
                ColliderDrawing.DebugDrawCapsule(position, rotation, capsule.Height, capsule.Radius, duration, col, editorGizmo);
            }
            else if (collisionShape.Shape is BoxShape3D)
            {
                BoxShape3D box = collisionShape.Shape as BoxShape3D;
                ColliderDrawing.DebugDrawBox(position, rotation, box.Size, duration, col, editorGizmo);
            }
            else if (collisionShape.Shape is SphereShape3D)
            {
                SphereShape3D sphere = collisionShape.Shape as SphereShape3D;
                ColliderDrawing.DebugDrawSphere(position, rotation, sphere.Radius, duration, col, editorGizmo);
            }
            else
            {
                GD.PrintErr("CollisionShape " + collisionShape.Shape.GetType().ToString() + " is not yet implemented for debug drawing");
            }
        }

        #endregion


    }




}