using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Engineer
{
    public class CpuGPU : IGPU
    {
        public Func<T[,], T[,], Task<T[,]>> CreateKernel2DExpr<T>(Expression<Func<GPUContext, T[,], T[,], T>> expression, KernelOptions options) where T : unmanaged
        {
            var func = expression.Compile();
            return (T[,] arg1, T[,] arg2) =>
            {
                var result = new T[options.XCount, options.YCount];
                for (var y = 0; y < options.YCount; y++)
                {
                    for (var x = 0; x < options.XCount; x++)
                    {
                        var ctx = new GPUContext(new GPUThread(x, y));
                        result[x, y] = func(ctx, arg1, arg2);
                    }
                }

                return Task.FromResult(result);
            };
        }
    }
}
