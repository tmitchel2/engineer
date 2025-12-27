using Silk.NET.Windowing;
using Silk.NET.WebGPU;

namespace Engineer
{
    public sealed unsafe class EngineState
    {
        public EngineState(
            WebGPU webGPU,
            Instance* instance,
            Surface* surface,
            SurfaceConfiguration surfaceConfiguration,
            SurfaceCapabilities surfaceCapabilities,
            Adapter* adapter,
            Device* device,
            ShaderModule* shader,
            RenderPipeline* pipeline,
            Queue* queue,
            Texture* texture,
            TextureView* textureView,
            IWindow window)
        {
            WebGPU = webGPU;
            Instance = instance;
            Surface = surface;
            SurfaceConfiguration = surfaceConfiguration;
            SurfaceCapabilities = surfaceCapabilities;
            Adapter = adapter;
            Device = device;
            Shader = shader;
            Pipeline = pipeline;
            Queue = queue;
            Texture = texture;
            TextureView = textureView;
            Window = window;
        }

        public WebGPU WebGPU { get; }
        public Instance* Instance { get; }
        public Surface* Surface { get; }
        public SurfaceConfiguration SurfaceConfiguration { get; }
        public SurfaceCapabilities SurfaceCapabilities { get; }
        public Adapter* Adapter { get; }
        public Device* Device { get; }
        public ShaderModule* Shader { get; }
        public RenderPipeline* Pipeline { get; }
        public Queue* Queue { get; }
        public Texture* Texture { get; }
        public TextureView* TextureView { get; }
        public IWindow Window { get; }
    };
}
