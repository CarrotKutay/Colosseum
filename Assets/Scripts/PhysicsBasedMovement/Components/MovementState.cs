using Unity.Entities;

[GenerateAuthoringComponent]
public struct MovementState : IComponentData
{
    public TransformState Value;
}

public enum TransformState
{
    Grounded, InAir
}
