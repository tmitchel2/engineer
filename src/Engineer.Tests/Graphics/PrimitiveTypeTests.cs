using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engineer.Graphics;

namespace Engineer.Tests.Graphics
{
    [TestClass]
    public class PrimitiveTypeTests
    {
        [TestMethod]
        public void Triangles_HasValue()
        {
            var primitiveType = PrimitiveType.Triangles;

            Assert.AreEqual(PrimitiveType.Triangles, primitiveType);
        }

        [TestMethod]
        public void Triangles_CanBeCompared()
        {
            var type1 = PrimitiveType.Triangles;
            var type2 = PrimitiveType.Triangles;

            Assert.AreEqual(type1, type2);
        }

        [TestMethod]
        public void Enum_HasDefaultValue()
        {
            PrimitiveType defaultType = default;

            Assert.AreEqual(PrimitiveType.Triangles, defaultType);
        }
    }
}
