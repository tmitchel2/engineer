using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engineer.Graphics;
using System.Numerics;

namespace Engineer.Tests.Graphics
{
    [TestClass]
    public class NodeTests
    {
        [TestMethod]
        public void Constructor_WithId_CreatesNode()
        {
            var id = "test-node-1";
            var node = new Node(id);

            Assert.AreEqual(id, node.Id);
            Assert.AreEqual(Vector3.Zero, node.Translation);
            Assert.AreEqual(default(Quaternion), node.Rotation);
            Assert.AreEqual(Vector3.Zero, node.Scale);
        }

        [TestMethod]
        public void Constructor_WithAllParameters_CreatesNode()
        {
            var id = "test-node-2";
            var translation = new Vector3(1, 2, 3);
            var rotation = Quaternion.CreateFromYawPitchRoll(0.5f, 0.3f, 0.2f);
            var scale = new Vector3(2, 2, 2);

            var node = new Node(id, translation, rotation, scale);

            Assert.AreEqual(id, node.Id);
            Assert.AreEqual(translation, node.Translation);
            Assert.AreEqual(rotation, node.Rotation);
            Assert.AreEqual(scale, node.Scale);
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var node1 = new Node("id1", new Vector3(1, 2, 3));
            var node2 = new Node("id1", new Vector3(1, 2, 3));

            Assert.AreEqual(node1, node2);
        }

        [TestMethod]
        public void Record_InequalityWorks()
        {
            var node1 = new Node("id1", new Vector3(1, 2, 3));
            var node2 = new Node("id2", new Vector3(1, 2, 3));

            Assert.AreNotEqual(node1, node2);
        }

        [TestMethod]
        public void Id_IsAccessible()
        {
            var id = "unique-id";
            var node = new Node(id);

            Assert.AreEqual(id, node.Id);
        }
    }
}
