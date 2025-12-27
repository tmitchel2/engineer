// using System;
// using System.Diagnostics;
// using System.Runtime.InteropServices;
// using System.Threading.Tasks;
// using Silk.NET.Core.Native;
// using Silk.NET.WebGPU;
// using Silk.NET.WebGPU.Extensions.WGPU;
// using SixLabors.ImageSharp.Processing;

// // https://surma.dev/things/webgpu/
// namespace Engineer
// {
//     public class SilkGPUEngine<T>
//     {
//         // public const int BUFFER_SIZE = 1000;

//         // private const string SHADER = @"
//         // @compute @workgroup_size(64)
//         // fn main() {
//         // }";

//         // private const string SHADER_CODE = @"
//         // @group(0)
//         // @binding(0)
//         // var<storage, read_write> output: array<f32>;

//         // @compute
//         // @workgroup_size(64)
//         // fn main(
//         //   @builtin(global_invocation_id)
//         //   global_id : vec3<u32>,

//         //   @builtin(local_invocation_id)
//         //   local_id : vec3<u32>,
//         // ) {
//         //   output[global_id.x] = f32(global_id.x) * 1000. + f32(local_id.x);
//         // }";

//         private const string SHADER_CODE = @"
// @group(0)
// @binding(0)
// var<storage, read_write> v_indices: array<u32>; // this is used as both input and output for convenience
// // The Collatz Conjecture states that for any integer n:
// // If n is even, n = n/2
// // If n is odd, n = 3n+1
// // And repeat this process for each new n, you will always eventually reach 1.
// // Though the conjecture has not been proven, no counterexample has ever been found.
// // This function returns how many times this recurrence needs to be applied to reach 1.
// fn collatz_iterations(n_base: u32) -> u32{
//     var n: u32 = n_base;
//     var i: u32 = 0u;
//     loop {
//         if (n <= 1u) {
//             break;
//         }
//         if (n % 2u == 0u) {
//             n = n / 2u;
//         }
//         else {
//             // Overflow? (i.e. 3*n + 1 > 0xffffffffu?)
//             if (n >= 1431655765u) {   // 0x55555555u
//                 return 4294967295u;   // 0xffffffffu
//             }
// n = 3u * n + 1u;
//         }
//         i = i + 1u;
//     }
//     return i;
// }
// @compute
// @workgroup_size(1)
// fn main(@builtin(global_invocation_id) global_id: vec3<u32>) {
//     v_indices[global_id.x] = collatz_iterations(v_indices[global_id.x]);
// }";

//         public async Task<T[,]> RunAsync()
//         {
//             var numbers = new int[] { 1, 4, 3, 295 };
//             var state = await CreateAsync(numbers);
//             var result = await GetResultAsync(state, GetByteLength(numbers));
//             CleanUpCommand(state);
//             Close(state);
//             return new T[,] { };
//         }

//         private unsafe static Task<SilkGPUEngineState> CreateAsync(int[] numbers)
//         {
//             var webGPU = WebGPU.GetApi();
//             var instance = CreateInstance(webGPU);
//             var task = CreateAdapterAsync(webGPU, instance)
//                 .ContinueWith((adapterIntPtr) =>
//                 {
//                     var adapter = (Adapter*)adapterIntPtr.Result;
//                     PrintAdapterFeatures(webGPU, adapter);
//                     var deviceTask = CreateDevice(webGPU, adapter);
//                     return deviceTask
//                         .ContinueWith((deviceIntPtr) =>
//                         {
//                             var device = (Device*)deviceIntPtr.Result;
//                             SetLogCallback(webGPU, device);
//                             SetUncapturedErrorCallback(webGPU, device);
//                             var shaderModule = CreateShaderModule(webGPU, device);
//                             var stagingBuffer = CreateStagingBuffer(webGPU, device, numbers);
//                             var storageBuffer = CreateStorageBuffer(webGPU, device, numbers);

//                             var copyArrayBuffer = (uint*)webGPU.BufferGetMappedRange(storageBuffer, 0, (nuint)GetByteLength(numbers));
//                             fixed (int* data = numbers)
//                             {
//                                 System.Buffer.MemoryCopy(data, copyArrayBuffer, GetByteLength(numbers), GetByteLength(numbers));
//                             }
//                             webGPU.BufferUnmap(storageBuffer);

//                             var bindGroupLayout = CreateBindGroupLayout(webGPU, device, numbers);
//                             var computePipelineLayout = CreateComputePipelineLayout(webGPU, device, bindGroupLayout);
//                             var computePipelineTask = CreateComputePipelineAsync(webGPU, device, shaderModule, computePipelineLayout);
//                             return computePipelineTask
//                             .ContinueWith((computePipelineIntPtr) =>
//                             {
//                                 var computePipeline = (ComputePipeline*)computePipelineIntPtr.Result;
//                                 var commandEncoder = CreateCommandEncoder(webGPU, device);
//                                 var bindGroupLayout2 = GetBindGroupLayout(webGPU, computePipeline);
//                                 var bindGroup = CreateBindGroup(webGPU, device, bindGroupLayout2, storageBuffer, numbers);

//                                 EncodeComputePass(webGPU, commandEncoder, computePipeline, bindGroup, numbers);
//                                 webGPU.CommandEncoderCopyBufferToBuffer(commandEncoder, storageBuffer, 0, stagingBuffer, 0, GetByteLength(numbers));

//                                 // Finish command
//                                 var queue = webGPU.DeviceGetQueue(device);
//                                 var descriptor = new CommandBufferDescriptor();
//                                 var command = webGPU.CommandEncoderFinish(commandEncoder, in descriptor);
//                                 webGPU.QueueSubmit(queue, 1, &command);

//                                 return new SilkGPUEngineState(
//                                     webGPU, instance, adapter, device, shaderModule, computePipeline,
//                                     storageBuffer, stagingBuffer, bindGroup, command, commandEncoder);
//                             });
//                         });
//                 });

//             return task.Unwrap().Unwrap();
//         }

//         private static unsafe BindGroupLayout* GetBindGroupLayout(WebGPU webGPU, ComputePipeline* computePipeline)
//         {
//             return webGPU.ComputePipelineGetBindGroupLayout(computePipeline, 0);
//         }

//         private unsafe static Instance* CreateInstance(WebGPU webGPU)
//         {
//             var instanceDescriptor = new InstanceDescriptor();
//             return webGPU.CreateInstance(&instanceDescriptor);
//         }

//         private unsafe static Task<IntPtr> CreateAdapterAsync(WebGPU webGPU, Instance* instance)
//         {
//             var task = new TaskCompletionSource<IntPtr>();
//             var requestAdapterOptions = new RequestAdapterOptions { };
//             webGPU.InstanceRequestAdapter
//             (
//                 instance,
//                 in requestAdapterOptions,
//                 new PfnRequestAdapterCallback((status, adapter, message, userData) =>
//                 {
//                     if (status != RequestAdapterStatus.Success)
//                     {
//                         task.SetException(new Exception($"Unable to create adapter: {SilkMarshal.PtrToString((nint)message)}"));
//                         return;
//                     }

//                     task.SetResult((IntPtr)adapter);
//                 }),
//                 null
//             );

//             return task.Task;
//         }

//         private unsafe static Task<IntPtr> CreateDevice(WebGPU webGPU, Adapter* adapter)
//         {
//             var task = new TaskCompletionSource<IntPtr>();
//             var deviceDescriptor = new DeviceDescriptor
//             {
//                 DeviceLostCallback = new PfnDeviceLostCallback(DeviceLost),
//             };

//             webGPU.AdapterRequestDevice
//             (
//                 adapter,
//                 in deviceDescriptor,
//                 new PfnRequestDeviceCallback((status, device, message, userData) =>
//                 {
//                     if (status != RequestDeviceStatus.Success)
//                     {
//                         task.SetException(new Exception($"Unable to create device: {SilkMarshal.PtrToString((nint)message)}"));
//                         return;
//                     }

//                     task.SetResult((IntPtr)device);
//                 }),
//                 null
//             );

//             return task.Task;
//         }

//         private unsafe static void SetUncapturedErrorCallback(WebGPU webGPU, Device* device)
//         {
//             webGPU.DeviceSetUncapturedErrorCallback(device, new PfnErrorCallback(UncapturedError), null);
//         }

//         private unsafe static void SetLogCallback(WebGPU webGPU, Device* device)
//         {
//             webGPU.TryGetDeviceExtension<Wgpu>(device, out var wgpu);
//             wgpu.SetLogLevel(LogLevel.Trace);
//             wgpu.SetLogCallback(new PfnLogCallback((arg0, arg1, arg2) =>
//             {
//                 var log = SilkMarshal.PtrToString((nint)arg1);
//                 if (log == null)
//                 {
//                     return;
//                 }
//                 Log(log);
//             }), null);
//         }

//         private unsafe static ShaderModule* CreateShaderModule(WebGPU webGPU, Device* device)
//         {
//             var wgslDescriptor = new ShaderModuleWGSLDescriptor
//             {
//                 Code = (byte*)SilkMarshal.StringToPtr(SHADER_CODE),
//                 Chain = new ChainedStruct
//                 {
//                     SType = SType.ShaderModuleWgslDescriptor
//                 }
//             };

//             var shaderModuleDescriptor = new ShaderModuleDescriptor
//             {
//                 NextInChain = (ChainedStruct*)&wgslDescriptor,
//             };

//             return webGPU.DeviceCreateShaderModule(device, in shaderModuleDescriptor);
//         }

//         private static unsafe BindGroup* CreateBindGroup(WebGPU webGPU, Device* device, BindGroupLayout* bindGroupLayout, Silk.NET.WebGPU.Buffer* outputBuffer, int[] numbers)
//         {
//             var bindGroupEntry = new BindGroupEntry
//             {
//                 Binding = 0,
//                 Buffer = outputBuffer,
//                 Size = GetByteLength(numbers),
//             };

//             var bindGroupDescriptor = new BindGroupDescriptor
//             {
//                 Entries = &bindGroupEntry,
//                 EntryCount = 1,
//                 Layout = bindGroupLayout,
//             };

//             return webGPU.DeviceCreateBindGroup(device, in bindGroupDescriptor);
//         }

//         private static unsafe Silk.NET.WebGPU.Buffer* CreateStagingBuffer(WebGPU webGPU, Device* device, int[] numbers)
//         {
//             var stagingBufferDescriptor = new BufferDescriptor
//             {
//                 Size = GetByteLength(numbers),
//                 Usage = BufferUsage.MapRead | BufferUsage.CopyDst
//             };

//             return webGPU.DeviceCreateBuffer(device, in stagingBufferDescriptor);
//         }

//         private static unsafe Silk.NET.WebGPU.Buffer* CreateStorageBuffer(WebGPU webGPU, Device* device, int[] numbers)
//         {
//             var outputBufferDescriptor = new BufferDescriptor
//             {
//                 Label = (byte*)SilkMarshal.StringToPtr("Storage Buffer"),
//                 Size = GetByteLength(numbers),
//                 Usage = BufferUsage.Storage | BufferUsage.CopyDst | BufferUsage.CopySrc,
//                 MappedAtCreation = true
//             };
//             return webGPU.DeviceCreateBuffer(device, in outputBufferDescriptor);
//         }

//         private static unsafe ulong GetByteLength(int[] numbers)
//         {
//             return (ulong)System.Buffer.ByteLength(numbers);
//         }

//         private static unsafe Task<IntPtr> CreateComputePipelineAsync(WebGPU webGPU, Device* device, ShaderModule* computeModule, PipelineLayout* computePipelineLayout)
//         {
//             var computePipelineDescriptor = new ComputePipelineDescriptor
//             {
//                 Layout = computePipelineLayout,
//                 Compute = new ProgrammableStageDescriptor
//                 {
//                     Module = computeModule,
//                     EntryPoint = (byte*)SilkMarshal.StringToPtr("main"),
//                 }
//             };

//             var computePipeline = webGPU.DeviceCreateComputePipeline(device, in computePipelineDescriptor);
//             return Task.FromResult((IntPtr)computePipeline);

//             // var task = new TaskCompletionSource<IntPtr>();
//             // webGPU.DeviceCreateComputePipelineAsync(device, in computePipelineDescriptor, new PfnCreateComputePipelineAsyncCallback((arg0, computePipeline, arg2, arg3) =>
//             //     {
//             //         if (arg0 != CreatePipelineAsyncStatus.Success)
//             //         {
//             //             task.SetException(new Exception($"Unable to create compute pipeline"));
//             //             return;
//             //         }

//             //         task.SetResult((IntPtr)computePipeline);
//             //     }), null);

//             // webGPU.TryGetDeviceExtension<Wgpu>(device, out var wgpu);
//             // wgpu.DevicePoll(device, true, null);
//             // return task.Task;
//         }

//         private static unsafe PipelineLayout* CreateComputePipelineLayout(WebGPU webGPU, Device* device, BindGroupLayout* bindGroupLayout)
//         {
//             var bindGroupLayouts = stackalloc BindGroupLayout*[1];
//             bindGroupLayouts[0] = bindGroupLayout;

//             var pipelineLayoutDescriptor = new PipelineLayoutDescriptor
//             {
//                 BindGroupLayoutCount = 1,
//                 BindGroupLayouts = bindGroupLayouts,
//             };

//             var computePipelineLayout = webGPU.DeviceCreatePipelineLayout(device, in pipelineLayoutDescriptor);
//             return computePipelineLayout;
//         }

//         private static unsafe BindGroupLayout* CreateBindGroupLayout(WebGPU webGPU, Device* device, int[] numbers)
//         {
//             var entries = stackalloc BindGroupLayoutEntry[1];
//             entries[0] = new BindGroupLayoutEntry
//             {
//                 Binding = 0,
//                 Visibility = ShaderStage.Compute,
//                 Buffer = new BufferBindingLayout
//                 {
//                     Type = BufferBindingType.Storage,
//                     MinBindingSize = GetByteLength(numbers),
//                 }
//             };

//             var bindGroupLayoutDescriptor = new BindGroupLayoutDescriptor
//             {
//                 EntryCount = 1,
//                 Entries = entries
//             };

//             var bindGroupLayout = webGPU.DeviceCreateBindGroupLayout(device, in bindGroupLayoutDescriptor);
//             return bindGroupLayout;
//         }

//         private unsafe static CommandEncoder* CreateCommandEncoder(WebGPU webGPU, Device* device)
//         {
//             var commandEncoderDescriptor = new CommandEncoderDescriptor();
//             return webGPU.DeviceCreateCommandEncoder(device, in commandEncoderDescriptor);
//         }

//         private static unsafe void EncodeComputePass(WebGPU webGPU, CommandEncoder* commandEncoder, ComputePipeline* computePipeline, BindGroup* bindGroup, int[] numbers)
//         {
//             var computePassDescriptor = new ComputePassDescriptor { };
//             var computePassEncoder = webGPU.CommandEncoderBeginComputePass(commandEncoder, in computePassDescriptor);
//             webGPU.ComputePassEncoderSetPipeline(computePassEncoder, computePipeline);
//             webGPU.ComputePassEncoderSetBindGroup(computePassEncoder, 0, bindGroup, 0, 0);
//             // webGPU.ComputePassEncoderInsertDebugMarker();
//             // webGPU.ComputePassEncoderDispatchWorkgroups(computePassEncoder, (uint)Math.Ceiling(GetByteLength(numbers) / 64.0d), 1, 1);
//             webGPU.ComputePassEncoderDispatchWorkgroups(computePassEncoder, (uint)numbers.Length, 1, 1);
//             webGPU.ComputePassEncoderEnd(computePassEncoder);
//         }

//         private unsafe static Task MapStagingBufferAsync(SilkGPUEngineState state, ulong bufferSize)
//         {
//             var task = new TaskCompletionSource();
//             state.WebGPU.BufferMapAsync(state.StagingBuffer, MapMode.Read, 0, (nuint)bufferSize,
//                 new PfnBufferMapCallback((arg0, data) =>
//                 {
//                     if (arg0 != BufferMapAsyncStatus.Success)
//                     {
//                         task.SetException(new Exception($"Unable to map buffer! status: {arg0}"));
//                         return;
//                     }

//                     task.SetResult();
//                 }), null);

//             state.WebGPU.TryGetDeviceExtension<Wgpu>(state.Device, out var wgpu);
//             wgpu.DevicePoll(state.Device, true, null);

//             return task.Task;
//         }

//         private static async Task<int[]> GetResultAsync(SilkGPUEngineState state, ulong bufferSize)
//         {
//             await MapStagingBufferAsync(state, bufferSize);
//             var result = ReadResult(state, (nuint)bufferSize);
//             UnmapStagingBuffer(state);
//             return result;
//         }

//         private unsafe static void UnmapStagingBuffer(SilkGPUEngineState state)
//         {
//             state.WebGPU.BufferUnmap(state.StagingBuffer);
//         }

//         private unsafe static int[] ReadResult(SilkGPUEngineState state, nuint bufferSize)
//         {
//             var nativeResultPointer = state.WebGPU.BufferGetMappedRange(state.StagingBuffer, 0, bufferSize);
//             var result = new int[4];
//             fixed (int* managedResultPointer = result)
//             {
//                 System.Buffer.MemoryCopy(nativeResultPointer, managedResultPointer, bufferSize, bufferSize);
//             }
//             return result;
//         }

//         private unsafe static void CleanUpCommand(SilkGPUEngineState state)
//         {
//             state.WebGPU.CommandBufferRelease(state.Command);
//             // state.WebGPU.ComputePassEncoderRelease(state.ComputePassEncoder);
//             state.WebGPU.CommandEncoderRelease(state.CommandEncoder);
//         }

//         private unsafe static void PrintAdapterFeatures(WebGPU webGPU, Adapter* adapter)
//         {
//             var count = (int)webGPU.AdapterEnumerateFeatures(adapter, null);
//             var features = stackalloc FeatureName[count];
//             webGPU.AdapterEnumerateFeatures(adapter, features);
//             Log("Adapter features:");
//             for (var i = 0; i < count; i++)
//             {
//                 Log($"\t{features[i]}");
//             }
//         }

//         private unsafe static void Close(SilkGPUEngineState state)
//         {
//             state.WebGPU.ShaderModuleRelease(state.Shader);
//             state.WebGPU.ComputePipelineRelease(state.Pipeline);
//             state.WebGPU.DeviceRelease(state.Device);
//             state.WebGPU.AdapterRelease(state.Adapter);
//             state.WebGPU.InstanceRelease(state.Instance);
//             state.WebGPU.Dispose();
//         }

//         private unsafe static void DeviceLost(DeviceLostReason arg0, byte* arg1, void* arg2)
//         {
//             Log($"Device lost! Reason: {arg0} Message: {SilkMarshal.PtrToString((nint)arg1)}");
//         }

//         private unsafe static void UncapturedError(ErrorType arg0, byte* arg1, void* arg2)
//         {
//             Log($"{arg0}: {SilkMarshal.PtrToString((nint)arg1)}");
//         }

//         private static void Log(string value)
//         {
//             Debug.WriteLine(value);
//         }
//     }
// }
