using System;
using Engineer.Graphics;

namespace Engineer.Tests
{
    public sealed class EngineTests
    {
        public static void Run()
        {
            var box = new MeshNode(
                Guid.NewGuid().ToString(),
                Geometry.Box(new BoxProps
                {
                    Width = 10,
                    Depth = 10,
                    Height = 10,
                }));

            var engine = new Engine();
            engine.Run();
        }
    }
}
