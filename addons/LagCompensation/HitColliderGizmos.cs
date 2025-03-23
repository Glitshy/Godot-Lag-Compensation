using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using Godot.Collections;
using PG.LagCompensation.Base;
using PG.LagCompensation.Hybrid;

#if TOOLS

namespace PG.LagCompensation.Parametric
{
    /// <summary>
    /// Draw the shapes of HitCollider nodes in the editor
    /// </summary>
    [Tool]
    public partial class HitColliderGizmos : EditorNode3DGizmoPlugin
    {

        private Vector3 nodeHandleStart;
        private Vector3 nodeHandleStop;

        private Vector3 cameraScreenStart;
        private Vector3 cameraScreenStop;

        private Vector3 tanStart;
        private Vector3 tanStop;

        /// <summary>
        /// Initializes materials in constructor
        /// </summary>
        public HitColliderGizmos()
        {
            // create materials only once
            CreateMaterial("hitCollider", new Color("blue"));
            CreateMaterial("hitColliderBoundingSphere", new Color("cyan"));
            CreateMaterial("trackerBoundingSphere", new Color("orange"));

            CreateHandleMaterial("handles");

            CreateMaterial("debugPink", new Color(1, 0, 1));
            CreateMaterial("debugCyan", new Color(0, 1, 1));
        }

        public override string _GetGizmoName()
        {
            return "HitColliderGizmo";
        }

        public override bool _HasGizmo(Node3D node)
        {
            // which node types use this gizmo?
            return node is HitColliderGeneric || node is HitColliderCollection || node is HybridTrackerCollection;
        }

        public override void _Redraw(EditorNode3DGizmo gizmo)
        {
            gizmo.Clear();

            Node3D node = gizmo.GetNode3D();

            //GD.Print("Redraw Gizmo for " + node.Name + " of type " + node.GetType().ToString());

            TrackerBase tracker = node as TrackerBase;

            if (tracker == null)
            {
                GD.PrintErr("Trying to redraw Gizmo for node of type " + node.GetType().ToString() + ", are you lacking the [Tools] attribute?");
                return;
            }

            if (node is HitColliderGeneric)
            {
                DrawHitColliderWithHandles(gizmo, node, "hitCollider");
            }
            else if (node is HitColliderCollection)
            {
                DrawTrackerBoundingSphere(gizmo, node, "hitColliderBoundingSphere");
            }
            else if (node is HybridTrackerCollection)
            {
                DrawTrackerBoundingSphere(gizmo, node, "trackerBoundingSphere"); // we want to draw the bounding sphere of a collection, but we don't need to handle the individual collision shapes, as these are drawn by default
            }
            else
            {
                // not yet implemented
            }

        }

        private void DrawHitColliderWithHandles(EditorNode3DGizmo gizmo, Node3D node, string lineMaterial)
        {
            HitColliderGeneric hitCol = node as HitColliderGeneric;

            if (hitCol == null)
            {
                // did you pass a node without checkign the type?
                return;
            }

            hitCol.DebugDrawColliderLive(0f, new Color(0, 0, 0), true);

            List<Vector3> lines = ColliderDrawing.GetGizmoDrawLines();

            // lines and handles are drawn at a position relative to the node set for the gizmo.
            // Therefore, convert from global space to local space
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = node.ToLocal(lines[i]);
            }

            gizmo.AddLines(lines.ToArray(), GetMaterial(lineMaterial, gizmo), false);


            // for debugging of handles
            List<Vector3> debugLinesPink = new List<Vector3>();
            debugLinesPink.Add(nodeHandleStart);
            debugLinesPink.Add(nodeHandleStop);

            debugLinesPink.Add(tanStart);
            debugLinesPink.Add(tanStop);

            //gizmo.AddLines(debugLinesPink.ToArray(), GetMaterial("debugPink", gizmo), false);

            List<Vector3> debugLinesCyan = new List<Vector3>();
            debugLinesCyan.Add(cameraScreenStart);
            debugLinesCyan.Add(cameraScreenStop);

            //gizmo.AddLines(debugLinesCyan.ToArray(), GetMaterial("debugCyan", gizmo), false);

            // add handles for all 6 sides
            var handles = new Vector3[6];
            int[] handleIds = new int[6];

            for (int i = 0; i < 6; i++)
            {
                handles[i] = GetPositionForHandle(i, hitCol);
                handleIds[i] = i;
            }

            gizmo.AddHandles(handles, GetMaterial("handles", gizmo), handleIds); // currently, no handles are used
        }

        private void DrawTrackerBoundingSphere(EditorNode3DGizmo gizmo, Node3D node, string lineMaterial)
        {
            TrackerBase tracker = node as TrackerBase;

            if (tracker == null)
            {
                // did you pass a node without checkign the type?
                return;
            }

            tracker.DebugDrawBoundingSphereLive(0f, new Color(0, 0, 0), true);

            List<Vector3> lines = ColliderDrawing.GetGizmoDrawLines();

            // lines and handles are drawn at a position relative to the node set for the gizmo.
            // Therefore, convert from global space to local space
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = node.ToLocal(lines[i]);
            }

            gizmo.AddLines(lines.ToArray(), GetMaterial(lineMaterial, gizmo), false);
        }

        public override void _SetHandle(EditorNode3DGizmo gizmo, int handleId, bool secondary, Camera3D camera, Vector2 screenPos)
        {
            //base._SetHandle(gizmo, handleId, secondary, camera, screenPos);

            Node3D node = gizmo.GetNode3D();

            HitColliderGeneric hitCol = node as HitColliderGeneric;



            Vector3 localHandlePosition = GetPositionForHandle(handleId, hitCol);
            Vector3 globalHandlePosition = node.ToGlobal(localHandlePosition);

            ColliderMath.ClosestPointsOnTwoLines(camera.GlobalPosition, camera.ProjectRayNormal(screenPos), node.GlobalPosition, globalHandlePosition - node.GlobalPosition, out Vector3 point1, out Vector3 point2);

            float t = ColliderMath.GetTValueAlongLine(node.GlobalPosition, globalHandlePosition, point1);

            Vector3 positionAtT = node.GlobalPosition * (1 - t) + globalHandlePosition * t;

            // for debug draw of handle lines
            nodeHandleStart = Vector3.Zero;
            nodeHandleStop = node.ToLocal(globalHandlePosition);

            cameraScreenStart = node.ToLocal(camera.GlobalPosition);
            cameraScreenStop = node.ToLocal(camera.GlobalPosition + camera.ProjectRayNormal(screenPos) * 50);

            tanStart = node.ToLocal(positionAtT);
            tanStop = node.ToLocal(point1);

            float scaleFactor = (t / 1f - 1f) * 0.5f + 1f; // radius changes diameter by factor 2, therefore reduce scale factor * 0.5

            if (hitCol is HitColliderSphere sphere)
            {
                float oldRadius = sphere.GetRadius;
                float newRadius = Mathf.Max(oldRadius * scaleFactor, 0.00001f); // prevent going to or below zero

                sphere.SetRadius = newRadius;

                node.GlobalPosition = node.GlobalPosition + (globalHandlePosition - node.GlobalPosition) * (newRadius / oldRadius - 1);
            }
            else if (hitCol is HitColliderCapsule capsule)
            {
                float radius = capsule.GetRadius;
                float height = capsule.GetHeight;

                float oldRadius;
                float newRadius;

                if (handleId == 1 || handleId == 4) // y-axis
                {
                    oldRadius = capsule.GetHeight * 0.5f;
                    newRadius = Mathf.Max(oldRadius * scaleFactor, 0.00001f); // prevent going to or below zero

                    capsule.SetHeight = newRadius * 2f;
                }
                else
                {
                    oldRadius = capsule.GetRadius;
                    newRadius = Mathf.Max(oldRadius * scaleFactor, 0.00001f); // prevent going to or below zero

                    capsule.SetRadius = newRadius;
                }

                node.GlobalPosition = node.GlobalPosition + (globalHandlePosition - node.GlobalPosition) * (newRadius / oldRadius - 1);
            }
            else if (hitCol is HitColliderBox box)
            {
                Vector3 size = box.GetSize;
                float oldRadius = size[handleId % 3] * 0.5f;
                float newRadius = Mathf.Max(oldRadius * scaleFactor, 0.00001f); // prevent going to or below zero

                size[handleId % 3] = newRadius * 2f;

                box.SetSize = size;

                node.GlobalPosition = node.GlobalPosition + (globalHandlePosition - node.GlobalPosition) * (newRadius / oldRadius - 1);
            }

            // test math
            /*
            Vector3 P1 = new Vector3(1, 1, 1);
            Vector3 D1 = new Vector3(-2, 0, -2);
            Vector3 P2 = new Vector3(-1, -1, 1);
            Vector3 D2 = new Vector3(2, 0, -2);

            HitCollider.ClosestPointsOnTwoLines(P1, D1, P2, D2, out Vector3 pointTest1, out Vector3 pointTest2);
            
            GD.Print("pointTest1 " + pointTest1 + " pointTest2 " + pointTest2);

            nodeHandleStart = node.ToLocal(P1);
            nodeHandleStop = node.ToLocal(P1+D1);

            cameraScreenStart = node.ToLocal(P2);
            cameraScreenStop = node.ToLocal(P2+D2);
            */

            node.UpdateGizmos();
        }

        /// <summary>
        /// Return a normalized vector pointing in one of six directions
        /// </summary>
        /// <param name="handleId">Handle index from 0 to 5</param>
        private Vector3 GetDirectionForHandle(int handleId)
        {
            switch (handleId)
            {
                case 0:
                    return new Vector3(1, 0, 0);
                case 1:
                    return new Vector3(0, 1, 0);
                case 2:
                    return new Vector3(0, 0, 1);
                case 3:
                    return new Vector3(-1, 0, 0);
                case 4:
                    return new Vector3(0, -1, 0);
                case 5:
                    return new Vector3(0, 0, -1);
            }

            return Vector3.Zero;
        }

        /// <summary>
        /// Return a local position vector describing the handle position for one of six directions
        /// </summary>
        /// <param name="handleId">Handle index from 0 to 5</param>
        private Vector3 GetPositionForHandle(int handleId, HitColliderGeneric hitCol)
        {
            Vector3 direction = GetDirectionForHandle(handleId);

            if (hitCol is HitColliderSphere sphere)
            {
                float radius = sphere.GetRadius;

                return direction * radius;
            }
            else if (hitCol is HitColliderCapsule capsule)
            {
                float radius = capsule.GetRadius;
                float height = capsule.GetHeight;

                
                if (direction.Y == 0)
                {
                    return direction * radius;
                }
                else
                {
                    return direction * height * 0.5f;
                }
            }
            else if (hitCol is HitColliderBox box)
            {
                Vector3 size = box.GetSize;

                return direction * size[handleId % 3] * 0.5f;
            }

            return Vector3.Zero;
        }

    }

}

#endif //TOOLS
