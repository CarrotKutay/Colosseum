using Unity.Entities;

[GenerateAuthoringComponent]
public struct MovementSpeedComponent : IComponentData
{
    // a value of 70 seems to be a good basic movement speed
    public int Value;
}
