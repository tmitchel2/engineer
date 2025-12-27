using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Engineer.Tests
{
    [TestClass]
    public class SilkGPUTests : GPUTests
    {
        protected override IGPU CreateGPU()
        {
            return new SilkGPU();
        }
    }
}
