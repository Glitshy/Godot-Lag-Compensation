using Godot;
using System;

namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Contains global postion and rotation of collider
    /// </summary>
    public struct TransformFrameData
    {
        /// <summary>
        /// Position in global space
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Rotation in global space
        /// </summary>
        public Quaternion rotation;

        public TransformFrameData()
        {
            this.position = Vector3.Zero;
            this.rotation = Quaternion.Identity;
        }

        /// <summary>
        /// Store given global position and rotation values
        /// </summary>
        public TransformFrameData(Vector3 position, Quaternion rotation)
        {
            this.position = position;
            this.rotation = rotation;
        }

        /// <summary>
        /// Store global position and rotation of given node
        /// </summary>
        public TransformFrameData(Node3D node)
        {
            this.position = node.GlobalPosition;
            this.rotation = node.GlobalBasis.GetRotationQuaternion();
        }

        /// <summary>
        /// Apply stored global position androtation to the given node. This resets the scale to (1, 1, 1).
        /// </summary>
        public void Apply(Node3D node)
        {
            node.GlobalPosition = this.position;
            node.GlobalBasis = new Basis(this.rotation);
        }

        /// <summary>
        /// Interpolate between two structs
        /// </summary>
        /// <param name="from">Start state</param>
        /// <param name="to">End state</param>
        /// <param name="t">Interpolation value, should be between 0 and 1</param>
        /// <returns>Interpolated state</returns>
        public static TransformFrameData Interpolate(TransformFrameData from, TransformFrameData to, double t)
        {
            return new TransformFrameData(
                from.position.Lerp(to.position, (float)t),
                from.rotation.Slerp(to.rotation, (float)t)
            );
        }
    }

}