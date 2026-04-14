# Engineer

A .NET 10 game engine and GPU compute framework built on top of [Silk.NET](https://github.com/dotnet/Silk.NET) and WebGPU. Engineer exposes a unified `IGPU` abstraction that can run 2D compute kernels either on the CPU (for debugging and fallback) or on the GPU via auto-generated WGSL shaders, and ships a WebGPU-backed rendering pipeline with a small graphics primitives library.

## Features

- **Unified compute abstraction** — write a kernel once as a C# `Expression<Func<GPUContext, T[,], T[,], T>>` and execute it on either the CPU or GPU via a common `IGPU` interface.
- **Expression-tree to WGSL compiler** — `WGSLCompiler` walks LINQ expression trees and emits equivalent WGSL compute shaders for types including `int`, `uint`, `float`, `double`, and `bool`.
- **WebGPU rendering** — cross-platform rendering stack using Silk.NET WebGPU bindings (WGPU/Dawn), with surface configuration, swapchain management, texture uploads (via ImageSharp), and a minimal render-pass pipeline.
- **Graphics primitives** — `Geometry`, `MeshNode`, `Plane`, `BoxProps`, and `Node` types under `Engineer.Graphics` for building meshes and scene graphs.
- **Windowing and input** — GLFW and SDL backends via Silk.NET Windowing/Input.
- **Graceful GPU fallback** — `SilkGPUEngine` transparently falls back to CPU execution if WGSL compilation or GPU dispatch fails.
- **Test coverage** — xUnit-style tests across CPU/GPU kernels, graphics primitives, and the WGSL compiler, with Cobertura coverage reporting via ReportGenerator.

## Repository layout

```
engineer/
├── global.json                 # Pins .NET SDK 10.0.100 with latestFeature roll-forward
├── claude-yolo.sh              # Convenience launcher for Claude Code with permission skipping
├── copilot-yolo.sh             # Convenience launcher for GitHub Copilot CLI
└── src/
    ├── Directory.Build.props   # Shared analyzers (Roslynator), warnings-as-errors, GitVersioning
    ├── Engineer.slnx           # Solution file
    ├── settings.runsettings    # Test/coverage configuration
    ├── build.sh                # Runs tests + generates coverage report
    ├── Engineer/               # Main library
    │   ├── IGPU.cs             # Kernel abstraction
    │   ├── CpuGPU.cs           # CPU implementation
    │   ├── SilkGPU.cs          # Silk.NET/WebGPU implementation
    │   ├── SilkGPUEngine.cs    # WGSL dispatch + CPU fallback
    │   ├── WGSLCompiler.cs     # LINQ expression → WGSL translation
    │   ├── GPUContext.cs       # Per-kernel-invocation context
    │   ├── GPUThread.cs        # (X, Y) thread coordinates
    │   ├── KernelOptions.cs    # Kernel dispatch dimensions
    │   ├── Engine.cs           # Primary WebGPU render loop
    │   ├── Engine2.cs          # Alternate render engine prototype
    │   ├── Program.cs          # Entry point (no-op by default)
    │   └── Graphics/           # Mesh, geometry, and scene-graph primitives
    └── Engineer.Tests/         # xUnit tests mirroring the main library layout
```

## Requirements

- .NET SDK `10.0.100` or later (see `global.json`).
- A GPU and OS that supports a WebGPU backend (WGPU on Linux/Windows, Dawn on macOS). Vulkan, Metal, and D3D12 backends are supported transitively through WGPU/Dawn.
- Native windowing prerequisites (GLFW or SDL) are pulled in as managed NuGet packages via Silk.NET.

## Getting started

Clone the repository, restore dependencies, and build:

```bash
git clone <repo-url> engineer
cd engineer/src
dotnet restore Engineer.slnx
dotnet build Engineer.slnx
```

Run the (currently no-op) entry point:

```bash
dotnet run --project Engineer/Engineer.csproj
```

The `Main` method in `Program.cs` is intentionally empty — Engineer is primarily consumed as a library. Uncomment the `Engine2` instantiation in `Program.cs` to launch the sample render loop.

## Testing and coverage

Run the full non-integration test suite and generate an HTML coverage report:

```bash
cd src
./build.sh
```

This runs `dotnet test` with `XPlat Code Coverage`, then invokes ReportGenerator to produce a Cobertura-backed HTML report under `src/bin/test/results/codecoverage/`. The script prints a `file://` URL to the generated index at the end.

To run tests directly without coverage:

```bash
cd src
dotnet test Engineer.slnx --filter "TestCategory!=Integration"
```

## Usage

### Running a compute kernel

```csharp
using Engineer;

IGPU gpu = new SilkGPU();           // or new CpuGPU() for a pure-CPU execution path
var options = new KernelOptions(XCount: 256, YCount: 256);

var kernel = gpu.CreateKernel2DExpr<float>(
    (ctx, a, b) => a[ctx.Thread.X, ctx.Thread.Y] + b[ctx.Thread.X, ctx.Thread.Y],
    options);

float[,] a = new float[256, 256];
float[,] b = new float[256, 256];
// ... populate a and b ...

float[,] result = await kernel(a, b);
```

The expression is translated to WGSL by `WGSLCompiler` and dispatched through WebGPU. If translation or dispatch fails, `SilkGPUEngine` falls back to compiling and running the same expression on the CPU so behavior remains consistent.

### Running the render loop

```csharp
using Engineer;

var engine = new Engine();
engine.Run();   // opens an 800×600 window and renders a WebGPU triangle
```

`Engine.cs` demonstrates adapter/device acquisition, shader module creation (a red-triangle WGSL sample is inlined), surface configuration, and a per-frame render pass.

## Code quality

- `TreatWarningsAsErrors` is enabled project-wide via `Directory.Build.props`.
- Roslynator analyzers (`Roslynator.Analyzers`, `Roslynator.CodeAnalysis.Analyzers`, `Roslynator.Formatting.Analyzers`) are applied to every project.
- Nullability is enforced: `<Nullable>enable</Nullable>` and `nullable` is in `WarningsAsErrors`.
- XML documentation is generated for all projects (`GenerateDocumentationFile=true`).
- Versioning is handled by Nerdbank.GitVersioning.

## Dependencies

Major runtime dependencies (pinned to Silk.NET 2.22.0):

- `Silk.NET.WebGPU` + `Silk.NET.WebGPU.Native.WGPU` + `Silk.NET.WebGPU.Extensions.{WGPU,Dawn,Disposal}`
- `Silk.NET.Windowing` / `Silk.NET.Windowing.Glfw`
- `Silk.NET.Input` / `Silk.NET.Input.Glfw` / `Silk.NET.Input.Sdl`
- `SixLabors.ImageSharp` (3.1.12) for texture loading and pixel manipulation

## Development helpers

- `claude-yolo.sh` — launches `claude` with `--dangerously-skip-permissions` for fast iteration with Claude Code.
- `copilot-yolo.sh` — launches `copilot` with permissive flags and the GitHub MCP server disabled.

These are development-environment conveniences; do not use them in untrusted contexts.

## License

No license file is currently included in the repository.
