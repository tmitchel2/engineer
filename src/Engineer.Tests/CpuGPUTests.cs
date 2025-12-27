using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace Engineer.Tests
{
    [TestClass]
    public class CpuGPUTests
    {
        [TestMethod]
        public async Task CreateKernel2D_ExecutesSimpleAddition()
        {
            var gpu = new CpuGPU();
            var a = new int[2, 2] { { 1, 2 }, { 3, 4 } };
            var b = new int[2, 2] { { 5, 6 }, { 7, 8 } };
            var options = new KernelOptions(2, 2);

            var kernel = gpu.CreateKernel2D<int>((ctx, arrA, arrB) =>
            {
                return arrA[ctx.Thread.X, ctx.Thread.Y] + arrB[ctx.Thread.X, ctx.Thread.Y];
            }, options);

            var result = await kernel(a, b);

            // result[x,y] = a[x,y] + b[x,y]
            Assert.AreEqual(6, result[0, 0]);   // a[0,0]=1 + b[0,0]=5
            Assert.AreEqual(8, result[0, 1]);   // a[0,1]=2 + b[0,1]=6  
            Assert.AreEqual(10, result[1, 0]);  // a[1,0]=3 + b[1,0]=7
            Assert.AreEqual(12, result[1, 1]);  // a[1,1]=4 + b[1,1]=8
        }

        [TestMethod]
        public async Task CreateKernel2D_ExecutesMultiplication()
        {
            var gpu = new CpuGPU();
            var a = new int[2, 2] { { 2, 3 }, { 4, 5 } };
            var b = new int[2, 2] { { 10, 20 }, { 30, 40 } };
            var options = new KernelOptions(2, 2);

            var kernel = gpu.CreateKernel2D<int>((ctx, arrA, arrB) =>
            {
                return arrA[ctx.Thread.X, ctx.Thread.Y] * arrB[ctx.Thread.X, ctx.Thread.Y];
            }, options);

            var result = await kernel(a, b);

            Assert.AreEqual(20, result[0, 0]);    // a[0,0]=2 * b[0,0]=10
            Assert.AreEqual(60, result[0, 1]);    // a[0,1]=3 * b[0,1]=20
            Assert.AreEqual(120, result[1, 0]);   // a[1,0]=4 * b[1,0]=30
            Assert.AreEqual(200, result[1, 1]);   // a[1,1]=5 * b[1,1]=40
        }

        [TestMethod]
        public async Task CreateKernel2D_HandlesFloats()
        {
            var gpu = new CpuGPU();
            var a = new float[1, 2] { { 1.5f, 2.5f } };
            var b = new float[1, 2] { { 0.5f, 0.5f } };
            var options = new KernelOptions(1, 2);

            var kernel = gpu.CreateKernel2D<float>((ctx, arrA, arrB) =>
            {
                return arrA[ctx.Thread.X, ctx.Thread.Y] * arrB[ctx.Thread.X, ctx.Thread.Y];
            }, options);

            var result = await kernel(a, b);

            Assert.AreEqual(0.75f, result[0, 0], 0.001f);
            Assert.AreEqual(1.25f, result[0, 1], 0.001f);
        }

        [TestMethod]
        public async Task CreateKernel2D_UsesThreadCoordinates()
        {
            var gpu = new CpuGPU();
            var a = new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
            var b = new int[,] { { 0, 0, 0 }, { 0, 0, 0 }, { 0, 0, 0 } };
            var options = new KernelOptions(3, 3);

            var kernel = gpu.CreateKernel2D<int>((ctx, arrA, arrB) =>
            {
                return ctx.Thread.X * 10 + ctx.Thread.Y;
            }, options);

            var result = await kernel(a, b);

            Assert.AreEqual(0, result[0, 0]);
            Assert.AreEqual(10, result[1, 0]);
            Assert.AreEqual(20, result[2, 0]);
            Assert.AreEqual(1, result[0, 1]);
            Assert.AreEqual(11, result[1, 1]);
            Assert.AreEqual(21, result[2, 1]);
        }

        [TestMethod]
        public async Task CreateKernel2DExpr_WorksWithExpression()
        {
            var gpu = new CpuGPU();
            var a = new int[1, 2] { { 10, 20 } };
            var b = new int[1, 2] { { 5, 10 } };
            var options = new KernelOptions(1, 2);

            var kernel = gpu.CreateKernel2DExpr<int>((ctx, arrA, arrB) =>
                arrA[ctx.Thread.X, ctx.Thread.Y] - arrB[ctx.Thread.X, ctx.Thread.Y], options);

            var result = await kernel(a, b);

            Assert.AreEqual(5, result[0, 0]);
            Assert.AreEqual(10, result[0, 1]);
        }

        [TestMethod]
        public async Task CreateKernel2D_HandlesSingleCell()
        {
            var gpu = new CpuGPU();
            var a = new int[,] { { 42 } };
            var b = new int[,] { { 8 } };
            var options = new KernelOptions(1, 1);

            var kernel = gpu.CreateKernel2D<int>((ctx, arrA, arrB) =>
            {
                return arrA[0, 0] + arrB[0, 0];
            }, options);

            var result = await kernel(a, b);

            Assert.AreEqual(50, result[0, 0]);
        }

        [TestMethod]
        public async Task CreateKernel2D_HandlesLargeArrays()
        {
            var gpu = new CpuGPU();
            var size = 100;
            var a = new int[size, size];
            var b = new int[size, size];
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    a[x, y] = x + y;
                    b[x, y] = x * y;
                }
            }

            var options = new KernelOptions(size, size);

            var kernel = gpu.CreateKernel2D<int>((ctx, arrA, arrB) =>
            {
                return arrA[ctx.Thread.X, ctx.Thread.Y] + arrB[ctx.Thread.X, ctx.Thread.Y];
            }, options);

            var result = await kernel(a, b);

            Assert.AreEqual(0, result[0, 0]);      // (0+0) + (0*0) = 0
            Assert.AreEqual(3, result[1, 1]);      // (1+1) + (1*1) = 3
            Assert.AreEqual(9999, result[99, 99]); // (99+99) + (99*99) = 198 + 9801 = 9999
        }
    }
}
