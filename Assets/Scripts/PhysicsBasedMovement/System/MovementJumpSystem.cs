using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Extensions;
[UpdateAfter(typeof(TransformStateSystem))]
public class MovementJumpSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;

    protected override void OnStartRunning()
    {
        endSimulationEntityCommandBuffer = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var systemJobHandle = Entities.WithName("PerformJumping")
            .WithNone<Prefab>()
            .WithAll<PlayerPhysicsTag>()
            .ForEach(
                (
                    ref Entity entity,
                    ref PhysicsVelocity velocity,
                    ref MovementJumpComponent jumpComponent,
                    ref MovementState movementState,
                    in MovementSpeedComponent movementSpeed
                ) =>
                {

                    // * check if either entity is able to perfrom first jump or already in state of performing a jump but can still perfrom a second jump
                    var jumpPossibleOnGround = jumpComponent.FirstJump && jumpComponent.JumpTrigger;
                    var jumpPossibleInAir = jumpComponent.SecondJump && jumpComponent.JumpTrigger;
                    jumpComponent.JumpTrigger = false;

                    if ((movementState.Value == TransformState.StartJumping && jumpPossibleOnGround)
                        || (movementState.Value == TransformState.InAir && jumpPossibleInAir))
                    {
                        var jumpForceRegulator = jumpComponent.FirstJump ? movementSpeed.Value * .15f : movementSpeed.Value * .3f;
                        // * perform jump as an explosive force applied to entity relative to its movement speed
                        var explosiveJumpForce = (float)movementSpeed.Value / jumpForceRegulator;
                        velocity.Linear.y = explosiveJumpForce;

                        jumpComponent.SecondJump = jumpComponent.FirstJump ? true : false;
                        jumpComponent.FirstJump = false;
                    }
                }).ScheduleParallel(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, systemJobHandle);
        endSimulationEntityCommandBuffer.AddJobHandleForProducer(Dependency);

    }
}
