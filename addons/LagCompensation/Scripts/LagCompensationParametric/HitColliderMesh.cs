using Godot;
using PG.LagCompensation.Base;
using System;
using static Godot.TextServer;

namespace PG.LagCompensation.Parametric
{
    /// <summary>
    /// HitCollider with custom Mesh, uses ray intersection checking for each triangle face of the mesh
    /// </summary>
    [GlobalClass]
    [Tool]
    public partial class HitColliderMesh : HitColliderGeneric
    {
        [Export]
        private Mesh mesh;

        [Export]
        private Vector3 meshScale = new Vector3(1f, 1f, 1f);

        private Vector3[] _vertices;

        /// <summary>
        /// Initialzed mesh array and bounding sphere radius?
        /// </summary>
        private bool _initialized;


        private float _boundingSphereRadius;
        private float _boundingSphereRadiusSquared;

        public override float GetBoundingSphereRadius
        {
            get
            {
                TryInitialize();
                return _boundingSphereRadius;
            }
        }
        public override float GetBoundingSphereRadiusSquared
        {
            get
            {
                TryInitialize();
                return _boundingSphereRadiusSquared;
            }
        }

        #region Initialization

        /// <summary>
        /// Intitialize mesh vertice array of faces and bounding sphere radius
        /// </summary>
        private void TryInitialize()
        {
            if (_initialized)
            {
                return;
            }

            if (mesh != null)
            {
                _vertices = mesh.GetFaces();
                for (int i = 0; i < _vertices.Length; i++)
                {
                    // apply scale to cached mesh vertices
                    _vertices[i] = _vertices[i] * meshScale;
                }
            }
            else
            {
                // vertices array stays as it is, should have been assigned by TryGetParametersFromPhysicsCollider()
            }

            CalculateBoundingSphereRadius();

            _initialized = true;
        }

        private void CalculateBoundingSphereRadius()
        {
            if (_vertices == null)
            {
                GD.PrintErr("HitColliderMesh " + this.Name + " has no mesh vertices");
            }

            for (int i = 0; i < _vertices.Length; i++)
            {
                // distance of vertice from origin should be the minimum bounding sphere radius
                float length = _vertices[i].Length();
                float lengthSquared = _vertices[i].LengthSquared();

                if (length > _boundingSphereRadius)
                {
                    _boundingSphereRadius = length;
                    _boundingSphereRadiusSquared = lengthSquared;
                }
            }
        }

        public override void TryGetParametersFromPhysicsCollider()
        {
            CollisionShape3D col = GetParent() as CollisionShape3D;
            if (col == null)
            {
                return;
            }

            ConcavePolygonShape3D concave = (ConcavePolygonShape3D)col.Shape;

            if (concave != null)
            {
                _vertices = concave.GetFaces();
                CalculateBoundingSphereRadius();
                return;
            }

            ConvexPolygonShape3D convex = (ConvexPolygonShape3D)col.Shape;

            if (convex != null)
            {
                _vertices = convex.GetDebugMesh().GetFaces();
                CalculateBoundingSphereRadius();
                return;
            }


        }

        #endregion

        #region Raycasting





        public override bool ColliderCastLive(Vector3 rayOrigin, Vector3 rayDirection, float range, out ColliderCastHit hit)
        {
            TryInitialize();

            if (MeshTest(GlobalPosition, GlobalQuaternion, _vertices, rayOrigin, rayDirection, out hit))
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
            TryInitialize();

            if (MeshTest(_cachedPosRot.position, _cachedPosRot.rotation, _vertices, rayOrigin, rayDirection, out hit))
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
        private static bool MeshTest(Vector3 meshPosition, Quaternion meshRotation, Vector3[] meshVertices, Vector3 rayOrigin, Vector3 rayDirection, out ColliderCastHit hit)
        {
            Quaternion inverseRotation = meshRotation.Inverse();

            // transform ray to compensate for meshPosition and meshRotation
            Vector3 rayOriginTransformed = inverseRotation * (rayOrigin - meshPosition);
            Vector3 rayDirectionTransformed = inverseRotation * rayDirection;

            hit = ColliderCastHit.Zero; // intialize hit with infinite entry distances

            bool hitAnything = false;

            for (int i = 0; i < meshVertices.Length; i += 3)
            {
                bool localHit = TriangleIntersect(in rayOriginTransformed, in rayDirectionTransformed,
                in meshVertices[i], in meshVertices[i + 1], in meshVertices[i + 2],
                out float t, out float u, out float v, out Vector3 N
                );

                if (localHit)
                {
                    hitAnything = true;

                    N = -N; // flip the normal vector

                    // check using dot product if the face normal and the ray direction point in opposite directions.
                    // If opposite --> Hit the front of the face
                    // If same direction --> Hit the back of the face
                    bool hitFrontFace = N.Dot(rayDirectionTransformed) < 0f; 

                    if (hitFrontFace)
                    {
                        if (t < hit.entryDistance)
                        {
                            hit.entryDistance = t;
                            hit.entryNormal = N; // TODO: check if this needs to be normalized
                        }
                    }
                    else // hit back face
                    {
                        if (t < hit.exitDistance)
                        {
                            hit.exitDistance = t;
                            hit.exitNormal = N; // TODO: check if this needs to be normalized
                        }
                    }
                }
            }

            if (!hitAnything)
            {
                return false;
            }

            hit.entryPoint = rayOrigin + rayDirection * hit.entryDistance;
            hit.exitPoint = rayOrigin + rayDirection * hit.exitDistance;

            // correct normal vector rotations
            hit.entryNormal = meshRotation * hit.entryNormal;
            hit.exitNormal = meshRotation * hit.exitNormal;

            return true;
        }

        public bool CheckRayIntersection(Vector3 origin, Vector3 direction,
        out Vector3 closestHitPoint, out Vector3 currentHitNormal)
        {
            //TriangleMesh tris = mesh.GenerateTriangleMesh();

            Vector3[] vertices = mesh.GetFaces(); // array with length 3 * number of faces

            bool hitAnything = false;
            float closestHitDistance = Mathf.Inf;
            currentHitNormal = new Vector3(0, 0, 1); // default value when no hit

            for (int i = 0; i < vertices.Length; i += 3)
            {
                bool hit = TriangleIntersect(in origin, in direction,
                in vertices[i], in vertices[i + 1], in vertices[i + 2],
                out float t, out float u, out float v, out Vector3 N
                );

                if (hit)
                {
                    hitAnything = true;
                    if (t < closestHitDistance)
                    {
                        closestHitDistance = t;
                        currentHitNormal = N; // TODO: check if this needs to be noemalized
                    }
                }
            }

            closestHitPoint = origin + direction * closestHitDistance;

            return hitAnything;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction (must be normalized)</param>
        /// <param name="A">Triangle corner 1</param>
        /// <param name="B">Triangle corner 2</param>
        /// <param name="C">Triangle corner 3</param>
        /// <param name="t">Distance along ray for intersection</param>
        /// <param name="u">Value along first plane axis</param>
        /// <param name="v">Value along second plane axis</param>
        /// <param name="N">Normal vector of plane</param>
        /// <returns>Does the given ray intersect the given triangle?</returns>
        public static bool TriangleIntersect(in Vector3 origin, in Vector3 direction, in Vector3 A, in Vector3 B, in Vector3 C, out float t, out float u, out float v, out Vector3 N)
        {
            // solution 3) of the highest rated reply
            //https://stackoverflow.com/questions/42740765/intersection-between-line-and-triangle-in-3d
            // which is based on:
            // Möller and Trumbore, "Fast, Minimum Storage Ray-Triangle Intersection", Journal of Graphics Tools, vol. 2, 1997, p. 21–28 
            // (see also: https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm)

            /*
            GLSL

            bool intersect_triangle(
            in Ray R, in vec3 A, in vec3 B, in vec3 C, out float t, 
            out float u, out float v, out vec3 N
            ) { 
            vec3 E1 = B-A;
            vec3 E2 = C-A;
                    N = cross(E1,E2);
            float det = -dot(R.Dir, N);
            float invdet = 1.0/det;
            vec3 AO  = R.Origin - A;
            vec3 DAO = cross(AO, R.Dir);
            u =  dot(E2,DAO) * invdet;
            v = -dot(E1,DAO) * invdet;
            t =  dot(AO,N)  * invdet; 
            return (det >= 1e-6 && t >= 0.0 && u >= 0.0 && v >= 0.0 && (u+v) <= 1.0);
            }
            */

            Vector3 E1 = B - A; // first vector defining the plane in which the face lies
            Vector3 E2 = C - A; // second vector defining the plane in which the face lies
            N = E1.Cross(E2); // normal vector of plane
            float det = -direction.Dot(N);
            float invdet = 1.0f / det;
            Vector3 AO = origin - A;
            Vector3 DAO = AO.Cross(direction);
            u = E2.Dot(DAO) * invdet;
            v = -E1.Dot(DAO) * invdet;
            t = AO.Dot(N) * invdet;
            return (Mathf.Abs(det) >= 1e-6 && t >= 0.0 && u >= 0.0 && v >= 0.0 && (u + v) <= 1.0);
        }

        #endregion



        #region Debug Draw

        public override void DebugDraw(Vector3 position, Quaternion rotation, float duration, Color col, bool editorGizmo = false)
        {
            if (mesh == null && _vertices == null)
            {
                // no reference mesh and no vertice array 
                return;
            }

            Vector3[] _debugVertices;
            if (mesh != null)
            {
                _debugVertices = mesh.GetFaces();
                for (int i = 0; i < _debugVertices.Length; i++)
                {
                    // apply scale to cached mesh vertices
                    _debugVertices[i] = _debugVertices[i] * meshScale;
                }
            }
            else
            {
                _debugVertices = _vertices; // use vertice array which must have been set via 'TryGetParametersFromPhysicsCollider()'
            }


            ColliderDrawing.DebugDrawMesh(position, rotation, _debugVertices, duration, col, editorGizmo);
        }



        #endregion

    }

}
