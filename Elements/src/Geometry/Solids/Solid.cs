#pragma warning disable 1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Elements.Geometry.Interfaces;
using LibTessDotNet.Double;

[assembly: InternalsVisibleTo("Hypar.Elements.Tests")]

namespace Elements.Geometry.Solids
{
    /// <summary>
    /// A boundary representation of a solid.
    /// </summary>
    public class Solid : ITessellate
    {
        private long _faceId;
        private long _edgeId = 10000;
        private long _vertexId = 100000;

        /// <summary>
        /// The Faces of the Solid.
        /// </summary>
        public Dictionary<long, Face> Faces { get; }

        /// <summary>
        /// The edges of the solid.
        /// </summary>
        public Dictionary<long, Edge> Edges { get; }

        /// <summary>
        /// The vertices of the solid.
        /// </summary>
        public Dictionary<long, Vertex> Vertices { get; }

        /// <summary>
        /// Construct a solid.
        /// </summary>
        public Solid()
        {
            this.Faces = new Dictionary<long, Face>();
            this.Edges = new Dictionary<long, Edge>();
            this.Vertices = new Dictionary<long, Vertex>();
        }

        /// <summary>
        /// Construct a lamina solid.
        /// </summary>
        /// <param name="perimeter">The perimeter of the lamina's faces.</param>
        public static Solid CreateLamina(IList<Vector3> perimeter)
        {
            return CreateLamina(new Polygon(perimeter));
        }

        public static Solid CreateLamina(Polygon perimeter, IList<Polygon> voids = null)
        {
            var solid = new Solid();
            if (voids != null && voids.Count > 0)
            {
                solid.AddFace(perimeter, voids);
                solid.AddFace(perimeter.Reversed(), voids.Select(h => h.Reversed()).ToArray(), true);
            }
            else
            {
                solid.AddFace(perimeter);
                solid.AddFace(perimeter.Reversed(), null, true);
            }

            return solid;
        }

        public static Solid CreateLamina(Profile profile)
        {
            return CreateLamina(profile.Perimeter, profile.Voids);
        }

        /// <summary>
        /// Construct a solid by sweeping a face.
        /// </summary>
        /// <param name="perimeter">The perimeter of the face to sweep.</param>
        /// <param name="holes">The holes of the face to sweep.</param>
        /// <param name="distance">The distance to sweep.</param>
        /// <param name="bothSides">Should the sweep start offset by direction distance/2? </param>
        /// <param name="rotation">An optional rotation in degrees of the perimeter around the z axis.</param>
        /// <returns>A solid.</returns>
        public static Solid SweepFace(Polygon perimeter,
                                      IList<Polygon> holes,
                                      double distance,
                                      bool bothSides = false,
                                      double rotation = 0.0)
        {
            return Solid.SweepFace(perimeter, holes, Vector3.ZAxis, distance, bothSides, rotation);
        }

        /// <summary>
        /// Construct a solid by sweeping a face along a curve.
        /// </summary>
        /// <param name="perimeter">The perimeter of the face to sweep.</param>
        /// <param name="holes">The holes of the face to sweep.</param>
        /// <param name="curve">The curve along which to sweep.</param>
        /// <param name="startSetback">The setback distance of the sweep from the start of the curve.</param>
        /// <param name="endSetback">The setback distance of the sweep from the end of the curve.</param>
        /// <returns>A solid.</returns>
        public static Solid SweepFaceAlongCurve(Polygon perimeter,
                                                IList<Polygon> holes,
                                                ICurve curve,
                                                double startSetback = 0,
                                                double endSetback = 0)
        {
            var solid = new Solid();

            var l = curve.Length();

            // The start and end setbacks can't be more than
            // the length of the beam together.
            if ((startSetback + endSetback) >= l)
            {
                startSetback = 0;
                endSetback = 0;
            }

            // Calculate the setback parameter as a percentage
            // of the curve length. This will not work for curves
            // without non-uniform parameterization.
            var ssb = startSetback / l;
            var esb = endSetback / l;

            var transforms = curve.Frames(ssb, esb);

            if (curve is Polygon)
            {
                for (var i = 0; i < transforms.Length; i++)
                {
                    var next = i == transforms.Length - 1 ? transforms[0] : transforms[i + 1];
                    solid.SweepPolygonBetweenPlanes(perimeter, transforms[i], next);
                }
            }
            else if (curve is Bezier)
            {
                var startCap = solid.AddFace((Polygon)perimeter.Transformed(transforms[0]));
                for (var i = 0; i < transforms.Length - 1; i++)
                {
                    var next = transforms[i + 1];
                    solid.SweepPolygonBetweenPlanes(perimeter, transforms[i], next);
                }
                var endCap = solid.AddFace(((Polygon)perimeter.Transformed(transforms[transforms.Length - 1])).Reversed());
            }
            else
            {
                // Add start cap.
                Face cap = null;
                Edge[][] openEdges;

                if (holes != null)
                {
                    cap = solid.AddFace((Polygon)perimeter.Transformed(transforms[0]), transforms[0].OfPolygons(holes));
                    openEdges = new Edge[1 + holes.Count][];
                }
                else
                {
                    cap = solid.AddFace((Polygon)perimeter.Transformed(transforms[0]));
                    openEdges = new Edge[1][];
                }

                // last outer edge
                var openEdge = cap.Outer.GetLinkedEdges();
                openEdge = solid.SweepEdges(transforms, openEdge);
                openEdges[0] = openEdge;

                if (holes != null)
                {
                    for (var i = 0; i < cap.Inner.Length; i++)
                    {
                        openEdge = cap.Inner[i].GetLinkedEdges();

                        // last inner edge for one hole
                        openEdge = solid.SweepEdges(transforms, openEdge);
                        openEdges[i + 1] = openEdge;
                    }
                }

                solid.Cap(openEdges, true);
            }

            return solid;
        }

        /// <summary>
        /// Construct a solid by sweeping a face in a direction.
        /// </summary>
        /// <param name="perimeter">The perimeter of the face to sweep.</param>
        /// <param name="holes">The holes of the face to sweep.</param>
        /// <param name="direction">The direction in which to sweep.</param>
        /// <param name="distance">The distance to sweep.</param>
        /// <param name="bothSides">Should the sweep start offset by direction distance/2? </param>
        /// <param name="rotation">An optional rotation in degrees of the perimeter around the direction vector.</param>
        /// <returns>A solid.</returns>
        public static Solid SweepFace(Polygon perimeter,
                                      IList<Polygon> holes,
                                      Vector3 direction,
                                      double distance,
                                      bool bothSides = false,
                                      double rotation = 0.0)
        {
            // We do a difference of the polygons
            // to get the clipped shape. This will fail in interesting
            // ways if the clip creates two islands.
            // if(holes != null)
            // {
            //     var newPerimeter = perimeter.Difference(holes);
            //     perimeter = newPerimeter[0];
            //     holes = newPerimeter.Skip(1).Take(newPerimeter.Count - 1).ToArray();
            // }

            var solid = new Solid();
            Face fStart = null;
            if (bothSides)
            {
                var t = new Transform(direction.Negate() * (distance / 2), rotation);
                if (holes != null)
                {
                    fStart = solid.AddFace((Polygon)perimeter.Reversed().Transformed(t), t.OfPolygons(holes.Reversed()));
                }
                else
                {
                    fStart = solid.AddFace((Polygon)perimeter.Reversed().Transformed(t));
                }
            }
            else
            {
                if (holes != null)
                {
                    fStart = solid.AddFace(perimeter.Reversed(), holes.Reversed());
                }
                else
                {
                    fStart = solid.AddFace(perimeter.Reversed());
                }
            }

            var fEndOuter = solid.SweepLoop(fStart.Outer, direction, distance);

            if (holes != null)
            {
                var fEndInner = new Loop[holes.Count];
                for (var i = 0; i < holes.Count; i++)
                {
                    fEndInner[i] = solid.SweepLoop(fStart.Inner[i], direction, distance);
                }
                solid.AddFace(fEndOuter, fEndInner);
            }
            else
            {
                solid.AddFace(fEndOuter);
            }

            return solid;
        }

        /// <summary>
        /// Add a Vertex to the Solid.
        /// </summary>
        /// <param name="position"></param>
        /// <returns>The newly added vertex.</returns>
        public Vertex AddVertex(Vector3 position)
        {
            var v = new Vertex(_vertexId, position);
            this.Vertices.Add(_vertexId, v);
            _vertexId++;
            return v;
        }

        /// <summary>
        /// Add a Face to the Solid.
        /// </summary>
        /// <param name="outer">A polygon representing the perimeter of the face.</param>
        /// <param name="inner">An array of polygons representing the holes in the face.</param>
        /// <param name="mergeVerticesAndEdges">Should existing vertices / edges in the solid be used for the added face?</param>
        /// <returns>The newly added face.</returns>
        public Face AddFace(Polygon outer, IList<Polygon> inner = null, bool mergeVerticesAndEdges = false)
        {
            var outerLoop = LoopFromPolygon(outer, mergeVerticesAndEdges);
            Loop[] innerLoops = null;

            if (inner != null)
            {
                innerLoops = new Loop[inner.Count];
                for (var i = 0; i < inner.Count; i++)
                {
                    innerLoops[i] = LoopFromPolygon(inner[i], mergeVerticesAndEdges);
                }
            }

            var face = this.AddFace(outerLoop, innerLoops);
            return face;
        }

        /// <summary>
        /// Add an edge to the solid.
        /// </summary>
        /// <param name="from">The start vertex.</param>
        /// <param name="to">The end vertex.</param>
        /// <returns>The newly added edge.</returns>
        public Edge AddEdge(Vertex from, Vertex to)
        {
            var e = new Edge(_edgeId, from, to);
            this.Edges.Add(_edgeId, e);
            _edgeId++;
            return e;
        }

        private Edge AddEdge(Vertex from, Vertex to, bool useExistingEdges, out string edgeType)
        {
            if (useExistingEdges)
            {
                var matchingLeftEdge = Edges.Values.FirstOrDefault(e => e.Left.Vertex == from && e.Right.Vertex == to);
                if (matchingLeftEdge != null)
                {
                    edgeType = "left";
                    return matchingLeftEdge;
                }
                var matchingRightEdge = Edges.Values.FirstOrDefault(e => e.Right.Vertex == from && e.Left.Vertex == to);
                if (matchingRightEdge != null)
                {
                    edgeType = "right";
                    return matchingRightEdge;
                }
            }
            edgeType = "left";
            return AddEdge(from, to);
        }

        /// <summary>
        /// Add a face to the solid.
        /// Provided edges are expected to be wound CCW for outer,
        /// and CW for inner. The face will be linked to the edges.
        /// </summary>
        /// <param name="outer">The outer Loop of the Face.</param>
        /// <param name="inner">The inner Loops of the Face.</param>
        /// <returns>The newly added Face.</returns>
        public Face AddFace(Loop outer, Loop[] inner = null)
        {
            var f = new Face(_faceId, outer, inner);
            this.Faces.Add(_faceId, f);
            _faceId++;
            return f;
        }

        /// <summary>
        /// Creates a series of edges from a polygon.
        /// </summary>
        /// <param name="p"></param>
        public Edge[] AddEdges(Polygon p)
        {
            var loop = new Edge[p.Vertices.Count];
            var vertices = new Vertex[p.Vertices.Count];
            for (var i = 0; i < p.Vertices.Count; i++)
            {
                vertices[i] = AddVertex(p.Vertices[i]);
            }
            for (var i = 0; i < p.Vertices.Count; i++)
            {
                loop[i] = AddEdge(vertices[i], i == p.Vertices.Count - 1 ? vertices[0] : vertices[i + 1]);
            }
            return loop;
        }

        /// <summary>
        /// Slice a solid with the provided plane.
        /// </summary>
        /// <param name="p">The plane to be used to slice this solid.</param>
        internal void Slice(Plane p)
        {
            var keys = new List<long>(this.Edges.Keys);
            foreach (var key in keys)
            {
                var e = this.Edges[key];
                SplitEdge(p, e);
            }
        }

        /// <summary>
        /// Get the string representation of the solid.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Faces: {this.Faces.Count}, Edges: {this.Edges.Count}, Vertices: {this.Vertices.Count}");
            foreach (var e in Edges)
            {
                sb.AppendLine($"Edge: {e.ToString()}");
            }
            foreach (var f in Faces.Values)
            {
                sb.AppendLine($"Face: {f.ToString()}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get the Mesh of this Solid.
        /// </summary>
        public Mesh ToMesh()
        {
            var mesh = new Mesh();
            this.Tessellate(ref mesh);
            return mesh;
        }

        /// <summary>
        /// Triangulate this solid.
        /// </summary>
        /// <param name="mesh">The mesh to which the solid's tessellated data will be added.</param>
        /// <param name="transform">An optional transform used to transform the generated vertex coordinates.</param>
        /// <param name="color">An optional color to apply to the vertex.</param>
        public void Tessellate(ref Mesh mesh, Transform transform = null, Color color = default(Color))
        {
            foreach (var f in this.Faces.Values)
            {
                var tess = new Tess();
                tess.NoEmptyPolygons = true;

                tess.AddContour(f.Outer.ToContourVertexArray(f));

                if (f.Inner != null)
                {
                    foreach (var loop in f.Inner)
                    {
                        tess.AddContour(loop.ToContourVertexArray(f));
                    }
                }

                tess.Tessellate(WindingRule.Positive, LibTessDotNet.Double.ElementType.Polygons, 3);

                var faceMesh = new Mesh();
                (Vector3 U, Vector3 V) basis = (default(Vector3), default(Vector3));

                for (var i = 0; i < tess.ElementCount; i++)
                {
                    var a = tess.Vertices[tess.Elements[i * 3]].Position.ToVector3();
                    var b = tess.Vertices[tess.Elements[i * 3 + 1]].Position.ToVector3();
                    var c = tess.Vertices[tess.Elements[i * 3 + 2]].Position.ToVector3();

                    if (transform != null)
                    {
                        a = transform.OfPoint(a);
                        b = transform.OfPoint(b);
                        c = transform.OfPoint(c);
                    }

                    if (i == 0)
                    {
                        // Calculate the texture space basis vectors
                        // from the first triangle. This is acceptable
                        // for planar faces.
                        // TODO: Update this when we support non-planar faces.
                        // https://gamedev.stackexchange.com/questions/172352/finding-texture-coordinates-for-plane
                        var n = f.Plane().Normal;
                        basis = n.ComputeDefaultBasisVectors();
                    }

                    var v1 = faceMesh.AddVertex(a, new UV(basis.U.Dot(a), basis.V.Dot(a)), color: color);
                    var v2 = faceMesh.AddVertex(b, new UV(basis.U.Dot(b), basis.V.Dot(b)), color: color);
                    var v3 = faceMesh.AddVertex(c, new UV(basis.U.Dot(c), basis.V.Dot(c)), color: color);

                    faceMesh.AddTriangle(v1, v2, v3);
                }
                mesh.AddMesh(faceMesh);
            }
        }



        /// <summary>
        /// Triangulate this solid and pack the triangulated data into buffers
        /// appropriate for use with gltf.
        /// </summary>
        public GraphicsBuffers Tessellate()
        {
            var tessellations = new Tess[this.Faces.Count];

            var fi = 0;
            foreach (var f in this.Faces.Values)
            {
                var tess = new Tess();
                tess.NoEmptyPolygons = true;
                tess.AddContour(f.Outer.ToContourVertexArray(f));

                if (f.Inner != null)
                {
                    foreach (var loop in f.Inner)
                    {
                        tess.AddContour(loop.ToContourVertexArray(f));
                    }
                }

                tess.Tessellate(WindingRule.Positive, LibTessDotNet.Double.ElementType.Polygons, 3);

                tessellations[fi] = tess;
                fi++;
            }

            var buffers = new GraphicsBuffers();

            var iCursor = 0;
            var imax = int.MinValue;

            for (var i = 0; i < tessellations.Length; i++)
            {
                var tess = tessellations[i];

                var a = tess.Vertices[tess.Elements[0]].Position.ToVector3();
                var b = tess.Vertices[tess.Elements[1]].Position.ToVector3();
                var c = tess.Vertices[tess.Elements[2]].Position.ToVector3();
                var n = (b - a).Cross(c - a).Unitized();

                for (var j = 0; j < tess.Vertices.Length; j++)
                {
                    var v = tess.Vertices[j];
                    buffers.AddVertex(v.Position.ToVector3(), n, new UV());
                }

                for (var k = 0; k < tess.Elements.Length; k++)
                {
                    var t = tess.Elements[k];
                    var index = (ushort)(t + iCursor);
                    buffers.AddIndex(index);
                    imax = Math.Max(imax, index);
                }

                iCursor = imax + 1;
            }

            return buffers;
        }

        /// <summary>
        /// Create a face from edges.
        /// The first edge array is treated as the outer edge.
        /// Additional edge arrays are treated as holes.
        /// </summary>
        /// <param name="edges"></param>
        /// <param name="reverse"></param>
        protected void Cap(Edge[][] edges, bool reverse = true)
        {
            var loop = new Loop();
            for (var i = 0; i < edges[0].Length; i++)
            {
                if (reverse)
                {
                    loop.AddEdgeToStart(edges[0][i].Right);
                }
                else
                {
                    loop.AddEdgeToEnd(edges[0][i].Left);
                }
            }

            var inner = new Loop[edges.Length - 1];
            for (var i = 1; i < edges.Length; i++)
            {
                inner[i - 1] = new Loop();
                for (var j = 0; j < edges[i].Length; j++)
                {
                    if (reverse)
                    {
                        inner[i - 1].AddEdgeToStart(edges[i][j].Right);
                    }
                    else
                    {
                        inner[i - 1].AddEdgeToEnd(edges[i][j].Left);
                    }
                }
            }
            AddFace(loop, inner);
        }

        protected Loop LoopFromPolygon(Polygon p, bool mergeVerticesAndEdges = false)
        {
            var loop = new Loop();
            var verts = new Vertex[p.Vertices.Count];
            for (var i = 0; i < p.Vertices.Count; i++)
            {
                if (mergeVerticesAndEdges)
                {
                    var existingVertex = Vertices.Select(v => v.Value).FirstOrDefault(v => v.Point.IsAlmostEqualTo(p.Vertices[i]));
                    verts[i] = existingVertex ?? AddVertex(p.Vertices[i]);
                }
                else
                {
                    verts[i] = AddVertex(p.Vertices[i]);
                }
            }
            for (var i = 0; i < p.Vertices.Count; i++)
            {
                var v1 = verts[i];
                var v2 = i == verts.Length - 1 ? verts[0] : verts[i + 1];
                var edge = AddEdge(v1, v2, mergeVerticesAndEdges, out var edgeType);
                loop.AddEdgeToEnd(edgeType == "left" ? edge.Left : edge.Right);
            }
            return loop;
        }

        internal Face AddFace(long id, Loop outer, Loop[] inner = null)
        {
            var f = new Face(id, outer, inner);
            this.Faces.Add(id, f);
            return f;
        }

        internal Vertex AddVertex(long id, Vector3 position)
        {
            var v = new Vertex(id, position);
            this.Vertices.Add(id, v);
            return v;
        }

        internal Edge AddEdge(long id)
        {
            var e = new Edge(id);
            this.Edges.Add(id, e);
            return e;
        }

        internal Edge[] SweepEdges(Transform[] transforms, Edge[] openEdge)
        {
            for (var i = 0; i < transforms.Length - 1; i++)
            {
                var v = (transforms[i + 1].Origin - transforms[i].Origin).Unitized();
                openEdge = SweepEdgesBetweenPlanes(openEdge, v, transforms[i + 1].XY());
            }
            return openEdge;
        }

        internal Loop SweepLoop(Loop loop, Vector3 direction, double distance)
        {
            var sweepEdges = new Edge[loop.Edges.Count];
            var i = 0;
            foreach (var e in loop.Edges)
            {
                var v1 = e.Vertex;
                var v2 = AddVertex(v1.Point + direction * distance);
                sweepEdges[i] = AddEdge(v1, v2);
                i++;
            }

            var openLoop = new Loop();
            var j = 0;
            foreach (var e in loop.Edges)
            {
                var a = e.Edge;
                var b = sweepEdges[j];
                var d = sweepEdges[j == loop.Edges.Count - 1 ? 0 : j + 1];
                var c = AddEdge(b.Right.Vertex, d.Right.Vertex);
                var faceLoop = new Loop(new[] { a.Right, b.Left, c.Left, d.Right });
                AddFace(faceLoop);
                openLoop.AddEdgeToStart(c.Right);
                j++;
            }
            return openLoop;
        }

        private Edge[] ProjectEdgeAlong(Edge[] loop, Vector3 v, Plane p)
        {
            var edges = new Edge[loop.Length];
            for (var i = 0; i < edges.Length; i++)
            {
                var e = loop[i];
                var a = AddVertex(e.Left.Vertex.Point.ProjectAlong(v, p));
                var b = AddVertex(e.Right.Vertex.Point.ProjectAlong(v, p));
                edges[i] = AddEdge(a, b);
            }
            return edges;
        }

        private Edge[] SweepEdgesBetweenPlanes(Edge[] loop1, Vector3 v, Plane end)
        {
            // Project the starting loops to the end plane along v.
            var loop2 = ProjectEdgeAlong(loop1, v, end);

            var sweepEdges = new Edge[loop1.Length];
            for (var i = 0; i < loop1.Length; i++)
            {
                var v1 = loop1[i].Left.Vertex;
                var v2 = loop2[i].Left.Vertex;
                sweepEdges[i] = AddEdge(v1, v2);
            }

            var openEdge = new Edge[sweepEdges.Length];
            for (var i = 0; i < sweepEdges.Length; i++)
            {
                var a = loop1[i];
                var b = sweepEdges[i];
                var c = loop2[i];
                var d = sweepEdges[i == loop1.Length - 1 ? 0 : i + 1];

                var loop = new Loop(new[] { a.Right, b.Left, c.Left, d.Right });
                AddFace(loop);
                openEdge[i] = c;
            }
            return openEdge;
        }

        private Loop SweepPolygonBetweenPlanes(Polygon p, Transform start, Transform end, double rotation = 0.0)
        {
            // Transform the polygon to the mid plane between two transforms
            // then project onto the end transforms. We do this so that we
            // do not introduce shear into the transform.
            var v = (start.Origin - end.Origin).Unitized();
            var midTrans = new Transform(end.Origin.Average(start.Origin), start.YAxis.Cross(v), v);
            var mid = (Polygon)p.Transformed(midTrans);
            var startP = mid.ProjectAlong(v, start.XY());
            var endP = mid.ProjectAlong(v, end.XY());

            var loop1 = AddEdges(startP);
            var loop2 = AddEdges(endP);

            var sweepEdges = new Edge[loop1.Length];
            for (var i = 0; i < loop1.Length; i++)
            {
                var v1 = loop1[i].Left.Vertex;
                var v2 = loop2[i].Left.Vertex;
                sweepEdges[i] = AddEdge(v1, v2);
            }

            var openEdge = new Loop();
            for (var i = 0; i < sweepEdges.Length; i++)
            {
                var a = loop1[i];
                var b = sweepEdges[i];
                var c = loop2[i];
                var d = sweepEdges[i == loop1.Length - 1 ? 0 : i + 1];

                var loop = new Loop(new[] { a.Right, b.Left, c.Left, d.Right });
                AddFace(loop);
                openEdge.AddEdgeToEnd(c.Right);
            }
            return openEdge;
        }

        private void SplitEdge(Plane p, Edge e)
        {
            var start = e.Left.Vertex;
            var end = e.Right.Vertex;
            if (!new Line(start.Point, end.Point).Intersects(p, out Vector3 result))
            {
                return;
            }

            // Add vertex at intersection.
            // Create new edge from vertex to end.
            var mid = AddVertex(result);
            var e1 = AddEdge(mid, end);

            // Adjust end of existing edge to
            // new vertex
            e.Right.Vertex = mid;
            if (e.Left.Loop != null)
            {
                e.Left.Loop.InsertEdgeAfter(e.Left, e1.Left);
            }
            if (e.Right.Loop != null)
            {
                e.Right.Loop.InsertEdgeBefore(e.Right, e1.Right);
            }
        }
    }
}