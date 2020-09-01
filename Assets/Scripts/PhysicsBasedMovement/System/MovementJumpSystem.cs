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
        var EntityCommandBuffer = endSimulationEntityCommandBuffer
            .CreateCommandBuffer().AsParallelWriter();

        var getMovementSpeed = GetComponentDataFromEntity<MovementSpeedComponent>(true);
        var getTranslation = GetComponentDataFromEntity<Translation>(true);

        var systemJobHandle = Entities.WithName("PerformJumping")
            .WithNone<Prefab>()
            .WithAll<PlayerPhysicsTag>()
            .ForEach(
                (
                    int entityInQueryIndex,
                    ref Entity entity,
                    ref PhysicsVelocity velocity,
                    ref PhysicsMass mass,
                    ref MovementJumpComponent jumpComponent,
                    ref MovementState movementState,
                    in PhysicsCollider collider,
                    in Rotation rotation
                ) =>
                {
                    var movementSpeed = getMovementSpeed[entity];
                    var translation = getTranslation[entity];

                    // * check if either entity is able to perfrom first jump or already in state of performing a jump but can still perfrom a second jump
                    var jumpPossibleOnGround = jumpComponent.FirstJump && jumpComponent.JumpTrigger;
                    var jumpPossibleInAir = jumpComponent.SecondJump && jumpComponent.JumpTrigger;
                    jumpComponent.JumpTrigger = false;

                    if ((movementState.Value == TransformState.StartJumping && jumpPossibleOnGround)
                        || (movementState.Value == TransformState.InAir && jumpPossibleInAir))
                    {
                        var jumpForceRegulator = jumpComponent.FirstJump ? movementSpeed.Value * .055f : movementSpeed.Value * .075f;
                        // * perform jump as an explosive force applied to entity relative to its movement speed
                        var explosiveJumpForce = (float)movementSpeed.Value / jumpForceRegulator;
                        var up = new float3(0, 1f, 0);
                        var explosiveForcePosition = ComponentExtensions.GetCenterOfMassWorldSpace(ref mass, in translation, in rotation);

                        ComponentExtensions.ApplyExplosionForce(ref velocity,
                            in mass,
                            in collider,
                            in translation,
                            in rotation,
                            explosiveJumpForce,
                            (explosiveForcePosition - up),
                            1f,
                            1f,
                            up,
                            0,
                            ForceMode.VelocityChange);

                        jumpComponent.SecondJump = jumpComponent.FirstJump ? true : false;
                        jumpComponent.FirstJump = false;
                    }
                })
                .WithReadOnly(getMovementSpeed)
                .WithReadOnly(getTranslation)
                .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, systemJobHandle);
        endSimulationEntityCommandBuffer.AddJobHandleForProducer(Dependency);

    }
}
