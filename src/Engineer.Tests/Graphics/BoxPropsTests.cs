using Microsoft.VisualStudio.TestTools.UnitTesting;
using Engineer.Graphics;
using System.Numerics;

namespace Engineer.Tests.Graphics
{
    [TestClass]
    public class BoxPropsTests
    {
        [TestMethod]
        public void Constructor_SetsAllProperties()
        {
            var props = new BoxProps
            {
                Width = 10.5f,
                Height = 20.3f,
                Depth = 15.7f
            };

            Assert.AreEqual(10.5f, props.Width);
            Assert.AreEqual(20.3f, props.Height);
            Assert.AreEqual(15.7f, props.Depth);
        }

        [TestMethod]
        public void DefaultValues_AreZero()
        {
            var props = new BoxProps();

            Assert.AreEqual(0f, props.Width);
            Assert.AreEqual(0f, props.Height);
            Assert.AreEqual(0f, props.Depth);
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var props1 = new BoxProps { Width = 5f, Height = 10f, Depth = 15f };
            var props2 = new BoxProps { Width = 5f, Height = 10f, Depth = 15f };

            Assert.AreEqual(props1, props2);
        }

        [TestMethod]
        public void Record_InequalityWorks()
        {
            var props1 = new BoxProps { Width = 5f, Height = 10f, Depth = 15f };
            var props2 = new BoxProps { Width = 6f, Height = 10f, Depth = 15f };

            Assert.AreNotEqual(props1, props2);
        }
    }
}
