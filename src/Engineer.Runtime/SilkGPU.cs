// using System;
// using System.Linq.Expressions;
// using System.Threading.Tasks;

// namespace Engineer
// {
//     public class SilkGPU : IGPU
//     {
//         public Func<T[,], T[,], Task<T[,]>> CreateKernel2D<T>(Func<GPUContext, T[,], T[,], T> function, KernelOptions options)
//         {
//             var gpu = new SilkGPUEngine<T>();
//             return async (T[,] a, T[,] b) =>
//             {
//                 return await gpu.RunAsync();
//             };
//         }

//         public Func<T[,], T[,], Task<T[,]>> CreateKernel2DExpr<T>(Expression<Func<GPUContext, T[,], T[,], T>> expression, KernelOptions options)
//         {
//             return CreateKernel2D(expression.Compile(), options);
//         }
//     }
// }
