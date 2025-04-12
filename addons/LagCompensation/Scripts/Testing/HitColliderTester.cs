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
        private float maxDistance = 10f;
        [Export]
        private HitColliderCollection collection;
        [Export]
        private bool doPerformanceTest = false;
        [Export]
        private int loopCount = 1000;

        /// <summary>
        /// counter for debug draw interval
        /// </summary>
        private float _counter = 0f;
        /// <summary>
        /// Time between debug draw calls
        /// </summary>
        private float _debugDrawInterval = 1f;

        /// <summary>
        /// Buttons for enabling/disabling colliders
        /// </summary>
        private List<Button> _buttons = new List<Button>();
        private List<Vector3> _buttonLocationOffsets = new List<Vector3>();

        public override void _Ready()
        {
            _counter = _debugDrawInterval; // set it to inveral value for immediate drawing

            // create buttons via CallDeferred, because otherwise it doesn't work inside _Ready()
            CallDeferred(nameof(CreateButtons));

            if (!doPerformanceTest)
                return;

            CallDeferred(nameof(DelayedTest));
        }

        private void CreateButtons()
        {
            for (int i = 0; i < collection.GetHitColliderCount; i++)
            {
                HitColliderGeneric col = collection.GetHitColliderAtIndex(i);
                var button = new Button();
                button.Text = col.Name;
                int buttonIndex = i; // important: must create local deep copy of integer, otherwise the ButtonPressed() method will always use the latest value of i
                button.Pressed += () => ButtonPressed(buttonIndex);
                _buttons.Add(button);
                _buttonLocationOffsets.Add(Vector3.Zero);
            }

            var buttonAll = new Button();
            buttonAll.Text = "All";
            _buttons.Add(buttonAll);
            buttonAll.Pressed += () => ButtonPressed(-1);

            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].Position = new Vector2(i * 100f, 0f);
                GetTree().CurrentScene.AddChild(_buttons[i]);
            }
        }

        /// <summary>
        /// Move all colliders out of the way except the given one. If given -1, reset all postions.
        /// </summary>
        private void ButtonPressed(int index)
        {
            int count = collection.GetHitColliderCount;

            for (int i = 0; i < count; i++)
            {
                HitColliderGeneric col = collection.GetHitColliderAtIndex(i);

                if (index == i || index == -1)
                {
                    col.GlobalPosition -= _buttonLocationOffsets[i];
                    _buttonLocationOffsets[i] = Vector3.Zero;
                }
                else
                {
                    Vector3 offset = Vector3.Right * 100f;
                    col.GlobalPosition += offset;
                    _buttonLocationOffsets[i] += offset;
                }
            }
        }

        private async void DelayedTest()
        {
            await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);

            int count = collection.GetHitColliderCount;

            double[] summedTime = new double[count];
            bool[] hits = new bool[count];

            GD.Print("Testing performance with " + loopCount + " raycast iterations per collider");

            for (int i = 0; i < count; i++)
            {
                HitColliderGeneric col = collection.GetHitColliderAtIndex(i);

                // cast rays vertically down onto the colliders
                Vector3 rayOrigin = col.GlobalPosition + Vector3.Up * maxDistance * 0.5f;
                Vector3 rayDirection = -Vector3.Up;

                double t = Time.GetTicksUsec() * 1e-6;

                for (int k = 0; k < loopCount; k++)
                {
                    hits[i] = col.ColliderCastLive(rayOrigin, rayDirection, maxDistance, out ColliderCastHit _hit);
                }

                summedTime[i] = Time.GetTicksUsec() * 1e-6 - t;
            }

            for (int i = 0; i < count; i++)
            {
                HitColliderGeneric col = collection.GetHitColliderAtIndex(i);

                // also print if the raycasts hit. Should always be true because we cast straigt down onto the collider center location. Only might be false if a mesh collider with a hole was used.
                GD.Print("Collider " + col.Name + " time " + summedTime[i].ToString("F6") + " seconds" + " | " + " hit=" + hits[i].ToString());
            }


            doPerformanceTest = false; // allow process again after the test is done
        }



        public override void _Process(double delta)
        {
            if (doPerformanceTest)
                return;

            _counter += (float)delta;

            if (_counter > _debugDrawInterval)
            {
                _counter -= _debugDrawInterval;
                ColliderCastSystem.DebugDrawCollidersLive(_debugDrawInterval);
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