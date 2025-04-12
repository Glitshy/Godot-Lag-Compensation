using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Godot;
using PG.LagCompensation.Base;

namespace PG.LagCompensation.Parametric
{
    /// <summary>
    /// Collection of hit colliders with different shapes. On cast checking the bounding sphere of this is checked first before any child colliders.
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class HitColliderCollection : TrackerBase
    {
        public override Node3D GetTargetNode => this;
        public override int GetHistoryLength => ColliderCastSystem.GetFrameHistoryLength;

        /// <summary>
        /// Parent node used for editor button functionality
        /// </summary>
        [Export]
        private Node3D parentForButton;

        [ExportToolButton("Create Colliders in Children")]
        public Callable ButtonCreateColliders => Callable.From(AddHitCollidersToPhysicsShapes);

        [ExportToolButton("Fill Colliders List with nodes in Children")]
        public Callable ButtonFillColliders => Callable.From(GetAllHitColliders);

        [ExportToolButton("Delete Colliders in Children")]
        public Callable ButtonDeleteColliders => Callable.From(DeleteHitColliders);

        /// <summary>
        /// Must be manually set to be large enough to encompass all colliders in the collection.
        /// </summary>
        private float _radius = 0.5f;

        [Export]
        float radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                if (Engine.IsEditorHint())
                {
                    UpdateGizmos();
                }
            }
        }

        /// <summary>
        /// All HitColliders managed by this. Godot array so it can be displayed in the editor. 
        /// Important: Should not contain this itself!
        /// </summary>
        [Export]
        private Godot.Collections.Array<HitColliderGeneric> hitCollidersGodot = new Godot.Collections.Array<HitColliderGeneric>();

        /// <summary>
        /// Native C# array has better performance than godot array
        /// Arrays are also faster to loop than lists and we don't need/want the extendable nature of lists anyways https://stackoverflow.com/questions/365615/in-net-which-loop-runs-faster-for-or-foreach
        /// </summary>
        private HitColliderGeneric[] hitColliders = new HitColliderGeneric[0];

        public int GetHitColliderCount => hitColliders.Length;

        public HitColliderGeneric GetHitColliderAtIndex(int i)
		{
            if (i >= hitColliders.Length || i < 0)
			{
                GD.PrintErr("Index " + i + " out of range for list with count " + hitColliders.Length + " (Node: " + this.Name + ")");
                return null;

            }
            
            return hitColliders[i];
        }


        public override float GetBoundingSphereRadius => _radius;
        public override float GetBoundingSphereRadiusSquared => _radius * _radius;


        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            ColliderCastSystem.Add(this);

            hitColliders = hitCollidersGodot.ToArray(); // convert from godot type to C# type
        }

        // OnDestroy
        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                ColliderCastSystem.Remove(this);
            }
        }

        #region Collection

        /// <summary>
        /// Add postion/rotation with timestamp to list of this collection node as well as all nodes managed by this. Call this after doing movement updates.
        /// </summary>
        public void AddFrameAll(double time)
        {
            AddFrame(time);

            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].AddFrame(time);
            }
        }


        /// <summary>
        /// Simulate all managed hitColliders of this collection to the current simulation time value. Only do this after having checked that the ray intersects the collection's bounding sphere.
        /// Will not happen if it has already been performed for the corrent cached position and rotation.
        /// </summary>
        public void SimulateFully()
        {
            if (_cachedIsUpToDate)
            {
                return;
            }
                
            _cachedIsUpToDate = true;

            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].CalculateAndCacheInterpolatedPositionRotation(_cachedTime);
            }
        }



        /// <summary>
        /// Check ray against current transform. Cast against all HitColliders in the collection.
        /// </summary>
        /// <param name="origin">Origin of cast</param>
        /// <param name="direction">Direction of cast</param>
        /// <param name="range">Range of cast</param>
        /// <param name="hit">Exit/entry of hit</param>
        /// <param name="hitColliderIndex">Index of the hit collider in this collection which got hit</param>
        /// <returns>Was a collider hit?</returns>
        public bool ColliderCastLive(Vector3 origin, Vector3 direction, float range, out ColliderCastHit hit, out int hitColliderIndex)
        {
            hit = ColliderCastHit.Zero;
            ColliderCastHit newHit;
            hitColliderIndex = -1;

            for (int i = 0; i < hitColliders.Length; i++)
            {

                if (hitColliders[i].CheckBoundingSphereLive(origin, direction)) // CheckBoundingSphere   //CheckBoundingSphereAtTestPosition
                {
                    if (hitColliders[i].CheckBoundingSphereDistanceLive(origin, direction, range))
                    {
                        if (hitColliders[i].ColliderCastLive(origin, direction, range, out newHit))
                        {
                            if (newHit.entryDistance < hit.entryDistance)
							{
                                hitColliderIndex = i;
                                hit = newHit;
                            }
                                
                        }
                    }
                }

            }


            return hit.entryDistance != Mathf.Inf;
        }


        /// <summary>
        /// Check ray against cached postion/rotation. Cast against all HitColliders in the collection.
        /// </summary>
        /// <param name="origin">Origin of cast</param>
        /// <param name="direction">Direction of cast</param>
        /// <param name="range">Range of cast</param>
        /// <param name="hit">Exit/entry of hit</param>
        /// <param name="hitColliderIndex">Index of the hit collider in this collection which got hit</param>
        /// <returns>Was a collider hit?</returns>
        public bool ColliderCastCached(Vector3 origin, Vector3 direction, float range, out ColliderCastHit hit, out int hitColliderIndex)
        {

            hit = ColliderCastHit.Zero;
            ColliderCastHit newHit;
            hitColliderIndex = -1;

            for (int i = 0; i < hitColliders.Length; i++)
            {

                if (hitColliders[i].CheckBoundingSphereCached(origin, direction)) // CheckBoundingSphere   //CheckBoundingSphereAtTestPosition
                {
                    if (hitColliders[i].CheckBoundingSphereDistanceCached(origin, direction, range))
                    {
                        if (hitColliders[i].ColliderCastCached(origin, direction, range, out newHit))
                        {
                            if (newHit.entryDistance < hit.entryDistance)
                            {
                                hitColliderIndex = i;
                                hit = newHit;
                            }
                        }
                    }
                }

            }


            return hit.entryDistance != Mathf.Inf;
        }

        

        

        #endregion



        #region Debug Draw

        /// <summary>
        /// Draw the entire collection. Draws bounding sphere at given positon/rotation and collection at cached position/rotation.
        /// </summary>
        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            ColliderDrawing.DebugDrawSphere(position, rotation, _radius, duration, col, editorGizmo);
        }

        /// <summary>
        /// Draw all child colliders with Debug.DrawLine, for given duration, at the current live position
        /// </summary>
        public override void DebugDrawColliderLive(float duration, Color col, bool editorGizmo = false)
        {
            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].DebugDrawColliderLive(duration, col, false);
            }
        }

        /// <summary>
        /// Draw all child colliders with Debug.DrawLine, for given duration, at the currently cached position.
        /// Forces interpolation and caching for children.
        /// </summary>
        public override void DebugDrawColliderCached(float duration, Color col)
        {
            SimulateFully();

            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].DebugDrawColliderCached(duration, col);
            }
        }

        /// <summary>
        /// Debug draw at given data
        /// </summary>
        public void DebugDrawAtData(TransformFrameData[] data)
        {
            if (data.Length != hitColliders.Length) // length mismatch
            {
                GD.Print("Array Length mismatch: this collections has " + hitColliders.Length + " colliders but the received array has length " + data.Length);
                return;
            }

            for (int i = 0; i < hitColliders.Length; i++)
            {
                hitColliders[i].DebugDraw(data[i].position, data[i].rotation, 5f, new Color("green"));
            }
        }

        #endregion

        #region Tools

        /// <summary>
        /// Create a HitCollider node for each physics shape in children
        /// </summary>
        private void AddHitCollidersToPhysicsShapes()
        {
            Node3D parent = parentForButton;

            if (parentForButton == null)
            {
                // default to this node
                parent = this;
            }

            var collisionShapes = ColliderUtilities.GetChildrenOfType<CollisionShape3D>(parent, true);

            GD.Print("AddHitCollidersToPhysicsShapes size " + collisionShapes.Count);

            for (int i = 0; i < collisionShapes.Count; i++)
            {
                Shape3D shape = collisionShapes[i].Shape;

                if (shape is SphereShape3D || shape is CapsuleShape3D || shape is BoxShape3D)
                {
                    HitColliderGeneric hitCol = null;

                    if (shape is SphereShape3D sphereShape)
                    {
                        hitCol = new HitColliderSphere();
                        HitColliderSphere hitColSphere = hitCol as HitColliderSphere;
                        hitColSphere.SetRadius = sphereShape.Radius;
                    }
                    if (shape is CapsuleShape3D capsuleShape)
                    {
                        hitCol = new HitColliderCapsule();
                        HitColliderCapsule hitColSphere = hitCol as HitColliderCapsule;
                        hitColSphere.SetRadius = capsuleShape.Radius;
                        hitColSphere.SetHeight = capsuleShape.Height;
                    }
                    if (shape is BoxShape3D boxShape)
                    {
                        hitCol = new HitColliderBox();
                        HitColliderBox hitColSphere = hitCol as HitColliderBox;
                        hitColSphere.SetSize = boxShape.Size;
                    }

                    if (hitCol != null)
                    {
                        collisionShapes[i].AddChild(hitCol);
                        hitCol.Owner = GetTree().EditedSceneRoot; // needed for adding nodes in editor
                        hitCol.Name = "HitCollider " + hitCol.GetInstanceId();

                        GD.Print("Create HitCollider node for " + collisionShapes[i].Name + " (" + shape.GetType().ToString() + ")");
                        hitCollidersGodot.Add(hitCol);
                    }

                    
                }
            }
        }

        /// <summary>
        /// Delete each HitCollider node in children
        /// </summary>
        private void DeleteHitColliders()
        {
            Node3D parent = parentForButton;

            if (parentForButton == null)
            {
                // default to this node
                parent = this;
            }

            var hitCols = ColliderUtilities.GetChildrenOfType<HitColliderGeneric>(parent, true);

            GD.Print("Delete HitCollider quantity: " + hitCols.Count);

            for (int i = 0; i < hitCols.Count; i++)
            {
                hitCols[i].QueueFree();
            }

            hitCollidersGodot = new Godot.Collections.Array<HitColliderGeneric>();
        }

        /// <summary>
        /// Add all colliders in children of target node to this collection
        /// </summary>
        private void GetAllHitColliders()
        {
            Node3D parent = parentForButton;

            if (parentForButton == null)
            {
                // default to this node
                parent = this;
            }

            var hitCols = ColliderUtilities.GetChildrenOfType<HitColliderGeneric>(parent, true);
            for (int i = 0; i < hitCols.Count; i++)
            {
                if (!hitCollidersGodot.Contains(hitCols[i]))
                {
                    //GD.Print("Add " + hitCols[i].Name + " to collection");
                    hitCollidersGodot.Add(hitCols[i]);
                }
            }
        }

        

        #endregion
    }


}