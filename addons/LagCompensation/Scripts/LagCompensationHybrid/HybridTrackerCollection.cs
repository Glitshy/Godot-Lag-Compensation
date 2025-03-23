using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using PG.LagCompensation.Base;


namespace PG.LagCompensation.Hybrid
{

    /// <summary>
    /// Collection of collider trackers. First checking the bounding sphere of this with a parametric raycast before fully reversing the position of the physical child colliders.
    /// Must iteself be placed on the node which represents the center of the entity to be tracked, e.g. a human or vehicle consisting of multiple colliders.
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class HybridTrackerCollection : TrackerBase
    {
        public override Node3D GetTargetNode => this;
        public override int GetHistoryLength => HybridTrackerSystem.GetFrameHistoryLength;

        /// <summary>
        /// Parent node used for editor button functionality
        /// </summary>
        [Export]
        private Node3D parentForButton;

        [ExportToolButton("Create Trackers in Children")]
        public Callable ButtonCreateColliders => Callable.From(CreateTrackers);

        [ExportToolButton("Fill Trackers List with nodes in Children")]
        public Callable ButtonFillColliders => Callable.From(GetTrackers);

        [ExportToolButton("Delete Trackers in Children")]
        public Callable ButtonDeleteColliders => Callable.From(DeleteTrackers);

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
        /// All trackers managed by this. Godot array so it can be displayed in the editor.
        /// </summary>
        [Export]
        private Godot.Collections.Array<HybridTracker> trackersGodot = new Godot.Collections.Array<HybridTracker>();

        /// <summary>
        /// Native C# array has better performance than godot array
        /// Arrays are also faster to loop than lists and we don't need/want the extendable nature of lists anyways https://stackoverflow.com/questions/365615/in-net-which-loop-runs-faster-for-or-foreach
        /// </summary>
        private HybridTracker[] trackers = new HybridTracker[0];

        public HybridTracker GetTrackerAtIndex(int i)
        {
            if (i >= trackers.Length || i < 0)
            {
                GD.PrintErr("Index " + i + " out of range for list with count " + trackers.Length + " (Node: " + this.Name + ")");
                return null;

            }

            return trackers[i];
        }


        /// <summary>
        /// Get Radius of bounding sphere
        /// </summary>
        public override float GetBoundingSphereRadius => _radius;

        /// <summary>
        /// Get squared Radius of bounding sphere (better performance in some cases)
        /// </summary>
        public override float GetBoundingSphereRadiusSquared => _radius * _radius;

        /// <summary>
        /// Check if the interpolated position has been calculated?
        /// </summary>
        public bool GetCachedIsUpToDate => _cachedIsUpToDate;


        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            HybridTrackerSystem.Add(this);

            trackers = trackersGodot.ToArray(); // convert from godot type to C# type

            // radius must be manually set
            if (_radius <= 0f)
            {
                GD.PrintErr("Tracker " + this.Name + " has an invalid bounding sphere radius of " + _radius);
            }
        }

        // OnDestroy
        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                HybridTrackerSystem.Remove(this);
            }
        }

        #region Collection

        /// <summary>
        /// Add postion/rotation with timestamp to list of this collection node as well as all nodes managed by this. Call this after doing movement updates.
        /// </summary>
        public void AddFrameAll(double time)
        {
            AddFrame(time);

            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].AddFrame(time);
            }
        }


        /// <summary>
        /// Interpolate and cache all managed trackers of this collection to the current simulation time value. Only do this after having checked that the ray intersects this collection's bounding sphere.
        /// Will not happen if it has already been performed for the current cached position and rotation.
        /// </summary>
        public void InterpolateFully()
        {
            // do this check to avoid redundantly interpolating each collider
            if (_cachedIsUpToDate)
            {
                return;
            }

            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].CalculateAndCacheInterpolatedPositionRotation(_cachedTime);
            }

            _cachedIsUpToDate = true;
        }

        /// <summary>
        /// For all nodes of collection: Set transform corresponding to cached value from <c>CalculateAndCacheInterpolatedPositionRotation</c>. 
        /// Can optionally be forced NOT to save current state, useful when simulating multiple positions in a single frame and saving preformance. 
        /// Always call ResetStateTransform() at the end of the frame
        /// </summary>
        public void SetStateTransformToCached()
        {
            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].SetStateTransformToCached();
            }

        }

        /// <summary>
        /// For all nodes of collection: Re-apply previous position and rotation
        /// </summary>
        public void ResetStateTransform()
        {
            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].ResetStateTransform();
            }

            _cachedIsUpToDate = false;
        }

        /// <summary>
        /// Try to set the active state of the target collision shapes of all children of this collection.
        /// </summary>
        /// <param name="disabled">New value for <c>CollisionShape3D</c> Disabled property</param>
        public void SetCollisionShapeDisabled(bool disabled)
        {
            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].SetCollisionShapeDisabled(disabled);
            }
        }

        #endregion

        #region Debug Draw

        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            ColliderDrawing.DebugDrawSphere(position, rotation, _radius, duration, col, editorGizmo);
        }

        /// <summary>
        /// Draw all child colliders with Debug.DrawLine, for given duration, at the current live position
        /// </summary>
        public override void DebugDrawColliderLive(float duration, Color col, bool editorGizmo = false)
        {
            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].DebugDrawColliderLive(duration, col, false);
            }
        }

        /// <summary>
        /// Draw all child colliders with Debug.DrawLine, for given duration, at the currently cached position.
        /// Forces interpolation and caching for children.
        /// </summary>
        public override void DebugDrawColliderCached(float duration, Color col)
        {
            // avoid using InterpolateFully() and therefore messing with the _cachedIsUpToDate variable
            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].CalculateAndCacheInterpolatedPositionRotation(_cachedTime);
            }

            for (int i = 0; i < trackers.Length; i++)
            {
                trackers[i].DebugDrawColliderCached(duration, col);
            }
        }

        #endregion

        #region Tools

        /// <summary>
        /// Used for button
        /// </summary>
        private void CreateTrackers()
        {
            Node3D parent = parentForButton;

            if (parentForButton == null)
            {
                // default to this node
                parent = this;
            }

            var colliders = ColliderUtilities.GetChildrenOfType<CollisionShape3D>(parent, true).ToArray();

            for (int i = 0; i < colliders.Length; i++)
            {
                var trackersInChildren = ColliderUtilities.GetChildrenOfType<HybridTracker>(parent, false);

                // only create if there is not tracker child yet
                if (trackersInChildren.Count == 0)
                {
                    var tracker = new HybridTracker();
                    colliders[i].AddChild(tracker);
                    tracker.Owner = GetTree().EditedSceneRoot; // needed for adding nodes in editor
                    tracker.Name = "Tracker " + tracker.GetInstanceId();

                    tracker.SetTargetNode = colliders[i];
                }
            }

            GetTrackers();
        }

        /// <summary>
        /// Used for button
        /// </summary>
        private void GetTrackers()
        {
            Node3D parent = parentForButton;

            if (parentForButton == null)
            {
                // default to this node
                parent = this;
            }

            var nodes = ColliderUtilities.GetChildrenOfType<HybridTracker>(parent, true);

            GD.Print("found " + nodes.Count + " trackers");

            trackersGodot = new Godot.Collections.Array<HybridTracker>();

            for (int i = 0; i < nodes.Count; i++)
            {
                if (!trackersGodot.Contains(nodes[i]))
                {
                    trackersGodot.Add(nodes[i]);
                }
            }
        }

        /// <summary>
        /// Used for button
        /// </summary>
        private void DeleteTrackers()
        {
            GetTrackers();

            foreach (var tracker in trackersGodot)
            {
                tracker.QueueFree();
            }

            trackersGodot = new Godot.Collections.Array<HybridTracker>();
        }

        #endregion
    }




}