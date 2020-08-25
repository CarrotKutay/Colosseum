using Unity.Entities;

[GenerateAuthoringComponent]
public struct MovementJumpComponent : IComponentData
{
    public bool JumpTrigger;
    public bool SecondJump;
    public bool FirstJump;
}
