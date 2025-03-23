using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PG.LagCompensation.Base;
using PG.LagCompensation.Parametric;
using PG.LagCompensation.Physical;
using PG.LagCompensation.Hybrid;

namespace PG.LagCompensation.Testing
{

    /// <summary>
    /// Test lag compensation performance with both parametric and physical systems
    /// </summary>
    [GlobalClass]
    public partial class TestLagCompensation : Node
    {
        private enum TestType { testPhysicsRaycast, testPhysicsRaycastOptimized, testColliderCast, testAllWithLoop }

        [Export]
        private RayCast3D raycaster;

        [Export]
        private TestType testType;

        /// <summary>
        /// Do check after X seconds
        /// </summary>
        [Export]
        private float doCheckAfterSeconds = 0.5f;
        /// <summary>
        /// When check happens: Turn back time by X seconds, then du raycasts in regular intervals until this delay has been caught up
        /// </summary>
        [Export]
        private double catchUpTime = 0.3d;
        /// <summary>
        /// how often to simulate the catchup of a single projectile
        /// </summary>
        [Export]
        private int loopCount = 10;

        /// <summary>
        /// Should in the actual game be the same as the fixed physics update rate 'GetPhysicsProcessDeltaTime()'
        /// </summary>
        [Export]
        private float raycastUpdateInterval = 0.02f;
        /// <summary>
        /// Simulate the projectile with a certaain speed, given in meters per second
        /// </summary>
        [Export]
        private float raycastProjectileSpeed = 100f;



        /// <summary>
        /// Performance Test Results
        /// </summary>
        [Export]
        private double summedTimeParametric;

        [Export]
        private double summedTimePhysicalSimple;

        [Export]
        private double summedTimePhysicalOptimized;



        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            if (catchUpTime > doCheckAfterSeconds)
            {
                GD.PrintErr("catchUpTime is larger than doCheckAfterSeconds");
                return;
            }

            Timer(doCheckAfterSeconds, nameof(Test));
        }

        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint())
            {
                return;
            }
        }

        /// <summary>
        /// Call function after X seconds. Given method must be public.
        /// Based on https://www.reddit.com/r/godot/comments/18u1cvq/c_dead_simple_function_call_timer/
        /// </summary>
        public async void Timer(double time, string methodtocall)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(time * 1000f));
            MethodInfo mi = this.GetType().GetMethod(methodtocall);
            mi.Invoke(this, null);
        }

        public void Test()
        {
            GD.Print(testType.ToString());

            switch (testType)
            {
                case TestType.testAllWithLoop:
                    double currentTime = Time.GetTicksUsec() * 1e-6; // use this time for both parametric and physics test

                    TestParametric(currentTime - catchUpTime, currentTime, true); // do this for debug drawing
                    TestPhysics(currentTime - catchUpTime, currentTime, true, true); // do this for debug drawing

                    double previousTime = Time.GetTicksUsec() * 1e-6; // for elapsed time performance calculation
                    for (int i = 0; i < loopCount; i++)
                    {
                        TestParametric(currentTime - catchUpTime, currentTime, false);
                    }
                    summedTimeParametric = Time.GetTicksUsec() * 1e-6 - previousTime;

                    GD.Print("summedTimeParametric " + summedTimeParametric);

                    previousTime = Time.GetTicksUsec() * 1e-6;
                    for (int i = 0; i < loopCount; i++)
                    {
                        TestPhysics(currentTime - catchUpTime, currentTime, true, false);
                    }
                    summedTimePhysicalOptimized = Time.GetTicksUsec() * 1e-6 - previousTime;

                    GD.Print("summedTimePhysicalOptimized " + summedTimePhysicalOptimized);


                    previousTime = Time.GetTicksUsec() * 1e-6;
                    for (int i = 0; i < loopCount; i++)
                    {
                        TestPhysics(currentTime - catchUpTime, currentTime, false, false);
                    }
                    summedTimePhysicalSimple = Time.GetTicksUsec() * 1e-6 - previousTime;

                    GD.Print("summedTimePhysicalSimple " + summedTimePhysicalSimple);

                    break;

                case TestType.testColliderCast:
                    currentTime = Time.GetTicksUsec() * 1e-6;
                    TestParametric(currentTime - catchUpTime, currentTime, true);
                    break;
                case TestType.testPhysicsRaycast:
                    currentTime = Time.GetTicksUsec() * 1e-6;
                    TestPhysics(currentTime - catchUpTime, currentTime, false, true);
                    break;
                case TestType.testPhysicsRaycastOptimized:
                    currentTime = Time.GetTicksUsec() * 1e-6;
                    TestPhysics(currentTime - catchUpTime, currentTime, true, true);
                    break;

            }

        }


        /// <summary>
        /// Using standard physics colliders and trackers for lag compensation
        /// </summary>
        /// <param name="testTime"></param>
        private void TestPhysics(double testTime, double currentTime, bool useOptimized, bool debugDraw)
        {
            double simulationTime = testTime;

            bool firstLoop = true;

            int iterationCount = 0;

            double fixedDelta = raycastUpdateInterval;

            // catch up to the current time in steps of physics fixedDelta time
            while (simulationTime < currentTime)
            {
                if (useOptimized)
                {
                    HybridTrackerSystem.SimulateStart(simulationTime);
                }
                else
                {
                    // less optimized, updates all CollisionShapes3D in any case
                    HybridTrackerSystem.SimulateFully(simulationTime);
                }

                if (firstLoop)
                {
                    firstLoop = false;

                    if (debugDraw)
                    {
                        GD.Print("Draw physics colliders at iterationCount " + iterationCount);
                        HybridTrackerSystem.DebugDrawCollidersCached();
                    }
                }
                
                if (PhysicsDoOneCast(iterationCount, useOptimized, debugDraw))
                {
                    if (debugDraw)
                    {
                        GD.Print("Physics hit at iterationCount " + iterationCount);
                    }
                }

                simulationTime += fixedDelta;
                iterationCount++;

            }

            HybridTrackerSystem.SimulateReset();


        }

        /// <summary>
        /// Do a single raycast starting at Vector3.Zero and going in (0,0,1) direction with 'iterationCount * raycastUpdateInterval * raycastProjectileSpeed' steps
        /// </summary>
        private bool PhysicsDoOneCast(int iterationCount, bool useOptimized, bool debugDraw)
        {
            float iterationDistance = raycastProjectileSpeed * raycastUpdateInterval;
            Vector3 pos = Vector3.Zero;
            Vector3 dir = new Vector3(0, 0, 1);
            Vector3 start = pos + dir * iterationCount * iterationDistance;

            raycaster.GlobalPosition = start; // set start position of raycast
            raycaster.TargetPosition = new Vector3(0, 0, 1) * iterationDistance; // set end position of raycast (in local space)

            //GD.Print("Physics raycast from " + raycaster.GlobalPosition + " to " + raycaster.TargetPosition);

            if (useOptimized)
            {
                // check bounding sphere intersection with collections
                HybridTrackerSystem.RaycastPrepare(start, new Vector3(0, 0, 1), iterationDistance);
            }

            raycaster.ForceRaycastUpdate(); // do raycast

            Vector3 hitPoint = raycaster.GetCollisionPoint();
            Vector3 hitNormal = raycaster.GetCollisionNormal();

            if (raycaster.IsColliding())
            {
                if (debugDraw)
                {
                    ColliderDrawing.DrawLine(start, hitPoint, Color.Color8(0, 255, 0), 10f);
                    ColliderDrawing.DrawLine(hitPoint, hitPoint + Vector3.Up * 0.1f, Color.Color8(0, 255, 0), 10f);
                }
                return true;
            }
            else
            {
                if (debugDraw)
                {
                    Vector3 end = pos + dir * (iterationCount + 1) * iterationDistance;

                    ColliderDrawing.DrawLine(start, end, Color.Color8(255, 0, 0), 10f);
                    ColliderDrawing.DrawLine(end, end + Vector3.Up * 0.1f, Color.Color8(255, 0, 0), 10f);
                }

            }

            return false;
        }



        /// <summary>
        /// Using custom hit colliders
        /// </summary>
        /// <param name="testTime"></param>
        private void TestParametric(double testTime, double currentTime, bool debugDraw)
        {
            double simulationTime = testTime;

            bool firstLoop = true;

            int iterationCount = 0;

            double fixedDelta = raycastUpdateInterval;

            // catch up to the current time in steps of physics fixedDelta time
            while (simulationTime < currentTime)
            {
                ColliderCastSystem.Simulate(simulationTime);

                if (firstLoop)
                {
                    firstLoop = false;

                    if (debugDraw)
                    {
                        GD.Print("Draw parametric colliders at iterationCount " + iterationCount);
                        //ColliderCastSystem.DebugDrawCollidersLive();
                        ColliderCastSystem.DebugDrawCollidersCached();
                    }
                }

                if (ParametricDoOneCast(iterationCount, debugDraw))
                {
                    if (debugDraw)
                    {
                        GD.Print("Parametric hit at iterationCount " + iterationCount);
                    }
                }

                simulationTime += fixedDelta;
                iterationCount++;

            }


        }

        /// <summary>
        /// Do a single raycast starting at Vector3.Zero and going in (0,0,1) direction with 'iterationCount * raycastUpdateInterval * raycastProjectileSpeed' steps
        /// </summary>
        private bool ParametricDoOneCast(int iterationCount, bool debugDraw)
        {
            float iterationDistance = raycastProjectileSpeed * raycastUpdateInterval;
            Vector3 pos = Vector3.Zero;
            Vector3 dir = new Vector3(0, 0, 1);
            Vector3 start = pos + dir * iterationCount * iterationDistance;

            //GD.Print("Parametric raycast from " + start + " to " + (start + dir * iterationDistance) + "(iteration " + iterationCount + ")");

            if (ColliderCastSystem.ColliderCastCached(start, dir, iterationDistance, out ColliderCastHit hit, out HitColliderCollection hitCollection, out int hitIndex))
            {
                if (debugDraw)
                {
                    ColliderDrawing.DrawLine(start, hit.entryPoint, new Color("green"), 10f);
                    ColliderDrawing.DrawLine(hit.entryPoint, hit.entryPoint + Vector3.Up * 0.1f, new Color("green"), 10f);
                }

                //ColliderCastSystem.DebugDrawCollidersCached(3f);
                return true;
            }
            else
            {
                if (debugDraw)
                {
                    Vector3 end = pos + dir * (iterationCount + 1) * iterationDistance;

                    ColliderDrawing.DrawLine(start, end, Color.Color8(255, 0, 0), 10f);
                    ColliderDrawing.DrawLine(end, end + Vector3.Up * 0.1f, Color.Color8(255, 0, 0), 10f);
                }

                if (iterationCount % 10 == 0) // only draw every 10th
                {
                    //ColliderCastSystem.DebugDrawCollidersCached(9f);
                }

            }

            


            return false;
        }

    }

}