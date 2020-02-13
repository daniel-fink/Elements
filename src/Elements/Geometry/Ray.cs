using System;
using Elements.Geometry.Solids;

namespace Elements.Geometry
{
    /// <summary>
    /// An infinite ray starting at origin and pointing towards direction.
    /// </summary>
    public class Ray : IEquatable<Ray>
    {
        /// <summary>
        /// The origin of the ray.
        /// </summary>
        public Vector3 Origin { get; set; }

        /// <summary>
        /// The direction of the ray.
        /// </summary>
        public Vector3 Direction { get; set; }

        /// <summary>
        /// Construct a ray.
        /// </summary>
        /// <param name="origin">The origin of the ray.</param>
        /// <param name="direction">The direction of the ray.</param>
        public Ray(Vector3 origin, Vector3 direction)
        {
            this.Origin = origin;
            this.Direction = direction;
        }

        /// <summary>
        /// https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm
        /// </summary>
        /// <param name="tri">The triangle to intersect.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the type and location of intersection.</returns>
        public bool Intersects(Triangle tri, out Vector3 result)
        {
            result = default(Vector3);

            var vertex0 = tri.Vertices[0].Position;
            var vertex1 = tri.Vertices[1].Position;
            var vertex2 = tri.Vertices[2].Position;
            var edge1 = (vertex1 - vertex0);
            var edge2 = (vertex2 - vertex0);
            var h = this.Direction.Cross(edge2);
            var s = this.Origin - vertex0;
            double a, f, u, v;

            a = edge1.Dot(h);
            if (a > -Vector3.EPSILON && a < Vector3.EPSILON)
            {
                return false;    // This ray is parallel to this triangle.
            }
            f = 1.0 / a;
            u = f * (s.Dot(h));
            if (u < 0.0 || u > 1.0)
            {
                return false;
            }
            var q = s.Cross(edge1);
            v = f * this.Direction.Dot(q);
            if (v < 0.0 || u + v > 1.0)
            {
                return false;
            }
            // At this stage we can compute t to find out where the intersection point is on the line.
            double t = f * edge2.Dot(q);
            if (t > Vector3.EPSILON && t < 1 / Vector3.EPSILON) // ray intersection
            {
                result = this.Origin + this.Direction * t;
                return true;
            }
            else // This means that there is a line intersection but not a ray intersection.
            {
                return false;
            }
        }

        /// <summary>
        /// Does this ray intersect with the provided SolidOperation?
        /// </summary>
        /// <param name="solidOp">The SolidOperation to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        public bool Intersects(SolidOperation solidOp, out Vector3 result)
        {
            var intersects = Intersects(solidOp.GetSolid(), out Vector3 tempResult);
            result = tempResult;
            return intersects;
        }

        /// <summary>
        /// Does this ray intersect with the provided Solid? 
        /// </summary>
        /// <param name="solid">The Solid to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        internal bool Intersects(Solid solid, out Vector3 result)
        {
            var distanceToClosestResult = double.MaxValue;
            var faces = solid.Faces;
            var intersects = false;
            var finalResult = default(Vector3);
            foreach (var face in faces)
            {
                if (Intersects(face.Value, out Vector3 tempResult))
                {
                    intersects = true;
                    var newDistance = Origin.DistanceTo(tempResult);
                    if (newDistance < distanceToClosestResult)
                    {
                        distanceToClosestResult = newDistance;
                        finalResult = tempResult;
                    }
                }
            }
            result = finalResult;
            return intersects;
        }

        /// <summary>
        /// Does this ray intersect with the provided face?
        /// </summary>
        /// <param name="face">The Face to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false. If true, check the intersection result for the location of the intersection.</returns>
        internal bool Intersects(Face face, out Vector3 result)
        {
            var plane = face.Plane();
            if (Intersects(plane, out Vector3 intersection))
            {
                var boundaryPolygon = face.Outer.ToPolygon();
                var transformToPolygon = new Transform(plane.Origin, plane.Normal);
                var transformFromPolygon = new Transform(transformToPolygon);
                transformFromPolygon.Invert();
                var transformedPolygon = transformFromPolygon.OfPolygon(boundaryPolygon);
                var transformedIntersection = transformFromPolygon.OfVector(intersection);
                if(transformedPolygon.Contains(transformedIntersection))
                {
                    result = intersection;
                    return true;
                }
            }
            result = default;
            return false;
        }

        /// <summary>
        /// Does this ray intersect the provided plane?
        /// </summary>
        /// <param name="plane">The Plane to intersect with.</param>
        /// <param name="result">The intersection result.</param>
        /// <returns>True if an intersection occurs, otherwise false — this can occur if the ray is very close to parallel to the plane.
        /// If true, check the intersection result for the location of the intersection.</returns>
        public bool Intersects(Plane plane, out Vector3 result)
        {
            // adapted from https://www.scratchapixel.com/lessons/3d-basic-rendering/minimal-ray-tracer-rendering-simple-shapes/ray-plane-and-ray-disk-intersection
            var l0 = Origin;
            var l = Direction;
            var p0 = plane.Origin;
            var n = plane.Normal;
            var denom = n.Dot(l);
            if (Math.Abs(denom) < Vector3.EPSILON)
            {
                result = default;
                return false;
            }
            var p0l0 = p0 - l0;
            var t = p0l0.Dot(n) / denom;
            result = Origin + t * l;
            return t >= Vector3.EPSILON;

        }

        /// <summary>
        /// Does this ray intersect the provided topography?
        /// </summary>
        /// <param name="topo">The topography.</param>
        /// <param name="result">The location of intersection.</param>
        /// <returns>True if an intersection result occurs.
        /// The type of intersection should be checked in the intersection result. 
        /// False if no intersection occurs.</returns>
        public bool Intersects(Topography topo, out Vector3 result)
        {
            result = default(Vector3);
            foreach (var t in topo.Mesh.Triangles)
            {
                if (this.Intersects(t, out result))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Does this ray intersect the provided ray?
        /// </summary>
        /// <param name="ray">The ray to intersect.</param>
        /// <param name="result">The location of intersection.</param>
        /// <param name="ignoreRayDirection">If true, the direction of the rays will be ignored</param>
        /// <returns>True if the rays intersect, otherwise false.</returns>
        public bool Intersects(Ray ray, out Vector3 result, bool ignoreRayDirection = false)
        {
            var p1 = this.Origin;
            var p2 = ray.Origin;
            var d1 = this.Direction;
            var d2 = ray.Direction;

            if (d1.IsParallelTo(d2))
            {
                result = default(Vector3);
                return false;
            }

            var t1 = (((p2 - p1).Cross(d2)).Dot(d1.Cross(d2))) / Math.Pow(d1.Cross(d2).Length(), 2);
            var t2 = (((p2 - p1).Cross(d1)).Dot(d1.Cross(d2))) / Math.Pow(d1.Cross(d2).Length(), 2);
            result = p1 + d1 * t1;
            if (ignoreRayDirection)
            {
                return true;
            }
            return t1 >= 0 && t2 >= 0;
        }

        /// <summary>
        /// Is this ray equal to the provided ray?
        /// </summary>
        /// <param name="other">The ray to test.</param>
        /// <returns>Returns true if the two rays are equal, otherwise false.</returns>
        public bool Equals(Ray other)
        {
            if (other == null)
            {
                return false;
            }
            return this.Origin.Equals(other.Origin) && this.Direction.Equals(other.Direction);
        }
    }
}