using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
[UpdateBefore(typeof(MovementJumpSystem))]
public class TransformStateSystem : SystemBase
{

    public EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBuffer = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var writeJobHandle = Entities.WithName("UpdateEntityMovementState")
            .WithNone<Prefab>()
            .ForEach(
                (
                    int entityInQueryIndex,
                    ref Entity entity,
                    ref MovementState movementState,
                    ref MovementJumpComponent jumpInputComponent,
                    ref DynamicBuffer<BufferCollisionEventElement> collisionBuffer
                ) =>
                {

                    if (jumpInputComponent.JumpTrigger && movementState.Value == TransformState.Grounded)
                    {
                        movementState.Value = TransformState.StartJumping;
                    }
                    // * one jump is still available and entity is in air
                    // * -> update state to InAir
                    else if (collisionBuffer.Length <= 0 && !jumpInputComponent.FirstJump && jumpInputComponent.SecondJump)
                    {
                        movementState.Value = TransformState.InAir;
                    }
                    // * no jumps are available and the entity is not touching / colliding with
                    // * any other objects -> update state to Landing
                    else if (collisionBuffer.Length <= 0 && !jumpInputComponent.FirstJump && !jumpInputComponent.SecondJump)
                    {
                        movementState.Value = TransformState.Landing;
                    }
                    // * if entity can jump and is currently not about to start jumping 
                    // * and is colliding with another object
                    // * -> update state to grounded
                    else if (collisionBuffer.Length > 0 && movementState.Value != TransformState.StartJumping)
                    {
                        movementState.Value = TransformState.Grounded;
                        jumpInputComponent.FirstJump = true;
                        jumpInputComponent.SecondJump = true;
                    }
                }
            ).Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, writeJobHandle);
        endSimulationEntityCommandBuffer.AddJobHandleForProducer(Dependency);

    }
}