using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engineer.Graphics;

namespace Engineer.Tests.Graphics
{
    [TestClass]
    public class PlaneTests
    {
        [TestMethod]
        public void Constructor_SetsAllProperties()
        {
            var plane = new Plane(0, 1, 2, -1);

            Assert.AreEqual(0, plane.U);
            Assert.AreEqual(1, plane.V);
            Assert.AreEqual(2, plane.W);
            Assert.AreEqual(-1, plane.UDir);
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var plane1 = new Plane(0, 1, 2, 1);
            var plane2 = new Plane(0, 1, 2, 1);

            Assert.AreEqual(plane1, plane2);
        }

        [TestMethod]
        public void Record_InequalityWorks()
        {
            var plane1 = new Plane(0, 1, 2, 1);
            var plane2 = new Plane(1, 0, 2, 1);

            Assert.AreNotEqual(plane1, plane2);
        }

        [TestMethod]
        public void Properties_AreAccessible()
        {
            var plane = new Plane(2, 1, 0, -1);

            Assert.AreEqual(2, plane.U);
            Assert.AreEqual(1, plane.V);
            Assert.AreEqual(0, plane.W);
            Assert.AreEqual(-1, plane.UDir);
        }
    }
}
