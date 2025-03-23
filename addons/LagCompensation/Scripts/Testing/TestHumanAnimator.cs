using Godot;
using PG.LagCompensation.Parametric;
using System;

namespace PG.LagCompensation.Testing
{

    public partial class TestHumanAnimator : Node3D
    {
        [Export]
        private Skeleton3D skeleton;

        [Export]
        private PhysicalBoneSimulator3D boneSimulator;

        [Export]
        private AnimationPlayer animationPlayer;

        [Export]
        private int animationIndex = 0;

        private float timer = 0f;

        private float deathDelay = 5f;

        public override void _Ready()
        {
            var libraryNames = animationPlayer.GetAnimationLibraryList();

            var mainLibaryName = libraryNames[0];

            AnimationLibrary mainLibrary = animationPlayer.GetAnimationLibrary(mainLibaryName);

            var animationNames = mainLibrary.GetAnimationList();

            var firstAnimationName = animationNames[animationIndex];

            Animation firstAnimation = mainLibrary.GetAnimation(firstAnimationName);

            GD.Print("Play animation: " + mainLibaryName + "/" + firstAnimationName);

            animationPlayer.Play(mainLibaryName + "/" + firstAnimationName);
        }

        public override void _Process(double delta)
        {
            timer += (float)delta;

            if (timer > deathDelay)
            {
                if (!boneSimulator.Active)
                {
                    GD.Print("Start bone physics");
                    boneSimulator.PhysicalBonesStartSimulation();
                }

                timer -= deathDelay;
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            // physical bones do not move while not simulating. This means that they do not follow the animation.
            // https://github.com/godotengine/godot/issues/40076

            // this seems to be a hacky solution to force it to update. TODO: Maybe use bone attachment instead for HitColliders
            boneSimulator.PhysicalBonesStartSimulation();
            boneSimulator.PhysicalBonesStopSimulation();
        }
    }

}