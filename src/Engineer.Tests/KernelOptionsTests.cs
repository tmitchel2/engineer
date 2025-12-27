using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Engineer.Tests
{
    [TestClass]
    public class KernelOptionsTests
    {
        [TestMethod]
        public void Constructor_SetsXCountAndYCount()
        {
            var options = new KernelOptions(100, 200);

            Assert.AreEqual(100, options.XCount);
            Assert.AreEqual(200, options.YCount);
        }

        [TestMethod]
        public void Constructor_AcceptsSmallValues()
        {
            var options = new KernelOptions(1, 1);

            Assert.AreEqual(1, options.XCount);
            Assert.AreEqual(1, options.YCount);
        }

        [TestMethod]
        public void Constructor_AcceptsLargeValues()
        {
            var options = new KernelOptions(10000, 10000);

            Assert.AreEqual(10000, options.XCount);
            Assert.AreEqual(10000, options.YCount);
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var options1 = new KernelOptions(50, 75);
            var options2 = new KernelOptions(50, 75);

            Assert.AreEqual(options1, options2);
        }

        [TestMethod]
        public void Record_InequalityWorks()
        {
            var options1 = new KernelOptions(50, 75);
            var options2 = new KernelOptions(75, 50);

            Assert.AreNotEqual(options1, options2);
        }

        [TestMethod]
        public void ToString_ReturnsCorrectFormat()
        {
            var options = new KernelOptions(128, 256);
            var result = options.ToString();

            Assert.IsTrue(result.Contains("128"));
            Assert.IsTrue(result.Contains("256"));
        }
    }
}
