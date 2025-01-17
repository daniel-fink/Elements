using System.Collections.Generic;
using Elements.Geometry;
using Elements.Geometry.Profiles;
using Elements.Geometry.Solids;
using System;
using Xunit;

namespace Elements.Tests
{
    public class CsgTests : ModelTest
    {
        private HSSPipeProfileFactory _profileFactory = new HSSPipeProfileFactory();

        [Fact]
        public void Csg()
        {
            this.Name = "Elements_Geometry_Csg";

            var s1 = new Extrude(Polygon.Rectangle(Vector3.Origin, new Vector3(30, 30)), 50, Vector3.ZAxis, false);
            var s2 = new Extrude(Polygon.Rectangle(30, 30), 30, Vector3.ZAxis, true);
            var s3 = new Sweep(Polygon.Rectangle(Vector3.Origin, new Vector3(5, 5)), new Line(new Vector3(0, 0, 45), new Vector3(30, 0, 45)), 0, 0, 0, false);
            var poly = new Polygon(new List<Vector3>(){
                new Vector3(0,0,0), new Vector3(20,50,0), new Vector3(0,50,0)
            });
            var s4 = new Sweep(poly, new Line(new Vector3(0, 30, 0), new Vector3(30, 30, 0)), 0, 0, 0, true);

            var geom = new GeometricElement(new Transform(), new Material("Mod", Colors.White, 0.0, 0.0, "./Textures/UV.jpg"), new Representation(new List<SolidOperation>(){
                s1, s2, s3, s4
            }), false, Guid.NewGuid(), null);
            this.Model.AddElement(geom);
        }

        [Fact]
        public void Union()
        {
            this.Name = "CSG_Union";
            var s1 = new Extrude(Polygon.Rectangle(1, 1), 1, Vector3.ZAxis, false);
            var csg = s1.Solid.ToCsg();

            var s2 = new Extrude(Polygon.L(1.0, 2.0, 0.5), 1, Vector3.ZAxis, false);
            csg = csg.Union(s2.Solid.ToCsg());

            var result = new Mesh();
            csg.Tessellate(ref result);

            var me = new MeshElement(result);
            this.Model.AddElement(me);
        }


        [Fact]
        public void Difference()
        {
            this.Name = "CSG_Difference";
            var profile = _profileFactory.GetProfileByType(HSSPipeProfileType.HSS10_000x0_188);

            var path = new Arc(Vector3.Origin, 5, 0, 270);
            var beam = new Beam(path, profile);

            var s2 = new Extrude(new Circle(Vector3.Origin, 6).ToPolygon(20), 1, Vector3.ZAxis, true);
            beam.Representation.SolidOperations.Add(s2);

            for (var i = 0.0; i < 1.0; i += 0.05)
            {
                var pt = path.PointAt(i);
                var hole = new Extrude(new Circle(Vector3.Origin, 0.05).ToPolygon(), 3, Vector3.ZAxis, true)
                {
                    LocalTransform = new Transform(pt + new Vector3(0, 0, -2))
                };
                beam.Representation.SolidOperations.Add(hole);
            }

            this.Model.AddElement(beam);
        }
    }
}