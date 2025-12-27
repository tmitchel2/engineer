using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engineer.Graphics;
using System.Numerics;
using System;

namespace Engineer.Tests.Graphics
{
    [TestClass]
    public class MeshNodeTests
    {
        [TestMethod]
        public void Constructor_WithIdAndGeometry_CreatesMeshNode()
        {
            var id = "mesh-1";
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());

            var meshNode = new MeshNode(id, geometry);

            Assert.AreEqual(id, meshNode.Id);
            Assert.AreEqual(geometry, meshNode.Geometry);
            Assert.AreEqual(Vector3.Zero, meshNode.Translation);
            Assert.AreEqual(default(Quaternion), meshNode.Rotation);
            Assert.AreEqual(Vector3.Zero, meshNode.Scale);
        }

        [TestMethod]
        public void Constructor_WithAllParameters_CreatesMeshNode()
        {
            var id = "mesh-2";
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());
            var translation = new Vector3(5, 10, 15);
            var rotation = Quaternion.CreateFromYawPitchRoll(1f, 0.5f, 0.25f);
            var scale = new Vector3(1, 1, 1);

            var meshNode = new MeshNode(id, geometry, translation, rotation, scale);

            Assert.AreEqual(id, meshNode.Id);
            Assert.AreEqual(geometry, meshNode.Geometry);
            Assert.AreEqual(translation, meshNode.Translation);
            Assert.AreEqual(rotation, meshNode.Rotation);
            Assert.AreEqual(scale, meshNode.Scale);
        }

        [TestMethod]
        public void MeshNode_IsNode()
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());
            var meshNode = new MeshNode("test", geometry);

            Assert.IsInstanceOfType(meshNode, typeof(Node));
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3>(),
                new System.Collections.Generic.List<Vector3>());
            var meshNode1 = new MeshNode("id1", geometry, new Vector3(1, 2, 3));
            var meshNode2 = new MeshNode("id1", geometry, new Vector3(1, 2, 3));

            Assert.AreEqual(meshNode1, meshNode2);
        }

        [TestMethod]
        public void Geometry_IsAccessible()
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new System.Collections.Generic.List<Vector3> { new Vector3(1, 2, 3) },
                new System.Collections.Generic.List<Vector3> { new Vector3(0, 1, 0) });
            var meshNode = new MeshNode("test", geometry);

            Assert.AreEqual(1, meshNode.Geometry.Vertices.Count);
            Assert.AreEqual(1, meshNode.Geometry.Normals.Count);
        }
    }
}
