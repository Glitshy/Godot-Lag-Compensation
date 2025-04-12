using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PG.LagCompensation.Base
{

    /// <summary>
    /// Debug drawing utilities for both drawing in the game and gizmos in the editor.
    /// </summary>
    public static class ColliderDrawing
    {

        /// <summary>
        /// List of Vector3 describing the lines for custom gizmo drawing in the editor. Must have a multiple of 2 elements.
        /// </summary>
        private static List<Vector3> gizmoDrawLines = new List<Vector3>();

        /// <summary>
        /// Get current list for gizmo line drawing and reset list
        /// </summary>
        public static List<Vector3> GetGizmoDrawLines()
        {
            List<Vector3> currentList = gizmoDrawLines; // cache list
            gizmoDrawLines = new List<Vector3>(); // reset list
            return currentList;
        }


        #region Debug Draw Static

        public static void DrawLine(Vector3 start, Vector3 stop, Color color, float duration = 0f, bool editorGizmo = false)
        {
            if (!editorGizmo)
            {
                // use asset
                DebugDraw.Line(start, stop, color, duration);
            }
            else
            {
                gizmoDrawLines.Add(start);
                gizmoDrawLines.Add(stop);
            }
        }

        public static void DebugDrawCircle(Vector3 center, Vector3 forward, Vector3 right, float radius, float duration, Color color, bool editorGizmo = false)
        {
            int _stepCount = 16;
            float _stepAngleInRadians = 2 * Mathf.Pi / _stepCount;

            for (int i = 0; i < _stepCount; i++)
            {
                DrawLine(center + forward * Mathf.Cos(_stepAngleInRadians * i) * radius + right * Mathf.Sin(_stepAngleInRadians * i) * radius,
                    center + forward * Mathf.Cos(_stepAngleInRadians * (i + 1)) * radius + right * Mathf.Sin(_stepAngleInRadians * (i + 1)) * radius,
                    color, duration, editorGizmo);
            }
        }

        public static void DebugDrawHalfCircle(Vector3 center, Vector3 forward, Vector3 right, float radius, float duration, Color color, bool editorGizmo = false)
        {
            int _stepCount = 8;
            float _stepAngleInRadians = Mathf.Pi / _stepCount;

            for (int i = 0; i < _stepCount; i++)
            {
                DrawLine(center + forward * Mathf.Cos(_stepAngleInRadians * i) * radius + right * Mathf.Sin(_stepAngleInRadians * i) * radius,
                    center + forward * Mathf.Cos(_stepAngleInRadians * (i + 1)) * radius + right * Mathf.Sin(_stepAngleInRadians * (i + 1)) * radius,
                    color, duration, editorGizmo);
            }
        }

        

        public static void DebugDrawSphere(Vector3 centerGlobalPosition, Quaternion rotation, float radius, float duration, Color color, bool editorGizmo = false)
        {
            Vector3 _forward = rotation * Vector3.Forward;
            Vector3 _right = rotation * Vector3.Right;
            Vector3 _up = rotation * Vector3.Up;


            DebugDrawCircle(centerGlobalPosition, _forward, _right, radius, duration, color, editorGizmo);
            DebugDrawCircle(centerGlobalPosition, _forward, _up, radius, duration, color, editorGizmo);
            DebugDrawCircle(centerGlobalPosition, _up, _right, radius, duration, color, editorGizmo);
        }

        private static Quaternion GetCapsuleRotationWithGivenDirection(Quaternion _rotation)
        {
            return (_rotation * Quaternion.FromEuler(new Vector3(Mathf.DegToRad(90f), 0f, 0f)));
        }

        /// <summary>
        /// Draw a capsule. Oriented along the y-axis direction modified by the given rotation.
        /// </summary>
        public static void DebugDrawCapsule(Vector3 centerGlobal, Quaternion rotation, float height, float radius, float duration, Color color, bool editorGizmo = false)
        {
            Vector3 centerInGlobalSpace = centerGlobal;

            Quaternion _adjustedRotation = GetCapsuleRotationWithGivenDirection(rotation);

            Vector3 _planeDirection1 = _adjustedRotation * Vector3.Right;
            Vector3 _planeDirection2 = _adjustedRotation * Vector3.Up;
            Vector3 _capsuleDirection = _adjustedRotation * Vector3.Forward;
            //Vector3 _capusleDirection = _trans.rotation * Vector3.up;

            Vector3 _topCircleCenter = centerInGlobalSpace + _capsuleDirection * (height * 0.5f - radius);
            Vector3 _bottomCircleCenter = centerInGlobalSpace - _capsuleDirection * (height * 0.5f - radius);

            DrawLine(_topCircleCenter + _planeDirection1 * radius, _bottomCircleCenter + _planeDirection1 * radius, color, duration, editorGizmo);
            DrawLine(_topCircleCenter - _planeDirection1 * radius, _bottomCircleCenter - _planeDirection1 * radius, color, duration, editorGizmo);
            DrawLine(_topCircleCenter + _planeDirection2 * radius, _bottomCircleCenter + _planeDirection2 * radius, color, duration, editorGizmo);
            DrawLine(_topCircleCenter - _planeDirection2 * radius, _bottomCircleCenter - _planeDirection2 * radius, color, duration, editorGizmo);

            DebugDrawCircle(_topCircleCenter, _planeDirection1, _planeDirection2, radius, duration, color, editorGizmo);
            DebugDrawCircle(_bottomCircleCenter, _planeDirection1, _planeDirection2, radius, duration, color, editorGizmo);

            DebugDrawHalfCircle(_topCircleCenter, _planeDirection2, _capsuleDirection, radius, duration, color, editorGizmo);
            DebugDrawHalfCircle(_topCircleCenter, _planeDirection1, _capsuleDirection, radius, duration, color, editorGizmo);
            DebugDrawHalfCircle(_bottomCircleCenter, _planeDirection2, -_capsuleDirection, radius, duration, color, editorGizmo);
            DebugDrawHalfCircle(_bottomCircleCenter, _planeDirection1, -_capsuleDirection, radius, duration, color, editorGizmo);
        }

        /// <summary>
        /// Draw a cylinder. Oriented along the y-axis direction modified by the given rotation.
        /// </summary>
        public static void DebugDrawCylinder(Vector3 centerGlobal, Quaternion rotation, float height, float radius, float duration, Color color, bool editorGizmo = false)
        {
            Vector3 centerInGlobalSpace = centerGlobal;

            Quaternion _adjustedRotation = GetCapsuleRotationWithGivenDirection(rotation);

            Vector3 _planeDirection1 = _adjustedRotation * Vector3.Right;
            Vector3 _planeDirection2 = _adjustedRotation * Vector3.Up;
            Vector3 _cylinderDirection = _adjustedRotation * Vector3.Forward;

            Vector3 _topCircleCenter = centerInGlobalSpace + _cylinderDirection * (height * 0.5f);
            Vector3 _bottomCircleCenter = centerInGlobalSpace - _cylinderDirection * (height * 0.5f);

            DrawLine(_topCircleCenter + _planeDirection1 * radius, _bottomCircleCenter + _planeDirection1 * radius, color, duration, editorGizmo);
            DrawLine(_topCircleCenter - _planeDirection1 * radius, _bottomCircleCenter - _planeDirection1 * radius, color, duration, editorGizmo);
            DrawLine(_topCircleCenter + _planeDirection2 * radius, _bottomCircleCenter + _planeDirection2 * radius, color, duration, editorGizmo);
            DrawLine(_topCircleCenter - _planeDirection2 * radius, _bottomCircleCenter - _planeDirection2 * radius, color, duration, editorGizmo);

            DebugDrawCircle(_topCircleCenter, _planeDirection1, _planeDirection2, radius, duration, color, editorGizmo);
            DebugDrawCircle(_bottomCircleCenter, _planeDirection1, _planeDirection2, radius, duration, color, editorGizmo);
        }

        public static void DebugDrawBox(Vector3 position, Quaternion rotation, Vector3 size, float duration, Color color, bool editorGizmo = false)
        {
            Vector3 dimensions = size;

            Vector3 centerInGlobalSpace = position;

            Vector3 _forward = rotation * Vector3.Forward;
            Vector3 _right = rotation * Vector3.Right;
            Vector3 _up = rotation * Vector3.Up;

            Vector3 _RightUpForward = centerInGlobalSpace + _right * (dimensions.X / 2f) + _up * (dimensions.Y / 2f) + _forward * (dimensions.Z / 2f);
            Vector3 _RightUpBackward = centerInGlobalSpace + _right * (dimensions.X / 2f) + _up * (dimensions.Y / 2f) - _forward * (dimensions.Z / 2f);
            Vector3 _RightDownForward = centerInGlobalSpace + _right * (dimensions.X / 2f) - _up * (dimensions.Y / 2f) + _forward * (dimensions.Z / 2f);
            Vector3 _RightDownBackward = centerInGlobalSpace + _right * (dimensions.X / 2f) - _up * (dimensions.Y / 2f) - _forward * (dimensions.Z / 2f);
            Vector3 _LeftUpForward = centerInGlobalSpace - _right * (dimensions.X / 2f) + _up * (dimensions.Y / 2f) + _forward * (dimensions.Z / 2f);
            Vector3 _LeftUpBackward = centerInGlobalSpace - _right * (dimensions.X / 2f) + _up * (dimensions.Y / 2f) - _forward * (dimensions.Z / 2f);
            Vector3 _LeftDownForward = centerInGlobalSpace - _right * (dimensions.X / 2f) - _up * (dimensions.Y / 2f) + _forward * (dimensions.Z / 2f);
            Vector3 _LeftDownBackward = centerInGlobalSpace - _right * (dimensions.X / 2f) - _up * (dimensions.Y / 2f) - _forward * (dimensions.Z / 2f);


            // from forward to backward
            DrawLine(_RightUpForward, _RightUpBackward, color, duration, editorGizmo);
            DrawLine(_RightDownForward, _RightDownBackward, color, duration, editorGizmo);
            DrawLine(_LeftUpForward, _LeftUpBackward, color, duration, editorGizmo);
            DrawLine(_LeftDownForward, _LeftDownBackward, color, duration, editorGizmo);

            // front face
            DrawLine(_RightUpForward, _RightDownForward, color, duration, editorGizmo);
            DrawLine(_RightUpForward, _LeftUpForward, color, duration, editorGizmo);
            DrawLine(_LeftDownForward, _RightDownForward, color, duration, editorGizmo);
            DrawLine(_LeftDownForward, _LeftUpForward, color, duration, editorGizmo);

            // back face
            DrawLine(_RightUpBackward, _RightDownBackward, color, duration, editorGizmo);
            DrawLine(_RightUpBackward, _LeftUpBackward, color, duration, editorGizmo);
            DrawLine(_LeftDownBackward, _RightDownBackward, color, duration, editorGizmo);
            DrawLine(_LeftDownBackward, _LeftUpBackward, color, duration, editorGizmo);
        }

        /// <summary>
        /// Draw a custom mesh by drawing the lines of each triangle
        /// </summary>
        public static void DebugDrawMesh(Vector3 position, Quaternion rotation, Vector3[] faceVertices, float duration, Color color, bool editorGizmo = false)
        {
            Vector3[] newVertices = new Vector3[faceVertices.Length];

            for (int i = 0; i < newVertices.Length; i++)
            {
                // rotate and translate face vertice positions
                newVertices[i] = position + rotation * faceVertices[i];
            }

            // draw triangle faces, consisting of 3 vertices each
            for (int i = 0; i < newVertices.Length; i+=3)
            {
                DrawLine(newVertices[i], newVertices[i + 1], color, duration, editorGizmo); // face vertice 0 and 1
                DrawLine(newVertices[i + 1], newVertices[i + 2], color, duration, editorGizmo); // face vertice 1 and 2
                DrawLine(newVertices[i + 2], newVertices[i], color, duration, editorGizmo); // face vertice 2 and 0
            }
        }


        #endregion
    }

}