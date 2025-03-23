using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using Godot;
using PG.LagCompensation.Base;
using PG.LagCompensation.Parametric;
using PG.LagCompensation.Physical;

namespace PG.LagCompensation.Testing
{

    /// <summary>
    /// Test collision checking with custom hit colliders and parametric raycasts
    /// </summary>
    public partial class HitColliderTester : Node3D
    {
        [Export]
        private HitColliderGeneric[] colliders;

        [Export]
        private float maxDistance = 10f;


        //[Header("Performance Test Settings")]
        [Export]
        private bool doPerformanceTest = false;
        [Export]
        private int loopCount = 10;

        //[Header("Performance Test Results")]
        [Export]
        private double[] summedTime;

        /// <summary>
        /// counter for debug draw interval
        /// </summary>
        private float counter = 0f;
        /// <summary>
        /// Time between debug draw calls
        /// </summary>
        private float debugDrawInterval = 1f;


        public override void _Ready()
        {
            if (!doPerformanceTest)
                return;

            CallDeferred(nameof(DelayedAction));
        }



        private async void DelayedAction()
        {
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

            for (int i = 0; i < colliders.Length; i++)
            {
                double t = Time.GetTicksUsec() * 1e-6;

                for (int k = 0; k < loopCount; k++)
                {
                    colliders[i].ColliderCastLive(GlobalPosition, GlobalBasis.Z, maxDistance, out ColliderCastHit _hit);

                }


                summedTime[i] = Time.GetTicksUsec() * 1e-6 - t;
            }



        }



        public override void _Process(double delta)
        {
            if (doPerformanceTest)
                return;

            counter += (float)delta;

            if (counter > debugDrawInterval)
            {
                counter -= debugDrawInterval;
                ColliderCastSystem.DebugDrawCollidersLive(debugDrawInterval);
            }


            ColliderCastHit _hit;

            Vector3 o = GlobalPosition;
            Vector3 d = GlobalBasis.Z;

            if (ColliderCastSystem.ColliderCastLive(o, d, maxDistance, out _hit, out HitColliderCollection collection, out int hitColIndex))
            {
                ColliderDrawing.DrawLine(_hit.entryPoint, _hit.entryPoint + _hit.entryNormal, Color.Color8(0, 127, 127));
                ColliderDrawing.DrawLine(o, _hit.entryPoint, Color.Color8(0, 255, 0));

                ColliderDrawing.DrawLine(_hit.exitPoint, _hit.exitPoint + _hit.exitNormal, Color.Color8(255, 0, 127));
                ColliderDrawing.DrawLine(_hit.entryPoint, _hit.exitPoint, Color.Color8(127, 127, 127));
            }
            else
            {
                ColliderDrawing.DrawLine(o, o + d * maxDistance, Color.Color8(255, 0, 0));
            }

            /*

            if (colliders.Length >= 1)
            {
                ColliderCastHit _hit;

                Vector3 o = transform.position;
                Vector3 d = transform.forward;

                if (colliders[0].ColliderCast(o, d, maxDistance, out _hit))
                {
                    Debug.DrawLine(_hit.entryPoint, _hit.entryPoint + _hit.entryNormal, Color.yellow);
                    Debug.DrawLine(o, _hit.entryPoint, Color.green);

                    Debug.DrawLine(_hit.exitPoint, _hit.exitPoint + _hit.exitNormal, Color.magenta);
                    Debug.DrawLine(_hit.entryPoint, _hit.exitPoint, Color.grey);
                }
                else
                {
                    Debug.DrawLine(o, o + d * maxDistance, Color.red);
                }

            }

            */
        }
    }

}