using Unity.Entities;

[GenerateAuthoringComponent]
public struct MovementState : IComponentData
{
    public TransformState Value;
}

/// <summary>
/// Transform state does refer to the current entities movement / transform state.
/// 
/// <para> <see cref="Grounded"/> refers to an entity touching the ground. </para>
/// <para> <see cref="StartJumping"/> refers to an entity about to start jumping.</para>
/// <para> <see cref="InAir"/> refers to an entity not touching the ground. Any entity 
/// with state <see cref="InAir"/> has only one available jump left.</para>
/// <para> <see cref="Landing"/> refers to an entity which has no jumps left and is not touching the ground.</para>
/// </summary>
public enum TransformState
{
    Grounded, StartJumping, InAir, Landing
}
