using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Engineer.Tests
{
    [TestClass]
    public class GPUThreadTests
    {
        [TestMethod]
        public void Constructor_SetsXAndY()
        {
            var thread = new GPUThread(10, 20);

            Assert.AreEqual(10, thread.X);
            Assert.AreEqual(20, thread.Y);
        }

        [TestMethod]
        public void Constructor_AcceptsZeroValues()
        {
            var thread = new GPUThread(0, 0);

            Assert.AreEqual(0, thread.X);
            Assert.AreEqual(0, thread.Y);
        }

        [TestMethod]
        public void Constructor_AcceptsNegativeValues()
        {
            var thread = new GPUThread(-5, -10);

            Assert.AreEqual(-5, thread.X);
            Assert.AreEqual(-10, thread.Y);
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var thread1 = new GPUThread(5, 10);
            var thread2 = new GPUThread(5, 10);

            Assert.AreEqual(thread1, thread2);
        }

        [TestMethod]
        public void Record_InequalityWorks()
        {
            var thread1 = new GPUThread(5, 10);
            var thread2 = new GPUThread(10, 5);

            Assert.AreNotEqual(thread1, thread2);
        }

        [TestMethod]
        public void ToString_ReturnsCorrectFormat()
        {
            var thread = new GPUThread(15, 25);
            var result = thread.ToString();

            Assert.IsTrue(result.Contains("15"));
            Assert.IsTrue(result.Contains("25"));
        }
    }
}
