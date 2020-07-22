using Unity.Entities;
using Unity.Mathematics;
public struct BufferCollisionEventElement : IBufferElementData
{
    public Entity Entity;
    public float3 Normal;
    public bool HasCollisionDetails;
    public float3 AverageContactPointPosition;
    public float EstimatedImpulse;
    public PhysicsCollisionEventState State;
    public bool isStale;
}

public struct CollisionEventsReceiverProperties : IComponentData
{
    public bool UsesCollisionDetails;
}
