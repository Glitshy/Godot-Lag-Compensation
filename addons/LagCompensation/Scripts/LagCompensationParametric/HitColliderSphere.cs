using System.Collections;
using System.Collections.Generic;
using Godot;
using PG.LagCompensation.Base;

namespace PG.LagCompensation.Parametric
{
    [GlobalClass]
    [Tool]
    public partial class HitColliderSphere : HitColliderGeneric
    {
        private float _radius = 0.5f;

        [Export]
        float radius
        {
            get { return _radius; }
            set
            {
                _radius = value;
                if (Engine.IsEditorHint())
                {
                    UpdateGizmos();
                }
            }
        }

        /// <summary>
        /// Get radius of sphere, e.g. for gizmo
        /// </summary>
        public float GetRadius => _radius;
        /// <summary>
        /// Set radius of sphere, e.g. for gizmo
        /// </summary>
        public float SetRadius { set => _radius = value; }


        public override float GetBoundingSphereRadius => _radius;
        public override float GetBoundingSphereRadiusSquared => _radius * _radius;


        public override void TryGetParametersFromPhysicsCollider()
        {
            CollisionShape3D col = GetParent() as CollisionShape3D;
            if (col == null)
            {
                return;
            }

            SphereShape3D sph = (SphereShape3D)col.Shape;

            if (sph == null)
            {
                return;
            }

            _radius = sph.Radius;
        }


        #region Raycasting

        public override bool ColliderCastLive(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit)
        {
            if (ParametricRaycastSphereBothSided(GlobalPosition, _radius, rayOrigin, rayDirection, out hit.entryPoint, out hit.entryNormal, out hit.entryDistance, out hit.exitPoint, out hit.exitNormal, out hit.exitDistance))
            {
                return hit.entryDistance <= range && hit.entryDistance >= 0f;
            }
            else
            {
                //hit = new ColliderCastHit();
                return false;
            }
        }


        public override bool ColliderCastCached(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit)
        {
            if (ParametricRaycastSphereBothSided(_cachedPosRot.position, _radius, rayOrigin, rayDirection, out hit.entryPoint, out hit.entryNormal, out hit.entryDistance, out hit.exitPoint, out hit.exitNormal, out hit.exitDistance))
            {
                return hit.entryDistance <= range && hit.entryDistance >= 0f;
            }
            else
            {
                hit = new ColliderCastHit();
                return false;
            }
        }

        #endregion

        #region Interpolation


        #endregion


        #region Debug Draw

        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            ColliderDrawing.DebugDrawSphere(position, rotation, _radius, duration, col, editorGizmo);

        }

        #endregion
    }


}