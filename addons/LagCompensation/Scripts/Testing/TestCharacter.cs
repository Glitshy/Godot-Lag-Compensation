using System.Collections;
using System.Collections.Generic;
using Godot;
using PG.LagCompensation.Parametric;
using PG.LagCompensation.Physical;
using PG.LagCompensation.Hybrid;
using PG.LagCompensation.Base;

namespace PG.LagCompensation.Testing
{

    /// <summary>
    /// Character for testing compensation with custom hit colliders and parametric raycasts as well as physical colliders
    /// </summary>
    [Tool]
    public partial class TestCharacter : Node3D
    {
        /// <summary>
        /// For Physics approach
        /// </summary>
        [Export]
        private Godot.Collections.Array<HybridTracker> trackers = new Godot.Collections.Array<HybridTracker>();
        //private NetworkTracker[] trackers;

        /// <summary>
        /// For Physics approach
        /// </summary>
        [Export]
        private HybridTrackerCollection hybridTrackerCollection;

        /// <summary>
        /// For Parametric approach
        /// </summary>
        [Export]
        private HitColliderCollection hitColCollection;

        private float _nextTime;


        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            if (Time.GetTicksUsec() * 1e-6 >= _nextTime)
            {
                _nextTime += NetworkTrackerSystem.GetStoreInterval; // using same interval for both systems

                double time = Time.GetTicksUsec() * 1e-6;

                // use the static classes to update the frame history of all trackers. Alternatively, could call the inidividual collections
                ColliderCastSystem.AddFrameGlobal(time);
                HybridTrackerSystem.AddFrameGlobal(time);

                //GD.Print("Add trackers at " + (Time.GetTicksUsec() * 1e-6).ToString("F3") + " seconds");
            }
        }
    }


}