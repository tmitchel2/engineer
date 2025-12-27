using System.Collections.Generic;
using System.Numerics;

namespace Engineer.Graphics
{
    public sealed record Geometry(
        PrimitiveType PrimitiveType,
        List<Vector3> Vertices,
        List<Vector3> Normals)
    {
        public static void DrawRectangle(
            Plane plane,
            Vector3 size,
            int gridX,
            int gridY,
            Geometry geometry)
        {
            var segmentWidth = size.X / (double)gridX;
            var segmentHeight = size.Y / (double)gridY;
            var widthHalf = size.X / 2d;
            var heightHalf = size.Y / 2d;
            var depthHalf = size.Z / 2d;
            var gridX1 = gridX + 1;
            var gridY1 = gridY + 1;
            
            var numberOfVertices = geometry.Vertices.Count;
            
            for (var iy = 0; iy < gridY1; iy++)
            {
                var y = iy * segmentHeight - heightHalf;
                for (var ix = 0; ix < gridX1; ix++)
                {
                    var x = ix * segmentWidth - widthHalf;
                    
                    var vertex = new Vector3();
                    vertex = SetComponent(vertex, plane.U, (float)(x * plane.UDir));
                    vertex = SetComponent(vertex, plane.V, (float)y);
                    vertex = SetComponent(vertex, plane.W, (float)depthHalf);
                    geometry.Vertices.Add(vertex);

                    var normal = new Vector3();
                    normal = SetComponent(normal, plane.U, 0);
                    normal = SetComponent(normal, plane.V, 0);
                    normal = SetComponent(normal, plane.W, depthHalf > 0 ? 1 : -1);
                    geometry.Normals.Add(normal);
                }
            }

            for (var iy = 0; iy < gridY; iy++)
            {
                for (var ix = 0; ix < gridX; ix++)
                {
                    var a = numberOfVertices + ix + gridX1 * iy;
                    var b = numberOfVertices + ix + gridX1 * (iy + 1);
                    var c = numberOfVertices + (ix + 1) + gridX1 * (iy + 1);
                    var d = numberOfVertices + (ix + 1) + gridX1 * iy;
                    
                    // Two triangles per quad
                    geometry.Vertices.Add(geometry.Vertices[a]);
                    geometry.Vertices.Add(geometry.Vertices[b]);
                    geometry.Vertices.Add(geometry.Vertices[d]);
                    
                    geometry.Vertices.Add(geometry.Vertices[b]);
                    geometry.Vertices.Add(geometry.Vertices[c]);
                    geometry.Vertices.Add(geometry.Vertices[d]);
                }
            }
        }
        
        private static Vector3 SetComponent(Vector3 vector, int component, float value)
        {
            return component switch
            {
                0 => new Vector3(value, vector.Y, vector.Z),
                1 => new Vector3(vector.X, value, vector.Z),
                2 => new Vector3(vector.X, vector.Y, value),
                _ => vector
            };
        }

        public static Geometry Box(BoxProps props)
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new List<Vector3>(),
                new List<Vector3>());

            // Right face (+X)
            DrawRectangle(
                new Plane(2, 1, 0, -1),
                new Vector3(props.Depth, props.Height, props.Width),
                1, 1,
                geometry);

            // Left face (-X)
            DrawRectangle(
                new Plane(2, 1, 0, 1),
                new Vector3(props.Depth, props.Height, -props.Width),
                1, 1,
                geometry);

            // Top face (+Y)
            DrawRectangle(
                new Plane(0, 2, 1, 1),
                new Vector3(props.Width, props.Depth, props.Height),
                1, 1,
                geometry);

            // Bottom face (-Y)
            DrawRectangle(
                new Plane(0, 2, 1, 1),
                new Vector3(props.Width, props.Depth, -props.Height),
                1, 1,
                geometry);

            // Front face (+Z)
            DrawRectangle(
                new Plane(0, 1, 2, 1),
                new Vector3(props.Width, props.Height, props.Depth),
                1, 1,
                geometry);

            // Back face (-Z)
            DrawRectangle(
                new Plane(0, 1, 2, -1),
                new Vector3(props.Width, props.Height, -props.Depth),
                1, 1,
                geometry);

            return geometry;
        }
    }
}
