#pragma warning disable CA2201 // Do not raise reserved exception types
using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.WebGPU;
using Silk.NET.WebGPU.Extensions.WGPU;

namespace Engineer
{
    public class SilkGPUEngine<T> where T : unmanaged
    {
        public async Task<T[,]> RunAsync(Func<GPUContext, T[,], T[,], T> func, T[,] a, T[,] b, KernelOptions options)
        {
            // This overload is for delegate-based kernels, not expressions
            // Fall back to CPU since we can't analyze delegates
            var result = new T[options.XCount, options.YCount];
            await Task.Run(() =>
            {
                for (var y = 0; y < options.YCount; y++)
                {
                    for (var x = 0; x < options.XCount; x++)
                    {
                        var ctx = new GPUContext(new GPUThread(x, y));
                        result[x, y] = func(ctx, a, b);
                    }
                }
            });
            return result;
        }

        public async Task<T[,]> RunAsync(Expression<Func<GPUContext, T[,], T[,], T>> expression, T[,] a, T[,] b, KernelOptions options)
        {
            // Convert 2D arrays to 1D buffers for GPU processing
            var aFlat = Flatten(a, options);
            var bFlat = Flatten(b, options);
            var resultFlat = new T[options.XCount * options.YCount];

            // Try to execute on GPU, fall back to CPU if needed
            try
            {
                var compiler = new WGSLCompiler();
                var shaderCode = compiler.CompileExpression(expression, options);
                await RunOnGPU(aFlat, bFlat, resultFlat, options, shaderCode);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPU execution failed: {ex.Message}, falling back to CPU");
                var func = expression.Compile();
                await Task.Run(() => RunOnCPU(func, a, b, resultFlat, options));
            }

            // Convert result back to 2D array
            return Unflatten(resultFlat, options);
        }

        private static T[] Flatten(T[,] array, KernelOptions options)
        {
            var flat = new T[options.XCount * options.YCount];
            for (int x = 0; x < options.XCount; x++)
            {
                for (int y = 0; y < options.YCount; y++)
                {
                    flat[y * options.XCount + x] = array[x, y];
                }
            }
            return flat;
        }

        private static T[,] Unflatten(T[] flat, KernelOptions options)
        {
            var array = new T[options.XCount, options.YCount];
            for (int x = 0; x < options.XCount; x++)
            {
                for (int y = 0; y < options.YCount; y++)
                {
                    array[x, y] = flat[y * options.XCount + x];
                }
            }
            return array;
        }

        private static void RunOnCPU(Func<GPUContext, T[,], T[,], T> func, T[,] a, T[,] b, T[] result, KernelOptions options)
        {
            for (var y = 0; y < options.YCount; y++)
            {
                for (var x = 0; x < options.XCount; x++)
                {
                    var ctx = new GPUContext(new GPUThread(x, y));
                    result[y * options.XCount + x] = func(ctx, a, b);
                }
            }
        }

        private static async Task RunOnGPU(T[] a, T[] b, T[] result, KernelOptions options, string shaderCode)
        {
            var state = await CreateGPUStateAsync(a, b, result, options, shaderCode);
            await ExecuteComputeShaderAsync(state, options);
            await ReadResultAsync(state, result);
            CleanUpGPUState(state);
        }

        private static unsafe Task<GPUComputeState> CreateGPUStateAsync(T[] a, T[] b, T[] result, KernelOptions options, string shaderCode)
        {
            var webGPU = WebGPU.GetApi();
            var instanceDescriptor = new InstanceDescriptor();
            var instance = webGPU.CreateInstance(&instanceDescriptor);

            var tcs = new TaskCompletionSource<GPUComputeState>();

            var requestAdapterOptions = new RequestAdapterOptions { };
            webGPU.InstanceRequestAdapter(
                instance,
                in requestAdapterOptions,
                new PfnRequestAdapterCallback((status, adapter, message, userData) =>
                {
                    if (status != RequestAdapterStatus.Success)
                    {
                        tcs.SetException(new Exception($"Unable to create adapter: {SilkMarshal.PtrToString((nint)message)}"));
                        return;
                    }

                    CreateDeviceAndBuffers(webGPU, adapter, instance, a, b, result, shaderCode, options, tcs);
                }),
                null
            );

            return tcs.Task;
        }

        private static unsafe void CreateDeviceAndBuffers(
            WebGPU webGPU,
            Adapter* adapter,
            Instance* instance,
            T[] a,
            T[] b,
            T[] result,
            string shaderCode,
            KernelOptions options,
            TaskCompletionSource<GPUComputeState> tcs)
        {
            var deviceDescriptor = new DeviceDescriptor
            {
                DeviceLostCallback = new PfnDeviceLostCallback(DeviceLost),
            };

            webGPU.AdapterRequestDevice(
                adapter,
                in deviceDescriptor,
                new PfnRequestDeviceCallback((status, device, message, userData) =>
                {
                    if (status != RequestDeviceStatus.Success)
                    {
                        tcs.SetException(new Exception($"Unable to create device: {SilkMarshal.PtrToString((nint)message)}"));
                        return;
                    }

                    try
                    {
                        webGPU.DeviceSetUncapturedErrorCallback(device, new PfnErrorCallback(UncapturedError), null);

                        var elementSize = (ulong)Marshal.SizeOf<T>();
                        var bufferSize = elementSize * (ulong)(a.Length);

                        // Create buffers
                        var bufferA = CreateBuffer(webGPU, device, bufferSize, BufferUsage.Storage | BufferUsage.CopyDst);
                        var bufferB = CreateBuffer(webGPU, device, bufferSize, BufferUsage.Storage | BufferUsage.CopyDst);
                        var bufferOut = CreateBuffer(webGPU, device, bufferSize, BufferUsage.Storage | BufferUsage.CopySrc);
                        var stagingBuffer = CreateBuffer(webGPU, device, bufferSize, BufferUsage.MapRead | BufferUsage.CopyDst);

                        // Upload data
                        UploadData(webGPU, device, bufferA, a);
                        UploadData(webGPU, device, bufferB, b);

                        // Create shader module
                        var shader = CreateShaderModule(webGPU, device, shaderCode);

                        // Create bind group layout
                        var bindGroupLayout = CreateBindGroupLayout(webGPU, device);

                        // Create pipeline
                        var pipelineLayout = CreatePipelineLayout(webGPU, device, bindGroupLayout);
                        var pipeline = CreateComputePipeline(webGPU, device, shader, pipelineLayout);

                        // Create bind group
                        var bindGroup = CreateBindGroup(webGPU, device, bindGroupLayout, bufferA, bufferB, bufferOut);

                        tcs.SetResult(new GPUComputeState(
                            webGPU, instance, adapter, device, shader, pipeline,
                            bufferA, bufferB, bufferOut, stagingBuffer, bindGroup, bindGroupLayout, pipelineLayout));
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }),
                null
            );
        }

        private static unsafe Silk.NET.WebGPU.Buffer* CreateBuffer(WebGPU webGPU, Device* device, ulong size, BufferUsage usage)
        {
            var bufferDescriptor = new BufferDescriptor
            {
                Size = size,
                Usage = usage,
                MappedAtCreation = false
            };
            return webGPU.DeviceCreateBuffer(device, in bufferDescriptor);
        }

        private static unsafe void UploadData(WebGPU webGPU, Device* device, Silk.NET.WebGPU.Buffer* buffer, T[] data)
        {
            var queue = webGPU.DeviceGetQueue(device);
            var elementSize = (nuint)Marshal.SizeOf<T>();
            var size = elementSize * (nuint)data.Length;

            fixed (T* dataPtr = data)
            {
                webGPU.QueueWriteBuffer(queue, buffer, 0, dataPtr, size);
            }
        }

        private static unsafe ShaderModule* CreateShaderModule(WebGPU webGPU, Device* device, string shaderCode)
        {
            var wgslDescriptor = new ShaderModuleWGSLDescriptor
            {
                Code = (byte*)SilkMarshal.StringToPtr(shaderCode),
                Chain = new ChainedStruct
                {
                    SType = SType.ShaderModuleWgslDescriptor
                }
            };

            var shaderModuleDescriptor = new ShaderModuleDescriptor
            {
                NextInChain = (ChainedStruct*)&wgslDescriptor,
            };

            return webGPU.DeviceCreateShaderModule(device, in shaderModuleDescriptor);
        }

        private static unsafe BindGroupLayout* CreateBindGroupLayout(WebGPU webGPU, Device* device)
        {
            var entries = stackalloc BindGroupLayoutEntry[3];
            
            // Input A
            entries[0] = new BindGroupLayoutEntry
            {
                Binding = 0,
                Visibility = ShaderStage.Compute,
                Buffer = new BufferBindingLayout
                {
                    Type = BufferBindingType.ReadOnlyStorage,
                }
            };

            // Input B
            entries[1] = new BindGroupLayoutEntry
            {
                Binding = 1,
                Visibility = ShaderStage.Compute,
                Buffer = new BufferBindingLayout
                {
                    Type = BufferBindingType.ReadOnlyStorage,
                }
            };

            // Output
            entries[2] = new BindGroupLayoutEntry
            {
                Binding = 2,
                Visibility = ShaderStage.Compute,
                Buffer = new BufferBindingLayout
                {
                    Type = BufferBindingType.Storage,
                }
            };

            var bindGroupLayoutDescriptor = new BindGroupLayoutDescriptor
            {
                EntryCount = 3,
                Entries = entries
            };

            return webGPU.DeviceCreateBindGroupLayout(device, in bindGroupLayoutDescriptor);
        }

        private static unsafe PipelineLayout* CreatePipelineLayout(WebGPU webGPU, Device* device, BindGroupLayout* bindGroupLayout)
        {
            var bindGroupLayouts = stackalloc BindGroupLayout*[1];
            bindGroupLayouts[0] = bindGroupLayout;

            var pipelineLayoutDescriptor = new PipelineLayoutDescriptor
            {
                BindGroupLayoutCount = 1,
                BindGroupLayouts = bindGroupLayouts,
            };

            return webGPU.DeviceCreatePipelineLayout(device, in pipelineLayoutDescriptor);
        }

        private static unsafe ComputePipeline* CreateComputePipeline(WebGPU webGPU, Device* device, ShaderModule* shader, PipelineLayout* layout)
        {
            var computePipelineDescriptor = new ComputePipelineDescriptor
            {
                Layout = layout,
                Compute = new ProgrammableStageDescriptor
                {
                    Module = shader,
                    EntryPoint = (byte*)SilkMarshal.StringToPtr("main"),
                }
            };

            return webGPU.DeviceCreateComputePipeline(device, in computePipelineDescriptor);
        }

        private static unsafe BindGroup* CreateBindGroup(
            WebGPU webGPU,
            Device* device,
            BindGroupLayout* layout,
            Silk.NET.WebGPU.Buffer* bufferA,
            Silk.NET.WebGPU.Buffer* bufferB,
            Silk.NET.WebGPU.Buffer* bufferOut)
        {
            var entries = stackalloc BindGroupEntry[3];
            
            entries[0] = new BindGroupEntry
            {
                Binding = 0,
                Buffer = bufferA,
                Size = webGPU.BufferGetSize(bufferA)
            };

            entries[1] = new BindGroupEntry
            {
                Binding = 1,
                Buffer = bufferB,
                Size = webGPU.BufferGetSize(bufferB)
            };

            entries[2] = new BindGroupEntry
            {
                Binding = 2,
                Buffer = bufferOut,
                Size = webGPU.BufferGetSize(bufferOut)
            };

            var bindGroupDescriptor = new BindGroupDescriptor
            {
                Entries = entries,
                EntryCount = 3,
                Layout = layout,
            };

            return webGPU.DeviceCreateBindGroup(device, in bindGroupDescriptor);
        }

        private static unsafe Task ExecuteComputeShaderAsync(GPUComputeState state, KernelOptions options)
        {
            var commandEncoderDescriptor = new CommandEncoderDescriptor();
            var encoder = state.WebGPU.DeviceCreateCommandEncoder(state.Device, in commandEncoderDescriptor);

            var computePassDescriptor = new ComputePassDescriptor { };
            var computePass = state.WebGPU.CommandEncoderBeginComputePass(encoder, in computePassDescriptor);
            
            state.WebGPU.ComputePassEncoderSetPipeline(computePass, state.Pipeline);
            state.WebGPU.ComputePassEncoderSetBindGroup(computePass, 0, state.BindGroup, 0, null);
            
            // Dispatch workgroups - 8x8 workgroup size
            var workgroupsX = (uint)((options.XCount + 7) / 8);
            var workgroupsY = (uint)((options.YCount + 7) / 8);
            state.WebGPU.ComputePassEncoderDispatchWorkgroups(computePass, workgroupsX, workgroupsY, 1);
            state.WebGPU.ComputePassEncoderEnd(computePass);

            // Copy result to staging buffer
            state.WebGPU.CommandEncoderCopyBufferToBuffer(
                encoder,
                state.BufferOut,
                0,
                state.StagingBuffer,
                0,
                state.WebGPU.BufferGetSize(state.BufferOut));

            var commandBufferDescriptor = new CommandBufferDescriptor();
            var commandBuffer = state.WebGPU.CommandEncoderFinish(encoder, in commandBufferDescriptor);

            var queue = state.WebGPU.DeviceGetQueue(state.Device);
            state.WebGPU.QueueSubmit(queue, 1, &commandBuffer);

            state.WebGPU.CommandBufferRelease(commandBuffer);
            state.WebGPU.ComputePassEncoderRelease(computePass);
            state.WebGPU.CommandEncoderRelease(encoder);

            return Task.CompletedTask;
        }

        private static unsafe Task ReadResultAsync(GPUComputeState state, T[] result)
        {
            var tcs = new TaskCompletionSource();
            var bufferSize = state.WebGPU.BufferGetSize(state.StagingBuffer);

            state.WebGPU.BufferMapAsync(
                state.StagingBuffer,
                MapMode.Read,
                0,
                (nuint)bufferSize,
                new PfnBufferMapCallback((status, data) =>
                {
                    if (status != BufferMapAsyncStatus.Success)
                    {
                        tcs.SetException(new Exception($"Unable to map buffer! status: {status}"));
                        return;
                    }

                    try
                    {
                        var mappedData = state.WebGPU.BufferGetMappedRange(state.StagingBuffer, 0, (nuint)bufferSize);
                        fixed (T* resultPtr = result)
                        {
                            System.Buffer.MemoryCopy(mappedData, resultPtr, (nuint)bufferSize, (nuint)bufferSize);
                        }
                        state.WebGPU.BufferUnmap(state.StagingBuffer);
                        tcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }),
                null
            );

            if (state.WebGPU.TryGetDeviceExtension<Wgpu>(state.Device, out var wgpu))
            {
                wgpu.DevicePoll(state.Device, true, null);
            }

            return tcs.Task;
        }

        private static unsafe void CleanUpGPUState(GPUComputeState state)
        {
            state.WebGPU.BufferRelease(state.BufferA);
            state.WebGPU.BufferRelease(state.BufferB);
            state.WebGPU.BufferRelease(state.BufferOut);
            state.WebGPU.BufferRelease(state.StagingBuffer);
            state.WebGPU.BindGroupRelease(state.BindGroup);
            state.WebGPU.BindGroupLayoutRelease(state.BindGroupLayout);
            state.WebGPU.PipelineLayoutRelease(state.PipelineLayout);
            state.WebGPU.ComputePipelineRelease(state.Pipeline);
            state.WebGPU.ShaderModuleRelease(state.Shader);
            state.WebGPU.DeviceRelease(state.Device);
            state.WebGPU.AdapterRelease(state.Adapter);
            state.WebGPU.InstanceRelease(state.Instance);
            state.WebGPU.Dispose();
        }

        private static unsafe void DeviceLost(DeviceLostReason arg0, byte* arg1, void* arg2)
        {
            Debug.WriteLine($"Device lost! Reason: {arg0} Message: {SilkMarshal.PtrToString((nint)arg1)}");
        }

        private static unsafe void UncapturedError(ErrorType arg0, byte* arg1, void* arg2)
        {
            Debug.WriteLine($"{arg0}: {SilkMarshal.PtrToString((nint)arg1)}");
        }

        private sealed class GPUComputeState
        {
            public WebGPU WebGPU { get; }
            public unsafe Instance* Instance { get; }
            public unsafe Adapter* Adapter { get; }
            public unsafe Device* Device { get; }
            public unsafe ShaderModule* Shader { get; }
            public unsafe ComputePipeline* Pipeline { get; }
            public unsafe Silk.NET.WebGPU.Buffer* BufferA { get; }
            public unsafe Silk.NET.WebGPU.Buffer* BufferB { get; }
            public unsafe Silk.NET.WebGPU.Buffer* BufferOut { get; }
            public unsafe Silk.NET.WebGPU.Buffer* StagingBuffer { get; }
            public unsafe BindGroup* BindGroup { get; }
            public unsafe BindGroupLayout* BindGroupLayout { get; }
            public unsafe PipelineLayout* PipelineLayout { get; }

            public unsafe GPUComputeState(
                WebGPU webGPU,
                Instance* instance,
                Adapter* adapter,
                Device* device,
                ShaderModule* shader,
                ComputePipeline* pipeline,
                Silk.NET.WebGPU.Buffer* bufferA,
                Silk.NET.WebGPU.Buffer* bufferB,
                Silk.NET.WebGPU.Buffer* bufferOut,
                Silk.NET.WebGPU.Buffer* stagingBuffer,
                BindGroup* bindGroup,
                BindGroupLayout* bindGroupLayout,
                PipelineLayout* pipelineLayout)
            {
                WebGPU = webGPU;
                Instance = instance;
                Adapter = adapter;
                Device = device;
                Shader = shader;
                Pipeline = pipeline;
                BufferA = bufferA;
                BufferB = bufferB;
                BufferOut = bufferOut;
                StagingBuffer = stagingBuffer;
                BindGroup = bindGroup;
                BindGroupLayout = bindGroupLayout;
                PipelineLayout = pipelineLayout;
            }
        }
    }
}
