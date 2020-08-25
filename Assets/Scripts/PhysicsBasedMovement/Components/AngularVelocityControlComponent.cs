using Unity.Entities;

[GenerateAuthoringComponent]
public struct AngularVelocityControlComponent : IComponentData
{
    public float DegreesFromYAxisToXAxis;
    public float DegreesFromYAxisToZAxis;
    public bool ActiveOnXY;
    public bool ActiveOnZY;
}