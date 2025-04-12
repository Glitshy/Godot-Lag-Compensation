using System.Collections;
using System.Collections.Generic;
using Godot;
using PG.LagCompensation.Base;

namespace PG.LagCompensation.Parametric
{
    [GlobalClass]
    [Tool]
    public partial class HitColliderCylinder : HitColliderGeneric
    {
        
        private float _height = 2f;

        private float _radius = 0.5f;

        [Export]
        float height
        {
            get { return _height; }
            set
            {
                _height = value;
                if (Engine.IsEditorHint())
                {
                    UpdateGizmos();
                }
            }
        }

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
        /// Get height of capsule, e.g. for gizmo
        /// </summary>
        public float GetHeight => _height;
        /// <summary>
        /// Set height of capsule, e.g. for gizmo
        /// </summary>
        public float SetHeight { set => _height = value; }
        /// <summary>
        /// Get radius of capsule, e.g. for gizmo
        /// </summary>
        public float GetRadius => _radius;
        /// <summary>
        /// Set radius of capsule, e.g. for gizmo
        /// </summary>
        public float SetRadius { set => _radius = value; }


        //public override float GetBoundingSphereRadius => Mathf.Sqrt(_height * _height * 0.25f + radius * radius); // pythagorean theorem
        //public override float GetBoundingSphereRadiusSquared => _height * _height * 0.25f + radius * radius; // pythagorean theorem squared

        public override float GetBoundingSphereRadius => _height * 0.5f;
        public override float GetBoundingSphereRadiusSquared => _height * _height * 0.25f;

        public override void TryGetParametersFromPhysicsCollider()
        {
            CollisionShape3D col = GetParent() as CollisionShape3D;
            if (col == null)
            {
                return;
            }

            CylinderShape3D shape = (CylinderShape3D)col.Shape;

            if (shape == null)
            {
                return;
            }

            _radius = shape.Radius;
            _height = shape.Height;
        }

        #region Raycasting

        public override bool ColliderCastLive(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit)
        {
            if (ParametricRaycastCylinder(GlobalPosition, GlobalQuaternion, _height, _radius, rayOrigin, rayDirection, out hit))
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
            if (ParametricRaycastCylinder(_cachedPosRot.position, _cachedPosRot.rotation, _height, _radius, rayOrigin, rayDirection, out hit))
            {
                return hit.entryDistance <= range && hit.entryDistance >= 0f;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Get global direction for height based on given global rotation
        /// </summary>
        private static Vector3 GetCylinderDirection(Quaternion _rotation)
        {
            return _rotation * Vector3.Up;
        }

        /// <summary>
        /// Get global direction for height based on current transform
        /// </summary>
        private Vector3 GetCylinderDirectionLive()
        {
            return GlobalBasis.Y;
        }


        private static bool ParametricRaycastCylinder(Vector3 position, Quaternion rotation, float height, float radius, Vector3 o, Vector3 d, out ColliderCastHit hit)
        {
            // using https://mathworld.wolfram.com/Point-LineDistance3-Dimensional.html

            Vector3 centerPosition = position;
            Vector3 cylinderDirection = GetCylinderDirection(rotation);



            hit = new ColliderCastHit();

            //d = d.normalized; // normalize direction vector

            Vector3 a = centerPosition - cylinderDirection * height * 0.5f;
            Vector3 b = centerPosition + cylinderDirection * height * 0.5f;

            Vector3 A = b - a;
            Vector3 k = a - o;

            float _inverseDenominator = 1f / (A.X * A.X + A.Y * A.Y + A.Z * A.Z);

            float sol_a = (A.Y * A.Y * d.Z * d.Z - 2 * A.Y * A.Z * d.Z * d.Y + A.Z * A.Z * d.Y * d.Y
                        + A.Z * A.Z * d.X * d.X - 2 * A.Z * A.X * d.X * d.Z + A.X * A.X * d.Z * d.Z
                        + A.X * A.X * d.Y * d.Y - 2 * A.X * A.Y * d.Y * d.X + A.Y * A.Y * d.X * d.X) * _inverseDenominator;

            float sol_b = (A.Y * A.Y * (-2 * k.Z * d.Z) - 2 * A.Y * A.Z * (-k.Z * d.Y - d.Z * k.Y) + A.Z * A.Z * (-2 * k.Y * d.Y)
                        + A.Z * A.Z * (-2 * k.X * d.X) - 2 * A.Z * A.X * (-k.X * d.Z - d.X * k.Z) + A.X * A.X * (-2 * k.Z * d.Z)
                        + A.X * A.X * (-2 * k.Y * d.Y) - 2 * A.X * A.Y * (-k.Y * d.X - d.Y * k.X) + A.Y * A.Y * (-2 * k.X * d.X)) * _inverseDenominator;

            float sol_c = (A.Y * A.Y * k.Z * k.Z - 2 * A.Y * A.Z * k.Z * k.Y + A.Z * A.Z * k.Y * k.Y
                        + A.Z * A.Z * k.X * k.X - 2 * A.Z * A.X * k.X * k.Z + A.X * A.X * k.Z * k.Z
                        + A.X * A.X * k.Y * k.Y - 2 * A.X * A.Y * k.Y * k.X + A.Y * A.Y * k.X * k.X) * _inverseDenominator
                        - radius * radius;



            hit.exitDistance = ColliderMath.QuadForm(sol_a, sol_b, sol_c, true);
            hit.entryDistance = ColliderMath.QuadForm(sol_a, sol_b, sol_c, false);


            if (!float.IsNaN(hit.entryDistance)) // we have an entry hit
            {
                hit.entryPoint = o + hit.entryDistance * d; // entry point assuming we are in the cylinder region. Will be overriden if we are in plane "a" or plane "b"
                hit.exitPoint = o + hit.exitDistance * d; // exit point assuming we are in the cylinder region. Will be overriden if we are in plane "a" or plane "b"


                float t_entry = ColliderMath.GetTValueAlongLine(a, b, hit.entryPoint);
                float t_exit = ColliderMath.GetTValueAlongLine(a, b, hit.exitPoint);

                if (t_entry > 1f) // look at upper plane (centered at "b")
                {
                    if (t_exit > 1f) // both intersect points are above the cylinder
                    {
                        return false;
                    }
                    else
                    {
                        hit.entryNormal = cylinderDirection;
                        CircularPlaneIntersect(o, d, b, cylinderDirection, radius, out hit.entryDistance, out hit.entryPoint);

                        if (t_exit > 0f) // we are in the cylinder range of the cylinder
                        {
                            Vector3 _closestPoint = a + t_exit * (b - a);
                            hit.exitNormal = (hit.exitPoint - _closestPoint).Normalized();
                        }
                        else // look at lower plane (centered at "a")
                        {
                            hit.exitNormal = -cylinderDirection;
                            CircularPlaneIntersect(o, d, a, -cylinderDirection, radius, out hit.exitDistance, out hit.exitPoint);
                        }
                    }
                }
                else if (t_entry < 0f) // look at lower plane (centered at "a")
                {
                    if (t_exit < 0f) // both intersect points are below the cylinder
                    {
                        return false;
                    }
                    else
                    {
                        hit.entryNormal = -cylinderDirection;
                        CircularPlaneIntersect(o, d, a, -cylinderDirection, radius, out hit.entryDistance, out hit.entryPoint);
                        
                        if (t_exit < 1f) // we are in the cylinder range of the cylinder
                        {
                            Vector3 _closestPoint = a + t_exit * (b - a);
                            hit.exitNormal = (hit.exitPoint - _closestPoint).Normalized();
                        }
                        else // look at upper plane (centered at "b")
                        {
                            hit.exitNormal = cylinderDirection;
                            CircularPlaneIntersect(o, d, b, cylinderDirection, radius, out hit.exitDistance, out hit.exitPoint);
                        }

                    }
                }
                else // we are in the cylinder range of the cylinder
                {
                    Vector3 _closestPoint = a + t_entry * (b - a);

                    hit.entryNormal = (hit.entryPoint - _closestPoint).Normalized();
                    //Debug.DrawLine(_closestPoint, _hit.entryPoint, Color.blue);


                    if (t_exit > 1f) // look at upper plane (centered at "b")
                    {
                        hit.exitNormal = cylinderDirection;
                        CircularPlaneIntersect(o, d, b, cylinderDirection, radius, out hit.exitDistance, out hit.exitPoint);
                    }
                    else if (t_exit < 0f) // look at lower plane (centered at "a")
                    {
                        hit.exitNormal = -cylinderDirection;
                        CircularPlaneIntersect(o, d, a, -cylinderDirection, radius, out hit.exitDistance, out hit.exitPoint);
                    }
                    else // we are in the cylinder range of the cylinder
                    {
                        _closestPoint = a + t_exit * (b - a);
                        hit.exitNormal = (hit.exitPoint - _closestPoint).Normalized();
                    }
                }


                if (float.IsNaN(hit.entryDistance)) // we did not hit after all
                {
                    return false;
                }
                else // we did hit
                {
                    return true;
                }

            }
            else // we did not hit
            {
                return false;
            }



        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="rayOrigin">Ray origin</param>
        /// <param name="rayDirection">Ray direction (must be normalized)</param>
        /// <param name="t">Distance along ray for intersection</param>
        /// <returns>Does the given ray intersect the given plane within the radius bounds?</returns>
        public static bool CircularPlaneIntersect(in Vector3 rayOrigin, in Vector3 rayDirection, in Vector3 circleCenter, in Vector3 circleNormal, in float circleRadius, out float t, out Vector3 intersectPoint)
        {
            // based on answer to https://math.stackexchange.com/questions/3412199/how-to-calculate-the-intersection-point-of-a-vector-and-a-plane-defined-as-a-poi

            t = ((circleCenter - rayOrigin).Dot(circleNormal)) / (rayDirection.Dot(circleNormal)); // t value along ray

            intersectPoint = rayOrigin + t * rayDirection;

            float distanceSquared = intersectPoint.DistanceSquaredTo(circleCenter);

            if (t < 0f)
            {
                GD.Print("t= " + t + " center " + circleCenter + " circleNormal " + circleNormal + " intersect " + intersectPoint);
            }

            return (distanceSquared <= circleRadius * circleRadius);
        }

        #endregion




        #region Debug Draw

        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            ColliderDrawing.DebugDrawCylinder(position, rotation, _height, _radius, duration, col, editorGizmo);
        }


        #endregion

    }

}