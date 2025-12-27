using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Engineer.Tests
{
    [TestClass]
    public class GPUContextTests
    {
        [TestMethod]
        public void Constructor_CreatesContextWithThread()
        {
            var thread = new GPUThread(5, 10);
            var context = new GPUContext(thread);

            Assert.IsNotNull(context);
            Assert.AreEqual(thread, context.Thread);
        }

        [TestMethod]
        public void Thread_ReturnsCorrectCoordinates()
        {
            var thread = new GPUThread(3, 7);
            var context = new GPUContext(thread);

            Assert.AreEqual(3, context.Thread.X);
            Assert.AreEqual(7, context.Thread.Y);
        }

        [TestMethod]
        public void Record_EqualityWorks()
        {
            var thread1 = new GPUThread(1, 2);
            var thread2 = new GPUThread(1, 2);
            var context1 = new GPUContext(thread1);
            var context2 = new GPUContext(thread2);

            Assert.AreEqual(context1, context2);
        }

        [TestMethod]
        public void Record_InequalityWorks()
        {
            var thread1 = new GPUThread(1, 2);
            var thread2 = new GPUThread(3, 4);
            var context1 = new GPUContext(thread1);
            var context2 = new GPUContext(thread2);

            Assert.AreNotEqual(context1, context2);
        }
    }
}
