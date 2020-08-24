using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
[UpdateAfter(typeof(InputSystem))]
public class MovementSystem : SystemBase
{
    public EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private Unity.Physics.Systems.BuildPhysicsWorld physicsWorldSystem;
    private Entity PlayerPhysics;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        physicsWorldSystem = World
            .DefaultGameObjectInjectionWorld
            .GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
    }

    protected override void OnStartRunning()
    {
        PlayerPhysics = GetSingletonEntity<PlayerPhysicsTag>();
    }

    protected override void OnUpdate()
    {
        float maxMoveVelocity = 9.81f; // incorporating terminal velocity (no free fall)

        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

        var raycastResult = new NativeArray<RaycastHit>(1, Allocator.TempJob);
        var Player = PlayerPhysics;

        var getLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true);

        var raycastJob = new RaycastJob()
        {
            getPlayerLocalToWorld = getLocalToWorld,
            Entity = Player,
            world = collisionWorld,
            results = raycastResult,
        };

        var raycastHandle = raycastJob.Schedule(Dependency);
        Dependency = JobHandle.CombineDependencies(raycastHandle, Dependency);

        var getCollisionBuffer = GetBufferFromEntity<BufferCollisionEventElement>(true);
        var getLookDirectionInput = GetComponentDataFromEntity<LookDirectionInputComponent>(true);

        var handle = Entities.WithName("Move_Player")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (
                    int entityInQueryIndex,
                    ref PhysicsVelocity physicsVelocity,
                    in Entity entity,
                    in PhysicsMass mass,
                    in MovementDirectionInputComponent movementInput,
                    in InputHoldComponent holdDurationInput,
                    in LocalToWorld localToWorld,
                    in MovementSpeedComponent baseMovementSpeed
                ) =>
                {
                    var buffer = getCollisionBuffer.HasComponent(entity) ? getCollisionBuffer[entity] : new DynamicBuffer<BufferCollisionEventElement>();
                    var bufferLength = buffer.Length;
                    var isStill = movementInput.NewValue.Equals(float2.zero);

                    float3 directionForce = bufferLength > 0 && !isStill ? // * check for current collision count and movement on entity
                                                                           // * if collisions and movement are happening, possible y force can be applied
                                                                           // * if no collisions or movement are found -> 'free fall' no yDirectionForce needed
                        math.normalizesafe(math.cross(raycastResult[0].SurfaceNormal, raycastResult[0].Position)) :
                        float3.zero;

                    // * determining movement direction in regards to y - direction : up- or downhill
                    var slopeMovement = (localToWorld.Forward.y > 0 && movementInput.NewValue.y < 0)
                                    || (localToWorld.Forward.y < 0 && movementInput.NewValue.y > 0) ?
                         -directionForce.y : directionForce.y;

                    // * movement 
                    // * movement should always be in according to the look-at directon of the player-entity
                    // * therefore, the look-at direction is needed
                    // * binarizing said direction and multipling it with the move order will express 
                    // * the move order in relatioin to the look-at direction of the player-entity
                    var lookDirectionInput = getLookDirectionInput[entity].WorldValue;
                    var moveOrder = math.normalizesafe(new float3(movementInput.NewValue.x, slopeMovement, movementInput.NewValue.y));
                    moveOrder = math.rotate(localToWorld.Rotation, moveOrder);

                    // * debug raycast
                    /* UnityEngine.Debug.DrawRay(localToWorld.Position, moveOrder, UnityEngine.Color.green);
                    UnityEngine.Debug.DrawRay(localToWorld.Position, directionForce, UnityEngine.Color.red);
                    UnityEngine.Debug.DrawRay(localToWorld.Position, localToWorld.Forward, UnityEngine.Color.blue); */

                    if (holdDurationInput.Value.Equals(float3.zero) && moveOrder.Equals(float3.zero))
                    {
                        // * for faster stop when no movement input is given
                        // * no restriction on y-directional force is given, as gravitational forces should be applied at 100%
                        // ? might be possible to forgo any kind of hardfix on linear velocity, due to use of specific
                        // ? physics material whith own values handling friction
                        physicsVelocity.Linear *= bufferLength > 0 ? new float3(.7f, .7f, .7f) : new float3(1, 1, 1);
                    }
                    else
                    {
                        ComponentExtensions.ApplyLinearImpulse(ref physicsVelocity, mass, moveOrder * baseMovementSpeed.Value);
                        physicsVelocity.Linear.y = math.clamp(physicsVelocity.Linear.y, -maxMoveVelocity, maxMoveVelocity);
                    }
                }
        )
        .WithReadOnly(getLookDirectionInput)
        .WithReadOnly(getCollisionBuffer)
        .WithDisposeOnCompletion(raycastResult)
        .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, handle);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
