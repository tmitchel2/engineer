using System.Numerics;

namespace Engineer.Graphics
{
    public record Node(
        string Id,
        Vector3 Translation = default,
        Quaternion Rotation = default,
        Vector3 Scale = default);
}