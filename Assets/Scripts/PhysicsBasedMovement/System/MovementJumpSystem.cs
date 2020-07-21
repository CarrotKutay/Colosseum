using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Collections;
[UpdateAfter(typeof(TransformStateSystem))]
public class MovementJumpSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;

    protected override void OnStartRunning()
    {
        endSimulationEntityCommandBuffer = World
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var EntityCommandBuffer = endSimulationEntityCommandBuffer
            .CreateCommandBuffer().ToConcurrent();

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

    }

    public struct PerformJumpMovement : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Entities;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        public ComponentDataFromEntity<MovementJumpComponent> getJumpComponent;
        public ComponentDataFromEntity<PhysicsVelocity> getVelocity;
        public ComponentDataFromEntity<PhysicsMass> getMass;
        [ReadOnly] public ComponentDataFromEntity<PhysicsCollider> getCollider;
        [ReadOnly] public ComponentDataFromEntity<Translation> getTranslation;
        //[ReadOnly] public ComponentDataFromEntity<LocalToWorld> getLocalToWorld;
        [ReadOnly] public ComponentDataFromEntity<Rotation> getRotation;
        [ReadOnly] public ComponentDataFromEntity<MovementSpeedComponent> getMovementSpeed;
        [ReadOnly] public ComponentDataFromEntity<MovementState> getMovementState;
        public void Execute()
        {

            foreach (var entity in Entities)
            {
                // * get actor state and jumping components
                var movementSpeed = getMovementSpeed[entity];
                var movementState = getMovementState[entity];
                var jumpComponent = getJumpComponent[entity];

                // * get actor physiscs and transform components
                var velocity = getVelocity[entity];
                var mass = getMass[entity];
                var collider = getCollider[entity];
                var translation = getTranslation[entity];
                var rotation = getRotation[entity];

                // * check if either entity is able to perfrom first jump or already in state of performing a jump but can still perfrom a second jump
                if ((movementState.Value == TransformState.Grounded && jumpComponent.FirstJump) || (movementState.Value == TransformState.InAir && jumpComponent.SecondJump))
                {
                    var jumpForceRegulator = jumpComponent.FirstJump ? movementSpeed.Value * .03f : movementSpeed.Value * .05f;
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
                        (translation.Value - up),
                        1f,
                        1f,
                        up,
                        0,
                        ForceMode.VelocityChange);

                    movementState.Value = jumpComponent.FirstJump ? TransformState.InAir : TransformState.Landing;
                    jumpComponent.FirstJump = false;
                    jumpComponent.SecondJump = false;
                }
            }
        }
    }
}
