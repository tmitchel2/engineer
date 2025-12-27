using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq.Expressions;

namespace Engineer.Tests
{
    [TestClass]
    public class WGSLCompilerTests
    {
        [TestMethod]
        public void CompileExpression_SimpleAddition_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr = 
                (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] + b[ctx.Thread.X, ctx.Thread.Y];
            var options = new KernelOptions(16, 16);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("input_a: array<i32>"), "Should declare input_a");
            Assert.IsTrue(wgsl.Contains("input_b: array<i32>"), "Should declare input_b");
            Assert.IsTrue(wgsl.Contains("output: array<i32>"), "Should declare output");
            Assert.IsTrue(wgsl.Contains("WIDTH: u32 = 16u"), "Should set WIDTH constant");
            Assert.IsTrue(wgsl.Contains("HEIGHT: u32 = 16u"), "Should set HEIGHT constant");
            Assert.IsTrue(wgsl.Contains("+"), "Should contain addition operator");
        }

        [TestMethod]
        public void CompileExpression_Multiplication_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, float[,], float[,], float>> expr =
                (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] * b[ctx.Thread.X, ctx.Thread.Y];
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("array<f32>"), "Should use f32 type for float");
            Assert.IsTrue(wgsl.Contains("*"), "Should contain multiplication operator");
        }

        [TestMethod]
        public void CompileExpression_ComplexExpression_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr =
                (ctx, a, b) => (a[ctx.Thread.X, ctx.Thread.Y] + b[ctx.Thread.X, ctx.Thread.Y]) * 2;
            var options = new KernelOptions(10, 10);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("+"), "Should contain addition");
            Assert.IsTrue(wgsl.Contains("*"), "Should contain multiplication");
            Assert.IsTrue(wgsl.Contains("2"), "Should contain constant 2");
        }

        [TestMethod]
        public void CompileExpression_Subtraction_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr =
                (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] - b[ctx.Thread.X, ctx.Thread.Y];
            var options = new KernelOptions(4, 4);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("-"), "Should contain subtraction operator");
        }

        [TestMethod]
        public void CompileExpression_Division_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, float[,], float[,], float>> expr =
                (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] / b[ctx.Thread.X, ctx.Thread.Y];
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("/"), "Should contain division operator");
        }

        [TestMethod]
        public void CompileExpression_WithMathMax_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, float[,], float[,], float>> expr =
                (ctx, a, b) => Math.Max(a[ctx.Thread.X, ctx.Thread.Y], b[ctx.Thread.X, ctx.Thread.Y]);
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("max"), "Should contain max function");
        }

        [TestMethod]
        public void CompileExpression_WithMathMin_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr =
                (ctx, a, b) => Math.Min(a[ctx.Thread.X, ctx.Thread.Y], b[ctx.Thread.X, ctx.Thread.Y]);
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("min"), "Should contain min function");
        }

        [TestMethod]
        public void CompileExpression_WithMathAbs_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr =
                (ctx, a, b) => Math.Abs(a[ctx.Thread.X, ctx.Thread.Y] - b[ctx.Thread.X, ctx.Thread.Y]);
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("abs"), "Should contain abs function");
        }

        [TestMethod]
        public void CompileExpression_Modulo_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr =
                (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] % b[ctx.Thread.X, ctx.Thread.Y];
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("%"), "Should contain modulo operator");
        }

        [TestMethod]
        public void CompileExpression_Comparison_GeneratesCorrectWGSL()
        {
            var compiler = new WGSLCompiler();
            Expression<Func<GPUContext, int[,], int[,], int>> expr =
                (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] > b[ctx.Thread.X, ctx.Thread.Y] ? 
                    a[ctx.Thread.X, ctx.Thread.Y] : b[ctx.Thread.X, ctx.Thread.Y];
            var options = new KernelOptions(8, 8);

            var wgsl = compiler.CompileExpression(expr, options);

            Assert.IsNotNull(wgsl);
            Assert.IsTrue(wgsl.Contains("select"), "Should use select for ternary operator");
        }
    }
}
