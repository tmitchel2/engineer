using Silk.NET.WebGPU;

namespace Engineer
{
    public sealed unsafe class SilkGPUEngineState
    {
        public SilkGPUEngineState(
            WebGPU webGPU,
            Instance* instance,
            Adapter* adapter,
            Device* device,
            ShaderModule* shader,
            ComputePipeline* pipeline,
            Buffer* output,
            Buffer* stagingBuffer,
            BindGroup* bindGroup,
            CommandBuffer* command,
            CommandEncoder* commandEncoder)
        {
            WebGPU = webGPU;
            Instance = instance;
            Adapter = adapter;
            Device = device;
            Shader = shader;
            Pipeline = pipeline;
            Output = output;
            StagingBuffer = stagingBuffer;
            BindGroup = bindGroup;
            Command = command;
            CommandEncoder = commandEncoder;
        }

        public WebGPU WebGPU { get; init; }
        public Instance* Instance { get; init; }
        public Adapter* Adapter { get; init; }
        public Device* Device { get; init; }
        public ShaderModule* Shader { get; init; }
        public ComputePipeline* Pipeline { get; init; }
        public Buffer* Output { get; init; }
        public Buffer* StagingBuffer { get; init; }
        public BindGroup* BindGroup { get; init; }
        public CommandBuffer* Command { get; init; }
        public CommandEncoder* CommandEncoder { get; init; }
    }
}
