using Godot;
using System;

namespace PG.LagCompensation.Testing
{
    /// <summary>
    /// Continuously rotate node around y axis
    /// </summary>
    public partial class Turntable : Node3D
    {
        /// <summary>
        /// Rotation speed in rotations per second
        /// </summary>
        [Export]
        private float rotationSpeed = 0.1f;


        public override void _Process(double delta)
        {
            RotationDegrees += new Vector3(0f, rotationSpeed * 360f * (float)delta, 0f);

        }

    }

}