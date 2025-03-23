using System;
using System.Collections.Generic;
using Godot;
using PG.LagCompensation.Base;


namespace PG.LagCompensation.Physical
{

    /// <summary>
    /// Track and store transforms of the parent <c>Node3D</c> in a list and rewind time for raycast calculations. Should only run on server, disabled on clients
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class NetworkTracker : Node
    {
        /// <summary>
        /// Target node, typically the parent node. Should be CollisionShape3D.
        /// </summary>
        [Export]
        private Node3D target;
        public Node3D GetTargetNode => target;

        private List<TransformFrameData> _frameData = new List<TransformFrameData>();
        private List<double> _frameTimes = new List<double>();

        /// <summary>
        /// Last postion and rotation before rewinding
        /// </summary>
        private TransformFrameData _savedFrameData = new TransformFrameData();

        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            NetworkTrackerSystem.Add(this);

            if (target == null)
            {
                target = GetParent() as Node3D;
            }
        }

        // OnDestroy
        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                NetworkTrackerSystem.Remove(this);
            }
        }

        #region Lag Compensation

        /// <summary>
        /// Add postion/rotation with timestamp to list. Call this after doing movement updates!
        /// </summary>
        public void AddFrame(double time)
        {
            if (_frameTimes.Count >= NetworkTrackerSystem.GetFrameHistoryLength) // remove oldest stored frame
            {
                _frameTimes.RemoveAt(0);
                _frameData.RemoveAt(0);
            }

            if (target == null)
            {
                GD.PrintErr("Target of Tracker " + this.Name + " is null, parent name is: " + GetParent().Name);
            }

            _frameData.Add(new TransformFrameData(target));
            _frameTimes.Add(time);
        }


        /// <summary>
        /// Set transform corresponding to desired simulation time. Can optionally be forced NOT to save current state, useful to save performance when simulation multiple positions in a single frame. 
        /// Always call ResetStateTransform() at the end of the frame
        /// </summary>
        /// <param name="simulationTime">Time to which the transform should be reset to</param>
        /// <param name="storeCurrentTransform">First simulation of the frame should always sore the transform, following simulations in the same frame shouldn't.</param>
        public void SetStateTransform(double simulationTime, bool storeCurrentTransform = true)
        {
            if (storeCurrentTransform)
            {
                _savedFrameData = new TransformFrameData(target); // store current position/rotation
            }


            for (int i = _frameTimes.Count - 1; i >= 0; i--)
            {
                if (_frameTimes[i] <= simulationTime) // if the data at [i] is older than the desired simulation time
                {
                    double timeOlder = _frameTimes[i];
                    TransformFrameData _interpolatedFrame;

                    if (i < _frameTimes.Count - 1) // if there is a newer frame
                    {
                        double timeNewer = _frameTimes[i + 1];

                        double fraction = Math.Clamp((simulationTime - timeOlder) / (timeNewer - timeOlder), 0d, 1d);
                        _interpolatedFrame = TransformFrameData.Interpolate(_frameData[i], _frameData[i + 1], fraction);
                    }
                    else // there is no newer frame --> interpolate between this 'newest' frame and the current position!
                    {
                        double fraction = Math.Clamp((simulationTime - timeOlder) / (Time.GetTicksUsec() * 1e-6 - timeOlder), 0d, 1d); // TODO: Check if replacing this time function with something else is required

                        _interpolatedFrame = TransformFrameData.Interpolate(_frameData[i], new TransformFrameData(target), fraction);
                    }

                    _interpolatedFrame.Apply(target);

                    return;
                }
            }

        }

        /// <summary>
        /// Re-apply previous position and rotation
        /// </summary>
        public void ResetStateTransform()
        {
            _savedFrameData.Apply(target);
        }

        #endregion

        #region Debug Draw

        /// <summary>
        /// Draw collider with DrawLine, for given duration
        /// </summary>
        public void DebugDrawColliders(float duration)
        {
            if (target == null)
            {
                return;
            }

            CollisionShape3D collisionShape = target as CollisionShape3D;

            if (collisionShape == null)
            {
                return;
            }

            Vector3 globalPosition = target.GlobalPosition;
            Quaternion globalQuaternion = target.GlobalBasis.GetRotationQuaternion();
            Color yellow = Color.Color8(255, 255, 0);

            if (collisionShape.Shape is CapsuleShape3D)
			{
                CapsuleShape3D capsule = collisionShape.Shape as CapsuleShape3D;
                ColliderDrawing.DebugDrawCapsule(globalPosition, globalQuaternion, capsule.Height, capsule.Radius, duration, yellow);
			}
            else if (collisionShape.Shape is BoxShape3D)
			{
                BoxShape3D box = collisionShape.Shape as BoxShape3D;
                ColliderDrawing.DebugDrawBox(globalPosition, globalQuaternion, box.Size, duration, yellow);
            }
            else if (collisionShape.Shape is SphereShape3D)
            {
                SphereShape3D sphere = collisionShape.Shape as SphereShape3D;
                ColliderDrawing.DebugDrawSphere(globalPosition, globalQuaternion, sphere.Radius, duration, yellow);
            }
        }

        #endregion


    }




}