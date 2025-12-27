using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Engineer
{
    public class SilkGPU : IGPU
    {
        public Func<T[,], T[,], Task<T[,]>> CreateKernel2DExpr<T>(Expression<Func<GPUContext, T[,], T[,], T>> expression, KernelOptions options) where T : unmanaged
        {
            var gpu = new SilkGPUEngine<T>();
            return async (T[,] a, T[,] b) =>
            {
                return await gpu.RunAsync(expression, a, b, options);
            };
        }
    }
}
