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
        var Player = PlayerPhysics;

        var getLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true);
        var raycastResult = new NativeArray<RaycastHit>(1, Allocator.TempJob);
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
        var camera = GetSingletonEntity<CameraComponent>(); // ? what if there are more then 1 camera necessary -> needs better solution
        var camera_localToWorld = GetComponent<LocalToWorld>(camera);
        var cameraRight = new float3(camera_localToWorld.Right.x, 0, camera_localToWorld.Right.z);
        var cameraForward = new float3(camera_localToWorld.Forward.x, 0, camera_localToWorld.Forward.z);

        var handle = Entities.WithName("Move_Player")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (
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
                    var playerIsMoving = !movementInput.NewValue.Equals(float2.zero);

                    float3 directionForce = bufferLength > 0 && playerIsMoving ? // * check for current collision count and movement on entity
                                                                                 // * if collisions are happening, possible y force can be applied
                                                                                 // * if no collisions are found -> 'free fall' no yDirectionForce needed
                        math.normalizesafe(math.cross(raycastResult[0].SurfaceNormal, raycastResult[0].Position)) :
                        float3.zero;

                    // * determining movement direction in regards to y - direction : up- or downhill
                    var slopeMovement = (localToWorld.Forward.y > 0 && movementInput.NewValue.y < 0)
                                    || (localToWorld.Forward.y < 0 && movementInput.NewValue.y > 0) ?
                         -directionForce.y : directionForce.y;

                    // * movement should always be in according to the look-at directon of the camera entity
                    // * therefore, the look-at direction is needed
                    // * the move order is the normalized movement input given by the player
                    // * lastly the move order can be devised by rotating the order according to the same rotation the camera does
                    // * for the rotation the y rotation is ignored as the move input for moving forwards and backwards (on z-axis)
                    // * should never be turned to moving the player up or down on the y-axis
                    var moveOrder = math.normalizesafe(new float3(movementInput.NewValue.x, slopeMovement, movementInput.NewValue.y));
                    moveOrder = moveOrder.x * cameraRight + moveOrder.y * camera_localToWorld.Up + moveOrder.z * cameraForward;

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
        .WithDisposeOnCompletion(raycastResult)
        .WithReadOnly(getCollisionBuffer)
        .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, handle);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}
