using System.Collections;
using System.Collections.Generic;
using Godot;
using PG.LagCompensation.Base;

namespace PG.LagCompensation.Parametric
{
    [Tool]
    public partial class HitColliderBox : HitColliderGeneric
    {
        [Export]
        private Vector3 size = Vector3.One;

        /// <summary>
        /// Get size of box, e.g. for gizmo
        /// </summary>
        public Vector3 GetSize => size;

        /// <summary>
        /// Set size of box, e.g. for gizmo
        /// </summary>
        public Vector3 SetSize { set => size = value; }

        public override float GetBoundingSphereRadius => (size * 0.5f).Length();
        public override float GetBoundingSphereRadiusSquared => (size * 0.5f).LengthSquared();


        public override void TryGetParametersFromPhysicsCollider()
        {
            CollisionShape3D col = GetParent() as CollisionShape3D;
            if (col == null)
            {
                return;
            }

            BoxShape3D box = (BoxShape3D)col.Shape;

            if (box == null)
            {
                return;
            }

            size = box.Size;
        }


        #region Raycasting

        public override bool ColliderCastLive(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit)
        {
            if (BoxTest(GlobalPosition, GlobalQuaternion, size, rayOrigin, rayDirection, out hit))
            {
                return hit.entryDistance <= range && hit.entryDistance >= 0f;
            }
            else
            {
                return false;
            }

        }

        public override bool ColliderCastCached(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit)
        {


            if (BoxTest(_cachedPosRot.position, _cachedPosRot.rotation, size, rayOrigin, rayDirection, out hit))
            {
                return hit.entryDistance <= range && hit.entryDistance >= 0f;
            }
            else
            {
                return false;
            }

        }




        /// <summary>
        /// Parametric raycast at box given by transform, center and size
        /// </summary>
        /// <returns></returns>
        private static bool BoxTest(Vector3 boxPosition, Quaternion boxRotation, Vector3 boxSize, Vector3 rayOrigin, Vector3 rayDirection, out ColliderCastHit _hit)
        {
            Quaternion _inverseRotation = boxRotation.Inverse();

            // forward ray
            Vector3 rayOriginTransformed = _inverseRotation * (rayOrigin - boxPosition);
            Vector3 rayDirectionTransformed = _inverseRotation * rayDirection;

            // backward ray
            Vector3 rayOriginOpposite = rayOriginTransformed + rayDirectionTransformed * 9999f;
            Vector3 rayDirectionOpposite = -rayDirectionTransformed;

            Aabb boundingBox = new Aabb(-0.5f * boxSize, 1f * boxSize);

            bool _hitBoolean = CheckBoxIntersection(boundingBox, rayOriginTransformed, rayDirectionTransformed, out float entryDistance);

            if (_hitBoolean) // --> figure out exit point and normals of entry and exit
            {
                CheckBoxIntersection(boundingBox, rayOriginOpposite, rayDirectionOpposite, out float exitDistance);
                exitDistance = 9999f - exitDistance;

                _hit = new ColliderCastHit() { entryPoint = rayOrigin + rayDirection * entryDistance, entryDistance = entryDistance, exitPoint = rayOrigin + rayDirection * exitDistance, exitDistance = exitDistance };

                // entry point normal calculation

                Vector3 _localEntryPoint = rayOriginTransformed + rayDirectionTransformed * entryDistance;
                Vector3 _deltaLocalEntryPointAbs = new Vector3(0.5f * boxSize.X - Mathf.Abs(_localEntryPoint.X), 0.5f * boxSize.Y - Mathf.Abs(_localEntryPoint.Y), 0.5f * boxSize.Z - Mathf.Abs(_localEntryPoint.Z));

                if (_deltaLocalEntryPointAbs.X < _deltaLocalEntryPointAbs.Y && _deltaLocalEntryPointAbs.X < _deltaLocalEntryPointAbs.Z) // smallest delta is in x direction
                {
                    _hit.entryNormal = boxRotation * Vector3.Right * Mathf.Sign(_localEntryPoint.X);
                }
                else if (_deltaLocalEntryPointAbs.Y < _deltaLocalEntryPointAbs.X && _deltaLocalEntryPointAbs.Y < _deltaLocalEntryPointAbs.Z) // smallest delta is in y direction
                {
                    _hit.entryNormal = boxRotation * Vector3.Up * Mathf.Sign(_localEntryPoint.Y);
                }
                else // smallest delta is in z direction
                {
                    _hit.entryNormal = boxRotation * Vector3.Back * Mathf.Sign(_localEntryPoint.Z); // for some reason Vector3.Forward had to be replaced by Vector3.Back
                }

                // exit point normal calculation

                Vector3 _LocalExitPoint = rayOriginTransformed + rayDirectionTransformed * exitDistance;
                Vector3 _deltaLocalExitPointAbs = new Vector3(0.5f * boxSize.X - Mathf.Abs(_LocalExitPoint.X), 0.5f * boxSize.Y - Mathf.Abs(_LocalExitPoint.Y), 0.5f * boxSize.Z - Mathf.Abs(_LocalExitPoint.Z));

                if (_deltaLocalExitPointAbs.X < _deltaLocalExitPointAbs.Y && _deltaLocalExitPointAbs.X < _deltaLocalExitPointAbs.Z) // smallest delta is in x direction
                {
                    _hit.exitNormal = boxRotation * Vector3.Right * Mathf.Sign(_LocalExitPoint.X);
                }
                else if (_deltaLocalExitPointAbs.Y < _deltaLocalExitPointAbs.X && _deltaLocalExitPointAbs.Y < _deltaLocalExitPointAbs.Z) // smallest delta is in y direction
                {
                    _hit.exitNormal = boxRotation * Vector3.Up * Mathf.Sign(_LocalExitPoint.Y);
                }
                else // smallest delta is in z direction
                {
                    _hit.exitNormal = boxRotation * Vector3.Back * Mathf.Sign(_LocalExitPoint.Z); // in godot, Vector3.Back = new Vector3(0, 0, 1)
                }

            }
            else
            {
                _hit = new ColliderCastHit();

            }


            return _hitBoolean;
        }

        /// <summary>
        /// Check if ray intersects with bounding box and give intersection distance
        /// </summary>
        /// <param name="boundingBox">Bounding box to check against.</param>
        /// <param name="origin">Ray origin.</param>
        /// <param name="direction">Normalized ray direction.</param>
        /// <param name="distance">Intersection distance.</param>
        /// <returns></returns>
        public static bool CheckBoxIntersection(Aabb boundingBox, Vector3 origin, Vector3 direction, out float distance)
        {
            distance = 0f;
            Vector3 invDir = new Vector3(1f / direction.X, 1f / direction.Y, 1f / direction.Z);
            Vector3 start = boundingBox.Position;
            Vector3 end = boundingBox.Position + boundingBox.Size;

            float t1 = (start.X - origin.X) * invDir.X;
            float t2 = (end.X - origin.X) * invDir.X;
            float t3 = (start.Y - origin.Y) * invDir.Y;
            float t4 = (end.Y - origin.Y) * invDir.Y;
            float t5 = (start.Z - origin.Z) * invDir.Z;
            float t6 = (end.Z - origin.Z) * invDir.Z;

            float tMin = Mathf.Max(Mathf.Max(Mathf.Min(t1, t2), Mathf.Min(t3, t4)), Mathf.Min(t5, t6));
            float tMax = Mathf.Min(Mathf.Min(Mathf.Max(t1, t2), Mathf.Max(t3, t4)), Mathf.Max(t5, t6));

            if (tMax < 0 || tMin > tMax)
                return false;

            distance = (tMin < 0) ? tMax : tMin;
            return true;
        }

        #endregion



        #region Debug Draw

        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            ColliderDrawing.DebugDrawBox(position, rotation, size, duration, col, editorGizmo);
        }

        

        #endregion
    }



}