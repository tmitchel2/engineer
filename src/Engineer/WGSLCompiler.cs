using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Engineer
{
    public class WGSLCompiler
    {
        private readonly Dictionary<Type, string> _typeMap = new()
        {
            { typeof(int), "i32" },
            { typeof(uint), "u32" },
            { typeof(float), "f32" },
            { typeof(double), "f64" },
            { typeof(bool), "bool" },
        };

        private readonly List<string> _localVariables = new();

        public string CompileExpression<T>(Expression<Func<GPUContext, T[,], T[,], T>> expression, KernelOptions options) where T : unmanaged
        {
            _localVariables.Clear();

            var typeStr = GetWGSLType(typeof(T));
            if (typeStr == null)
            {
                throw new NotSupportedException($"Type {typeof(T).Name} is not supported for WGSL compilation");
            }

            var bodyCode = CompileExpressionBody(expression.Body, expression.Parameters);

            return GenerateShader(typeStr, bodyCode, options);
        }

        private string CompileExpressionBody(Expression body, IReadOnlyList<ParameterExpression> parameters)
        {
            return Visit(body, parameters);
        }

        private string Visit(Expression node, IReadOnlyList<ParameterExpression> parameters)
        {
            return node switch
            {
                BinaryExpression binary => VisitBinary(binary, parameters),
                UnaryExpression unary => VisitUnary(unary, parameters),
                ParameterExpression param => VisitParameter(param, parameters),
                MemberExpression member => VisitMember(member, parameters),
                MethodCallExpression method => VisitMethodCall(method, parameters),
                ConstantExpression constant => VisitConstant(constant),
                ConditionalExpression conditional => VisitConditional(conditional, parameters),
                IndexExpression index => VisitIndex(index, parameters),
                _ => throw new NotSupportedException($"Expression type {node.NodeType} is not supported")
            };
        }

        private string VisitBinary(BinaryExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            var left = Visit(node.Left, parameters);
            var right = Visit(node.Right, parameters);

            var op = node.NodeType switch
            {
                ExpressionType.Add => "+",
                ExpressionType.Subtract => "-",
                ExpressionType.Multiply => "*",
                ExpressionType.Divide => "/",
                ExpressionType.Modulo => "%",
                ExpressionType.And or ExpressionType.AndAlso => "&&",
                ExpressionType.Or or ExpressionType.OrElse => "||",
                ExpressionType.Equal => "==",
                ExpressionType.NotEqual => "!=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LeftShift => "<<",
                ExpressionType.RightShift => ">>",
                ExpressionType.ExclusiveOr => "^",
                _ => throw new NotSupportedException($"Binary operator {node.NodeType} is not supported")
            };

            return $"({left} {op} {right})";
        }

        private string VisitUnary(UnaryExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            var operand = Visit(node.Operand, parameters);

            return node.NodeType switch
            {
                ExpressionType.Negate => $"(-{operand})",
                ExpressionType.Not => $"(!{operand})",
                ExpressionType.Convert => operand, // Type conversions handled automatically in WGSL
                _ => throw new NotSupportedException($"Unary operator {node.NodeType} is not supported")
            };
        }

        private static string VisitParameter(ParameterExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            // Check if this is the GPUContext parameter
            if (node == parameters[0])
            {
                return "ctx"; // Will be replaced with actual context access
            }

            // This shouldn't happen in our use case
            return node.Name ?? "param";
        }

        private string VisitMember(MemberExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            var obj = Visit(node.Expression!, parameters);

            // Handle GPUContext.Thread.X and GPUContext.Thread.Y
            if (node.Member.Name == "Thread" && obj == "ctx")
            {
                return "ctx_thread";
            }

            if (node.Member.Name == "X" && obj == "ctx_thread")
            {
                return "global_id.x";
            }

            if (node.Member.Name == "Y" && obj == "ctx_thread")
            {
                return "global_id.y";
            }

            return $"{obj}.{ToCamelCase(node.Member.Name)}";
        }

        private string VisitMethodCall(MethodCallExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            // Handle Math methods
            if (node.Method.DeclaringType == typeof(Math))
            {
                return VisitMathMethod(node, parameters);
            }

            // Handle array access methods
            if (node.Method.Name == "Get")
            {
                return VisitArrayAccess(node, parameters);
            }

            throw new NotSupportedException($"Method {node.Method.Name} is not supported");
        }

        private string VisitMathMethod(MethodCallExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            var methodName = node.Method.Name.ToLowerInvariant();
            var args = node.Arguments.Select(arg => Visit(arg, parameters)).ToArray();

            return methodName switch
            {
                "abs" => $"abs({args[0]})",
                "min" => $"min({args[0]}, {args[1]})",
                "max" => $"max({args[0]}, {args[1]})",
                "pow" => $"pow({args[0]}, {args[1]})",
                "sqrt" => $"sqrt({args[0]})",
                "sin" => $"sin({args[0]})",
                "cos" => $"cos({args[0]})",
                "tan" => $"tan({args[0]})",
                "floor" => $"floor({args[0]})",
                "ceil" => $"ceil({args[0]})",
                "round" => $"round({args[0]})",
                "clamp" => $"clamp({args[0]}, {args[1]}, {args[2]})",
                "exp" => $"exp({args[0]})",
                "log" => $"log({args[0]})",
                _ => throw new NotSupportedException($"Math method {node.Method.Name} is not supported")
            };
        }

        private string VisitArrayAccess(MethodCallExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            // This is array indexing: array[x, y]
            var array = Visit(node.Object!, parameters);
            var indices = node.Arguments.Select(arg => Visit(arg, parameters)).ToArray();

            // Determine which array this is (input_a or input_b)
            var arrayName = GetArrayName(node.Object!, parameters);

            // Convert 2D index to 1D: index = y * width + x
            return $"{arrayName}[{indices[1]} * WIDTH + {indices[0]}]";
        }

        private string VisitIndex(IndexExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            var obj = Visit(node.Object!, parameters);
            var indices = node.Arguments.Select(arg => Visit(arg, parameters)).ToArray();

            var arrayName = GetArrayName(node.Object!, parameters);

            // Convert 2D index to 1D: index = y * width + x
            return $"{arrayName}[{indices[1]} * WIDTH + {indices[0]}]";
        }

        private static string GetArrayName(Expression expr, IReadOnlyList<ParameterExpression> parameters)
        {
            if (expr is ParameterExpression param)
            {
                // Check if it's the second parameter (input_a) or third (input_b)
                if (param == parameters[1])
                {
                    return "input_a";
                }
                else if (param == parameters[2])
                {
                    return "input_b";
                }
            }

            return "unknown";
        }

        private static string VisitConstant(ConstantExpression node)
        {
            if (node.Value == null)
            {
                return "0";
            }

            return node.Type.Name switch
            {
                "Int32" => $"{node.Value}",
                "UInt32" => $"{node.Value}u",
                "Single" => $"{node.Value:F6}",
                "Double" => $"{node.Value:F6}",
                "Boolean" => node.Value.ToString()!.ToLowerInvariant(),
                _ => node.Value.ToString() ?? "0"
            };
        }

        private string VisitConditional(ConditionalExpression node, IReadOnlyList<ParameterExpression> parameters)
        {
            var test = Visit(node.Test, parameters);
            var ifTrue = Visit(node.IfTrue, parameters);
            var ifFalse = Visit(node.IfFalse, parameters);

            return $"select({ifFalse}, {ifTrue}, {test})";
        }

        private string? GetWGSLType(Type type)
        {
            return _typeMap.GetValueOrDefault(type);
        }

        private static string ToCamelCase(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;

            return char.ToLowerInvariant(str[0]) + str[1..];
        }

        private string GenerateShader(string typeStr, string bodyExpression, KernelOptions options)
        {
            var shader = new StringBuilder();

            shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"const WIDTH: u32 = {options.XCount}u;");
            shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"const HEIGHT: u32 = {options.YCount}u;");
            shader.AppendLine();

            shader.AppendLine("@group(0) @binding(0)");
            shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var<storage, read> input_a: array<{typeStr}>;");
            shader.AppendLine();

            shader.AppendLine("@group(0) @binding(1)");
            shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var<storage, read> input_b: array<{typeStr}>;");
            shader.AppendLine();

            shader.AppendLine("@group(0) @binding(2)");
            shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"var<storage, read_write> output: array<{typeStr}>;");
            shader.AppendLine();

            shader.AppendLine("@compute");
            shader.AppendLine("@workgroup_size(8, 8)");
            shader.AppendLine("fn main(@builtin(global_invocation_id) global_id: vec3<u32>) {");
            shader.AppendLine("    let index = global_id.y * WIDTH + global_id.x;");
            shader.AppendLine("    let size = WIDTH * HEIGHT;");
            shader.AppendLine();
            shader.AppendLine("    if (index >= size) {");
            shader.AppendLine("        return;");
            shader.AppendLine("    }");
            shader.AppendLine();

            // Add local variables if any
            foreach (var localVar in _localVariables)
            {
                shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    {localVar}");
            }

            shader.AppendLine(System.Globalization.CultureInfo.InvariantCulture, $"    output[index] = {bodyExpression};");
            shader.AppendLine("}");

            return shader.ToString();
        }
    }
}
