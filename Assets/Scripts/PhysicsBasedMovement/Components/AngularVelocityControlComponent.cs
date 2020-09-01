using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct AngularVelocityControlComponent : IComponentData
{
    public bool directionX;
    public bool directionZ;
    public bool resetZAxis;
    public bool resetXAxis;
    public bool hasSurface;
    public float x;
    public float y;
    public float z;
    public float3 getForce()
    {
        return new float3(x, y, z);
    }
}