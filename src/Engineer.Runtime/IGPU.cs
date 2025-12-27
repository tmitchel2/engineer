using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Engineer
{
    public interface IGPU
    {
        Func<T[,], T[,], Task<T[,]>> CreateKernel2D<T>(Func<GPUContext, T[,], T[,], T> func, KernelOptions options);

        Func<T[,], T[,], Task<T[,]>> CreateKernel2DExpr<T>(Expression<Func<GPUContext, T[,], T[,], T>> expression, KernelOptions options);
    }
}
