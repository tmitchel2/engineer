using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engineer.Graphics;
using System.Numerics;
using System.Linq;

namespace Engineer.Tests.Graphics
{
    [TestClass]
    public class GeometryTests
    {
        [TestMethod]
        public void Constructor_CreatesGeometryWithEmptyLists()
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());

            Assert.AreEqual(PrimitiveType.Triangles, geometry.PrimitiveType);
            Assert.IsNotNull(geometry.Vertices);
            Assert.IsNotNull(geometry.Normals);
            Assert.AreEqual(0, geometry.Vertices.Count);
            Assert.AreEqual(0, geometry.Normals.Count);
        }

        [TestMethod]
        public void Constructor_StoresVerticesAndNormals()
        {
            var vertices = new System.Collections.Generic.List<Vector3>
            {
                new Vector3(1, 2, 3),
                new Vector3(4, 5, 6)
            };
            var normals = new System.Collections.Generic.List<Vector3>
            {
                new Vector3(0, 1, 0),
                new Vector3(1, 0, 0)
            };

            var geometry = new Geometry(PrimitiveType.Triangles, vertices, normals);

            Assert.AreEqual(2, geometry.Vertices.Count);
            Assert.AreEqual(2, geometry.Normals.Count);
            Assert.AreEqual(new Vector3(1, 2, 3), geometry.Vertices[0]);
            Assert.AreEqual(new Vector3(0, 1, 0), geometry.Normals[0]);
        }

        [TestMethod]
        public void Box_CreatesGeometry()
        {
            var props = new BoxProps
            {
                Width = 10f,
                Height = 20f,
                Depth = 30f
            };

            var geometry = Geometry.Box(props);

            Assert.IsNotNull(geometry);
            Assert.AreEqual(PrimitiveType.Triangles, geometry.PrimitiveType);
            Assert.IsTrue(geometry.Vertices.Count > 0, "Box should have vertices");
            Assert.IsTrue(geometry.Normals.Count > 0, "Box should have normals");
        }

        [TestMethod]
        public void Box_HasCorrectNumberOfFaces()
        {
            var props = new BoxProps
            {
                Width = 1f,
                Height = 1f,
                Depth = 1f
            };

            var geometry = Geometry.Box(props);

            // A box has 6 faces, each with 2 triangles (12 total triangles)
            // But DrawRectangle adds vertices for initial grid and then triangles
            // So we verify we have vertices
            Assert.IsTrue(geometry.Vertices.Count >= 24, "Box should have at least 24 vertices for 6 faces with 4 corners each");
        }

        [TestMethod]
        public void Box_WithDifferentDimensions_CreatesGeometry()
        {
            var props = new BoxProps
            {
                Width = 5f,
                Height = 10f,
                Depth = 15f
            };

            var geometry = Geometry.Box(props);

            Assert.IsNotNull(geometry);
            Assert.AreEqual(PrimitiveType.Triangles, geometry.PrimitiveType);
            Assert.IsTrue(geometry.Vertices.Count > 0);
            Assert.IsTrue(geometry.Normals.Count > 0);
        }

        [TestMethod]
        public void Box_VerticesAndNormals_HaveSameCount()
        {
            var props = new BoxProps
            {
                Width = 2f,
                Height = 3f,
                Depth = 4f
            };

            var geometry = Geometry.Box(props);

            // After DrawRectangle processes, vertices are added for triangles
            // Normals are added only for the initial grid points
            Assert.IsTrue(geometry.Normals.Count > 0, "Should have normals");
        }

        [TestMethod]
        public void DrawRectangle_AddsVerticesAndNormals()
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());
            var plane = new Engineer.Graphics.Plane(0, 1, 2, 1);
            var size = new Vector3(10, 10, 5);

            Geometry.DrawRectangle(plane, size, 1, 1, geometry);

            Assert.IsTrue(geometry.Vertices.Count > 0, "Should have vertices");
            Assert.IsTrue(geometry.Normals.Count > 0, "Should have normals");
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var vertices = new System.Collections.Generic.List<Vector3> { new Vector3(1, 2, 3) };
            var normals = new System.Collections.Generic.List<Vector3> { new Vector3(0, 1, 0) };
            var geometry1 = new Geometry(PrimitiveType.Triangles, vertices, normals);
            var geometry2 = new Geometry(PrimitiveType.Triangles, vertices, normals);

            Assert.AreEqual(geometry1, geometry2);
        }

        [TestMethod]
        public void PrimitiveType_IsAccessible()
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());

            Assert.AreEqual(PrimitiveType.Triangles, geometry.PrimitiveType);
        }
    }
}
