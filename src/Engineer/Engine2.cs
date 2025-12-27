#pragma warning disable CA2201 // Do not raise reserved exception types
using System;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;

namespace Engineer
{
    public static unsafe class Engine2
    {
        private const string SHADER = @"@vertex
fn vs_main(@builtin(vertex_index) in_vertex_index: u32) -> @builtin(position) vec4<f32> {
    let x = f32(i32(in_vertex_index) - 1);
    let y = f32(i32(in_vertex_index & 1u) * 2 - 1);
    return vec4<f32>(x, y, 0.0, 1.0);
}

@fragment
fn fs_main() -> @location(0) vec4<f32> {
    return vec4<f32>(1.0, 0.0, 0.0, 1.0);
}";

        public static void Run()
        {
            var options = WindowOptions.Default;
            options.API = GraphicsAPI.None;
            options.Size = new Vector2D<int>(800, 600);
            options.FramesPerSecond = 60;
            options.UpdatesPerSecond = 60;
            options.Position = new Vector2D<int>(0, 0);
            options.Title = "WebGPU Triangle";
            options.IsVisible = true;
            options.ShouldSwapAutomatically = false;
            options.IsContextControlDisabled = true;

            var window = Window.Create(options);
            var state = new Engine2State { _window = window };
            window.Load += () => WindowOnLoad(state);
            window.Closing += () => WindowClosing(state);
            window.Update += (delta) => WindowOnUpdate(state, delta);
            window.Render += (delta) => WindowOnRender(state, delta);
            window.FramebufferResize += (vector) => FramebufferResize(state, vector);
            window.Run();
        }

        private static void FramebufferResize(Engine2State state, Vector2D<int> obj)
        {
            CreateSwapchain(state);
        }

        private static void WindowOnLoad(Engine2State state)
        {
            state._wgpu = WebGPU.GetApi();

            var instanceDescriptor = new InstanceDescriptor();
            state._instance = state._wgpu.CreateInstance(&instanceDescriptor);

            state._surface = state._window.CreateWebGPUSurface(state._wgpu, state._instance);

            var requestAdapterOptions = new RequestAdapterOptions
            {
                CompatibleSurface = state._surface,
                // PowerPreference = PowerPreference.HighPerformance,
                // ForceFallbackAdapter = false
            };

            state._wgpu.InstanceRequestAdapter
            (
                state._instance,
                in requestAdapterOptions,
                new PfnRequestAdapterCallback((status, adapter1, message, userData) =>
                {
                    if (status != RequestAdapterStatus.Success)
                    {
                        throw new Exception($"Unable to create adapter: {SilkMarshal.PtrToString((nint)message)}");
                    }

                    state._adapter = adapter1;
                }),
                null
            );

            PrintAdapterFeatures(state);

            var deviceDescriptor = new DeviceDescriptor
            {
                DeviceLostCallback = new PfnDeviceLostCallback(DeviceLost),
            };

            state._wgpu.AdapterRequestDevice
            (
                state._adapter,
                in deviceDescriptor,
                new PfnRequestDeviceCallback((status, device, message, userData) =>
                {
                    if (status != RequestDeviceStatus.Success)
                    {
                        throw new Exception($"Unable to create device: {SilkMarshal.PtrToString((nint)message)}");
                    }

                    state._device = device;
                }),
                null
            );

            state._wgpu.DeviceSetUncapturedErrorCallback(state._device, new PfnErrorCallback(UncapturedError), null);

            var wgslDescriptor = new ShaderModuleWGSLDescriptor
            {
                Code = (byte*)SilkMarshal.StringToPtr(SHADER),
                Chain = new ChainedStruct
                {
                    SType = SType.ShaderModuleWgslDescriptor
                }
            };

            var shaderModuleDescriptor = new ShaderModuleDescriptor
            {
                NextInChain = (ChainedStruct*)&wgslDescriptor,
            };

            state._shader = state._wgpu.DeviceCreateShaderModule(state._device, in shaderModuleDescriptor);

            state._wgpu.SurfaceGetCapabilities(state._surface, state._adapter, ref state._surfaceCapabilities);

            var blendState = new BlendState
            {
                Color = new BlendComponent
                {
                    SrcFactor = BlendFactor.One,
                    DstFactor = BlendFactor.Zero,
                    Operation = BlendOperation.Add
                },
                Alpha = new BlendComponent
                {
                    SrcFactor = BlendFactor.One,
                    DstFactor = BlendFactor.Zero,
                    Operation = BlendOperation.Add
                }
            };

            var colorTargetState = new ColorTargetState
            {
                Format = state._surfaceCapabilities.Formats[0],
                Blend = &blendState,
                WriteMask = ColorWriteMask.All
            };

            var fragmentState = new FragmentState
            {
                Module = state._shader,
                TargetCount = 1,
                Targets = &colorTargetState,
                EntryPoint = (byte*)SilkMarshal.StringToPtr("fs_main")
            };

            var renderPipelineDescriptor = new RenderPipelineDescriptor
            {
                Vertex = new VertexState
                {
                    Module = state._shader,
                    EntryPoint = (byte*)SilkMarshal.StringToPtr("vs_main"),
                },
                Primitive = new PrimitiveState
                {
                    Topology = PrimitiveTopology.TriangleList,
                    StripIndexFormat = IndexFormat.Undefined,
                    FrontFace = FrontFace.Ccw,
                    CullMode = CullMode.None
                },
                Multisample = new MultisampleState
                {
                    Count = 1,
                    Mask = ~0u,
                    AlphaToCoverageEnabled = false
                },
                Fragment = &fragmentState,
                DepthStencil = null
            };

            state._pipeline = state._wgpu.DeviceCreateRenderPipeline(state._device, in renderPipelineDescriptor);

            CreateSwapchain(state);
        }

        private static void WindowClosing(Engine2State state)
        {
            state._wgpu.ShaderModuleRelease(state._shader);
            state._wgpu.RenderPipelineRelease(state._pipeline);
            state._wgpu.DeviceRelease(state._device);
            state._wgpu.AdapterRelease(state._adapter);
            state._wgpu.SurfaceRelease(state._surface);
            state._wgpu.InstanceRelease(state._instance);
            state._wgpu.Dispose();
        }

        private static void CreateSwapchain(Engine2State state)
        {
            state._surfaceConfiguration = new SurfaceConfiguration
            {
                Usage = TextureUsage.RenderAttachment,
                Format = state._surfaceCapabilities.Formats[0],
                PresentMode = PresentMode.Fifo,
                Device = state._device,
                Width = (uint)state._window.FramebufferSize.X,
                Height = (uint)state._window.FramebufferSize.Y
            };

            state._wgpu.SurfaceConfigure(state._surface, in state._surfaceConfiguration);
        }

        private static void WindowOnUpdate(Engine2State state, double delta) { }

        private static void WindowOnRender(Engine2State state, double delta)
        {
            SurfaceTexture surfaceTexture;
            state._wgpu.SurfaceGetCurrentTexture(state._surface, &surfaceTexture);
            switch (surfaceTexture.Status)
            {
                case SurfaceGetCurrentTextureStatus.Timeout:
                case SurfaceGetCurrentTextureStatus.Outdated:
                case SurfaceGetCurrentTextureStatus.Lost:
                    // Recreate swapchain,
                    state._wgpu.TextureRelease(surfaceTexture.Texture);
                    CreateSwapchain(state);
                    // Skip this frame
                    return;
                case SurfaceGetCurrentTextureStatus.OutOfMemory:
                case SurfaceGetCurrentTextureStatus.DeviceLost:
                case SurfaceGetCurrentTextureStatus.Force32:
                    throw new Exception($"What is going on bros... {surfaceTexture.Status}");
            }

            var view = state._wgpu.TextureCreateView(surfaceTexture.Texture, null);

            var commandEncoderDescriptor = new CommandEncoderDescriptor();

            var encoder = state._wgpu.DeviceCreateCommandEncoder(state._device, in commandEncoderDescriptor);

            var colorAttachment = new RenderPassColorAttachment
            {
                View = view,
                ResolveTarget = null,
                LoadOp = LoadOp.Clear,
                StoreOp = StoreOp.Store,
                ClearValue = new Color
                {
                    R = 0,
                    G = 1,
                    B = 0,
                    A = 1
                }
            };

            var renderPassDescriptor = new RenderPassDescriptor
            {
                ColorAttachments = &colorAttachment,
                ColorAttachmentCount = 1,
                // DepthStencilAttachment = null
            };

            var renderPass = state._wgpu.CommandEncoderBeginRenderPass(encoder, in renderPassDescriptor);

            state._wgpu.RenderPassEncoderSetPipeline(renderPass, state._pipeline);
            state._wgpu.RenderPassEncoderDraw(renderPass, 3, 1, 0, 0);
            state._wgpu.RenderPassEncoderEnd(renderPass);

            var queue = state._wgpu.DeviceGetQueue(state._device);

            var descriptor = new CommandBufferDescriptor();
            var commandBuffer = state._wgpu.CommandEncoderFinish(encoder, in descriptor);

            state._wgpu.QueueSubmit(queue, 1, &commandBuffer);
            state._wgpu.SurfacePresent(state._surface);
            state._wgpu.CommandBufferRelease(commandBuffer);
            state._wgpu.RenderPassEncoderRelease(renderPass);
            state._wgpu.CommandEncoderRelease(encoder);
            state._wgpu.TextureViewRelease(view);
            state._wgpu.TextureRelease(surfaceTexture.Texture);
        }

        private static void PrintAdapterFeatures(Engine2State state)
        {
            var count = (int)state._wgpu.AdapterEnumerateFeatures(state._adapter, null);

            var features = stackalloc FeatureName[count];

            state._wgpu.AdapterEnumerateFeatures(state._adapter, features);

            Console.WriteLine("Adapter features:");

            for (var i = 0; i < count; i++)
            {
                Console.WriteLine($"\t{features[i]}");
            }
        }

        private static void DeviceLost(DeviceLostReason arg0, byte* arg1, void* arg2)
        {
            Console.WriteLine($"Device lost! Reason: {arg0} Message: {SilkMarshal.PtrToString((nint)arg1)}");
        }

        private static void UncapturedError(ErrorType arg0, byte* arg1, void* arg2)
        {
            Console.WriteLine($"{arg0}: {SilkMarshal.PtrToString((nint)arg1)}");
        }
    }
}
