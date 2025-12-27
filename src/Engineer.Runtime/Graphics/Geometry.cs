using System;
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
            // var vertices = new Vector3[gridY1 * gridX1];
            // var normals = new Vector3[gridY1 * gridX1];
            for (var iy = 0; iy < gridY1; iy++)
            {
                var y = iy * segmentHeight - heightHalf;
                for (var ix = 0; ix < gridX1; ix++)
                {
                    // var x = ix * segmentWidth - widthHalf;
                    // vector.setComponent(u, x * udir);
                    // vector.setComponent(v, y * vdir);
                    // vector.setComponent(w, depthHalf);
                    geometry.Vertices.Add(new Vector3());

                    // vector.setComponent(u, 0);
                    // vector.setComponent(v, 0);
                    // vector.setComponent(w, depth > 0 ? 1 : -1);
                    geometry.Normals.Add(new Vector3());

                    // uvs.push(ix / gridX, 1 - iy / gridY);
                }
            }

            throw new NotImplementedException();
            // for (var iy = 0; iy < gridY; iy++)
            // {
            //     for (var ix = 0; ix < gridX; ix++)
            //     {
            //         var a = numberOfVertices + ix + gridX1 * iy;
            //         var b = numberOfVertices + ix + gridX1 * (iy + 1);
            //         var c = numberOfVertices + (ix + 1) + gridX1 * (iy + 1);
            //         var d = numberOfVertices + (ix + 1) + gridX1 * iy;
            //         indices.push(a, b, d, b, c, d);
            //     }
            // }
            // numberOfVertices += 4;
        }

        public static Geometry Box(BoxProps props)
        {
            var geometry = new Geometry(
                PrimitiveType.Triangles,
                new List<Vector3>(),
                new List<Vector3>());

            DrawRectangle(
                new Plane(2, 1, 0, 0),
                new Vector3(props.Depth, props.Height, props.Width),
                0, 0,
                geometry);

            DrawRectangle(
                new Plane(2, 1, 0, 0),
                new Vector3(props.Depth, props.Height, -props.Width),
                0, 0,
                geometry);

            DrawRectangle(
                new Plane(0, 2, 1, 0),
                new Vector3(props.Width, props.Depth, props.Height),
                0, 0,
                geometry);

            return geometry;
        }
    }
}
