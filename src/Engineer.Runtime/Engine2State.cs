#pragma warning disable CA1051 // Do not declare visible instance fields
using Silk.NET.WebGPU;
using Silk.NET.Windowing;

namespace Engineer
{
    public unsafe class Engine2State
    {
        public WebGPU _wgpu = null!;
        public IWindow _window = null!;
        public Surface* _surface;
        public SurfaceConfiguration _surfaceConfiguration;
        public SurfaceCapabilities _surfaceCapabilities;
        public Instance* _instance;
        public Adapter* _adapter;
        public Device* _device;
        public ShaderModule* _shader;
        public RenderPipeline* _pipeline;
    }
}
