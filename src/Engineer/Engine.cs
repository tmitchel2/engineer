#pragma warning disable CA2201 // Do not raise reserved exception types
using System;
using System.Threading.Tasks;
using Silk.NET.Core.Native;
using Silk.NET.Input.Glfw;
using Silk.NET.Input.Sdl;
using Silk.NET.Maths;
using Silk.NET.WebGPU;
using Silk.NET.Windowing;
using Silk.NET.Windowing.Glfw;
using Silk.NET.Windowing.Sdl;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Color = SixLabors.ImageSharp.Color;

namespace Engineer
{
    public sealed class Engine
    {
        private EngineState? _state;

        public void Run()
        {
            var window = CreateWindow();
            window.Load += async () =>
            {
                _state = await CreateEngineStateAsync(window);
                // tcs.SetResult();
            };

            window.Closing += () =>
               {
                   if (_state == null)
                   {
                       return;
                   }

                   Release(_state);
                   _state = null;
               };

            window.Update += (delta) =>
            {

            };

            window.Render += (delta) =>
            {
                if (_state == null)
                {
                    return;
                }

                Render(_state, delta);
            };

            window.FramebufferResize += (Vector2D<int> v) =>
            {
                if (_state == null)
                {
                    return;
                }

                CreateSwapchain(_state);
            };

            window.Run();
        }

        private static IWindow CreateWindow()
        {
            GlfwWindowing.RegisterPlatform();
            SdlWindowing.RegisterPlatform();
            GlfwInput.RegisterPlatform();
            SdlInput.RegisterPlatform();

            var window = Window.Create(WindowOptions.Default with
            {
                API = GraphicsAPI.None,
                Size = new Vector2D<int>(800, 600),
                FramesPerSecond = 60,
                UpdatesPerSecond = 60,
                Position = new Vector2D<int>(0, 0),
                Title = "Engineer",
                IsVisible = true,
                ShouldSwapAutomatically = false,
                IsContextControlDisabled = true,
            });

            return window;
        }

        private static unsafe Task<EngineState> CreateEngineStateAsync(IWindow window)
        {
            var webGPU = WebGPU.GetApi();
            var instanceDescriptor = new InstanceDescriptor();
            var instance = webGPU.CreateInstance(in instanceDescriptor);
            var surface = window.CreateWebGPUSurface(webGPU, instance);
            return GetAdapterAsync(webGPU, instance, surface)
                .ContinueWith(adapter => GetDeviceAsync(webGPU, adapter.Result)
                .ContinueWith(adapterAndDevice =>
                {
                    var (adapter, device) = adapterAndDevice.Result;

                    var shader = CreateShaderModule(webGPU, device.Value);

                    var surfaceCapabilities = GetSurfaceCapabilities(webGPU, adapter.Value, surface);

                    var fragmentState = GetFragmentState(shader, surfaceCapabilities);

                    var pipelineDescriptor = new RenderPipelineDescriptor
                    {
                        Vertex = new VertexState
                        {
                            Module = shader,
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

                    var pipeline = webGPU.DeviceCreateRenderPipeline(
                        device.Value, in pipelineDescriptor);

                    var surfaceConfiguration = CreateSwapchain(
                        webGPU,
                        surfaceCapabilities,
                        device.Value,
                        surface,
                        window);

                    var queue = webGPU.DeviceGetQueue(device.Value);
                    var viewFormat = TextureFormat.Rgba8Unorm;
                    var textureDescriptor = new TextureDescriptor
                    {
                        Size = new Extent3D(1, 1, 1),
                        Format = TextureFormat.Rgba8Unorm,
                        Usage = TextureUsage.CopyDst | TextureUsage.TextureBinding | TextureUsage.RenderAttachment,
                        MipLevelCount = 1,
                        SampleCount = 1,
                        Dimension = TextureDimension.Dimension2D,
                        ViewFormats = &viewFormat,
                        ViewFormatCount = 1
                    };
                    var texture = webGPU.DeviceCreateTexture(device.Value, in textureDescriptor);

                    var textureViewDescriptor = new TextureViewDescriptor
                    {
                        Format = viewFormat,
                        Dimension = TextureViewDimension.Dimension2D,
                        Aspect = TextureAspect.All,
                        MipLevelCount = 1,
                        ArrayLayerCount = 1,
                        BaseArrayLayer = 0,
                        BaseMipLevel = 0
                    };
                    var textureView = webGPU.TextureCreateView(texture, in textureViewDescriptor);

                    var imageCopyTexture = new ImageCopyTexture
                    {
                        Texture = texture,
                        Aspect = TextureAspect.All,
                        MipLevel = 0,
                        Origin = new Origin3D(0, 0, 0)
                    };

                    using var image = new Image<Rgba32>(1, 1);
                    image.Mutate(x => x.BackgroundColor(Color.Red));

                    var textureLayout = new TextureDataLayout
                    {
                        BytesPerRow = (uint)(image.Width * sizeof(Rgba32)),
                        RowsPerImage = (uint)image.Height
                    };

                    var extent = new Extent3D
                    {
                        Width = 1,
                        Height = 1,
                        DepthOrArrayLayers = 1
                    };

                    image.ProcessPixelRows(pixels =>
                    {
                        var row = pixels.GetRowSpan(0);
                        fixed (void* dataPtr = row)
                        {
                            webGPU.QueueWriteTexture(queue, in imageCopyTexture, dataPtr, (nuint)(sizeof(Rgba32) * row.Length), in textureLayout, in extent);
                        }
                    });

                    return new EngineState(
                        webGPU,
                        instance,
                        surface,
                        surfaceConfiguration,
                        surfaceCapabilities,
                        adapter.Value,
                        device.Value,
                        shader,
                        pipeline,
                        queue,
                        texture,
                        textureView,
                        window);
                })).Unwrap();
        }

        private static unsafe FragmentState GetFragmentState(ShaderModule* shader, SurfaceCapabilities surfaceCapabilities)
        {
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
                Format = surfaceCapabilities.Formats[0],
                Blend = &blendState,
                WriteMask = ColorWriteMask.All
            };

            return new FragmentState
            {
                Module = shader,
                TargetCount = 1,
                Targets = &colorTargetState,
                EntryPoint = (byte*)SilkMarshal.StringToPtr("fs_main")
            };
        }

        sealed class AdapterPointer
        {
            public unsafe Adapter* Value;
        }

        private static unsafe Task<AdapterPointer> GetAdapterAsync(WebGPU webGPU, Instance* instance, Surface* surface)
        {
            var tcs = new TaskCompletionSource<AdapterPointer>();
            var instanceDescriptor = new RequestAdapterOptions
            {
                CompatibleSurface = surface,
                // PowerPreference = PowerPreference.HighPerformance,
                // ForceFallbackAdapter = false
            };
            webGPU.InstanceRequestAdapter(instance, in instanceDescriptor, new PfnRequestAdapterCallback((status, adapter, message, userData) =>
            {
                if (status != RequestAdapterStatus.Success)
                {
                    tcs.SetException(new Exception($"Unable to create adapter: {SilkMarshal.PtrToString((nint)message)}"));
                    return;
                }

                tcs.SetResult(new AdapterPointer { Value = adapter });
            }), null);

            return tcs.Task;
        }

        sealed class DevicePointer
        {
            public unsafe Device* Value;
        }

        private static unsafe Task<Tuple<AdapterPointer, DevicePointer>> GetDeviceAsync(WebGPU webGPU, AdapterPointer adapter)
        {
            var tcs = new TaskCompletionSource<Tuple<AdapterPointer, DevicePointer>>();
            var name = FeatureName.TimestampQuery;
            var deviceDescriptor = new DeviceDescriptor
            {
                // RequiredFeaturesCount = 1,
                RequiredFeatures = &name,
                RequiredLimits = null,
                DefaultQueue = default
            };
            webGPU.AdapterRequestDevice(adapter.Value, in deviceDescriptor, new PfnRequestDeviceCallback((status, device, message, userData) =>
            {
                if (status != RequestDeviceStatus.Success)
                {
                    tcs.SetException(new Exception($"Unable to create device: {SilkMarshal.PtrToString((nint)message)}"));
                    return;
                }

                tcs.SetResult(new Tuple<AdapterPointer, DevicePointer>(adapter, new DevicePointer { Value = device }));
            }), null);
            return tcs.Task;
        }

        private static unsafe SurfaceCapabilities GetSurfaceCapabilities(WebGPU webGPU, Adapter* adapter, Surface* surface)
        {
            SurfaceCapabilities surfaceCapabilities = new();
            webGPU.SurfaceGetCapabilities(surface, adapter, ref surfaceCapabilities);
            return surfaceCapabilities;
        }

        private static unsafe void OnUnhandledError(WebGPU webGPU, Device* device)
        {
            void UncapturedError(ErrorType arg0, byte* arg1, void* arg2)
            {
                Console.WriteLine($"{arg0}: {SilkMarshal.PtrToString((nint)arg1)}");
            }

            webGPU.DeviceSetUncapturedErrorCallback(device, new PfnErrorCallback(UncapturedError), null);
        }

        private static unsafe ShaderModule* CreateShaderModule(WebGPU webGPU, Device* device)
        {
            var shaderCode = @"@vertex
fn vs_main(@builtin(vertex_index) in_vertex_index: u32) -> @builtin(position) vec4<f32> {
    let x = f32(i32(in_vertex_index) - 1);
    let y = f32(i32(in_vertex_index & 1u) * 2 - 1);
    return vec4<f32>(x, y, 0.0, 1.0);
}

@fragment
fn fs_main() -> @location(0) vec4<f32> {
    return vec4<f32>(1.0, 0.0, 0.0, 1.0);
}";

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

        private static void Release(EngineState state)
        {
            unsafe
            {
                state.WebGPU.ShaderModuleRelease(state.Shader);
                state.WebGPU.RenderPipelineRelease(state.Pipeline);
                state.WebGPU.DeviceRelease(state.Device);
                state.WebGPU.AdapterRelease(state.Adapter);
                state.WebGPU.SurfaceRelease(state.Surface);
                state.WebGPU.InstanceRelease(state.Instance);
                state.WebGPU.Dispose();
            }
        }

        private static SurfaceConfiguration CreateSwapchain(EngineState state)
        {
            unsafe
            {
                return CreateSwapchain(
                    state.WebGPU,
                    state.SurfaceCapabilities,
                    state.Device,
                    state.Surface,
                    state.Window);
            }
        }

        private static unsafe SurfaceConfiguration CreateSwapchain(
            WebGPU webGPU,
            SurfaceCapabilities surfaceCapabilities,
            Device* device,
            Surface* surface,
            IWindow window)
        {
            var surfaceConfiguration = new SurfaceConfiguration
            {
                Usage = TextureUsage.RenderAttachment,
                Format = surfaceCapabilities.Formats[0],
                PresentMode = PresentMode.Fifo,
                Device = device,
                Width = (uint)window.FramebufferSize.X,
                Height = (uint)window.FramebufferSize.Y
            };

            webGPU.SurfaceConfigure(surface, in surfaceConfiguration);

            return surfaceConfiguration;
        }

        private static void Render(EngineState state, double delta)
        {
            unsafe
            {
                SurfaceTexture surfaceTexture;
                state.WebGPU.SurfaceGetCurrentTexture(state.Surface, &surfaceTexture);
                switch (surfaceTexture.Status)
                {
                    case SurfaceGetCurrentTextureStatus.Timeout:
                    case SurfaceGetCurrentTextureStatus.Outdated:
                    case SurfaceGetCurrentTextureStatus.Lost:
                        // Recreate swapchain,
                        state.WebGPU.TextureRelease(surfaceTexture.Texture);
                        CreateSwapchain(
                            state.WebGPU,
                            state.SurfaceCapabilities,
                            state.Device,
                            state.Surface,
                            state.Window);
                        // Skip this frame
                        return;
                    case SurfaceGetCurrentTextureStatus.OutOfMemory:
                    case SurfaceGetCurrentTextureStatus.DeviceLost:
                    case SurfaceGetCurrentTextureStatus.Force32:
                        throw new Exception($"What is going on bros... {surfaceTexture.Status}");
                }

                var view = state.WebGPU.TextureCreateView(surfaceTexture.Texture, null);

                var commandEncoderDescriptor = new CommandEncoderDescriptor();

                var encoder = state.WebGPU.DeviceCreateCommandEncoder(state.Device, in commandEncoderDescriptor);

                var colorAttachment = new RenderPassColorAttachment
                {
                    View = view,
                    ResolveTarget = null,
                    LoadOp = LoadOp.Clear,
                    StoreOp = StoreOp.Store,
                    ClearValue = new Silk.NET.WebGPU.Color
                    {
                        R = 1,
                        G = 0,
                        B = 1,
                        A = 1
                    }
                };

                var renderPassDescriptor = new RenderPassDescriptor
                {
                    ColorAttachments = &colorAttachment,
                    ColorAttachmentCount = 1,
                    DepthStencilAttachment = null
                };

                var renderPass = state.WebGPU.CommandEncoderBeginRenderPass(encoder, in renderPassDescriptor);

                state.WebGPU.RenderPassEncoderSetPipeline(renderPass, state.Pipeline);
                state.WebGPU.RenderPassEncoderDraw(renderPass, 3, 1, 0, 0);
                state.WebGPU.RenderPassEncoderEnd(renderPass);

                var queue = state.WebGPU.DeviceGetQueue(state.Device);

                var commandBufferDescriptor = new CommandBufferDescriptor();
                var commandBuffer = state.WebGPU.CommandEncoderFinish(encoder, in commandBufferDescriptor);

                state.WebGPU.QueueSubmit(queue, 1, &commandBuffer);
                state.WebGPU.SurfacePresent(state.Surface);
                state.WebGPU.CommandBufferRelease(commandBuffer);
                state.WebGPU.RenderPassEncoderRelease(renderPass);
                state.WebGPU.CommandEncoderRelease(encoder);
                state.WebGPU.TextureViewRelease(view);
                state.WebGPU.TextureRelease(surfaceTexture.Texture);
            }
        }
    }
}
