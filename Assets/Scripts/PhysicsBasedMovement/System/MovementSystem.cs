using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
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

    public struct RaycastJob : IJob
    {
        [ReadOnly] public CollisionWorld world;
        public NativeArray<RaycastHit> results;
        public Entity Player;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> getPlayerLocalToWorld;
        [ReadOnly] public ComponentDataFromEntity<Translation> getPlayerPosition;

        public void Execute()
        {
            // get player data
            var LocalToWorld = getPlayerLocalToWorld[Player];
            var playerForward = LocalToWorld.Forward;
            var playerRight = LocalToWorld.Right;
            var playerPosition = getPlayerPosition[Player].Value;

            var rayDirection = math.normalizesafe(math.cross(playerRight, playerForward));


            BitField32 filter = new BitField32();
            filter.SetBits(1, true, 31);
            filter.SetBits(0, false);


            RaycastInput input = new RaycastInput()
            {
                Start = playerPosition,
                End = playerPosition + rayDirection * 2,
                Filter = new CollisionFilter()
                {
                    BelongsTo = ~0u,
                    CollidesWith = filter.GetBits(0, 32), // all 1s, so all layers, exept layer 0 = player layer
                    GroupIndex = 0
                }
            };

            RaycastHit hit;
            world.CastRay(input, out hit);
            results[0] = hit;
        }
    }

    protected override void OnUpdate()
    {
        float3 maxVelocity = new float3(9.81f, 9.81f, 9.81f); // incorporating terminal velocity (no free fall)

        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;

        var raycastResult = new NativeArray<RaycastHit>(1, Allocator.TempJob);
        var Player = PlayerPhysics;

        var getLocalToWorld = GetComponentDataFromEntity<LocalToWorld>(true);
        var getPosition = GetComponentDataFromEntity<Translation>(true);

        var raycastJob = new RaycastJob()
        {
            getPlayerLocalToWorld = getLocalToWorld,
            getPlayerPosition = getPosition,
            Player = Player,
            world = collisionWorld,
            results = raycastResult,
        };

        var raycastHandle = raycastJob.Schedule(Dependency);
        raycastHandle.Complete();
        /*         Dependency = JobHandle.CombineDependencies(raycastHandle, Dependency);
                endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(raycastHandle); */

        /* var movementJob = new MovePlayerJob
        {
            GetInputHoldComponent = GetComponentDataFromEntity<InputHoldComponent>(true),
            GetMovementDirectionInput = GetComponentDataFromEntity<MovementDirectionInputComponent>(true),
            GetMass = GetComponentDataFromEntity<PhysicsMass>(true),
            GetLocalToWorld = getLocalToWorld,
            GetPhysicsVelocity = GetComponentDataFromEntity<PhysicsVelocity>(),
            maxVelocity = maxVelocity,
            Player = PlayerPhysics,
            raycastHit = raycastResult[0],
        };
        raycastResult.Dispose();

        var movementHandle = movementJob.Schedule(Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, movementHandle); */
        //endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(movementHandle);

        var handle = Entities.WithName("Move_Player")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (int entityInQueryIndex,
                ref PhysicsVelocity physicsVelocity,
                in PhysicsMass mass,
                in MovementDirectionInputComponent movementInput,
                in InputHoldComponent holdDurationInput,
                in LocalToWorld localToWorld,
                in MovementSpeedComponent baseMovementSpeed) =>
                {
                    // * getting raycast results 
                    var groundSurfaceNormal = raycastResult[0].SurfaceNormal;
                    var yDirection = math.normalizesafe(math.cross(groundSurfaceNormal, raycastResult[0].Position + new float3(1, 0, 0))).y;

                    /* // debug
                    UnityEngine.Debug.DrawRay(raycastResult[0].Position, groundNormal * 2, UnityEngine.Color.red);
                    UnityEngine.Debug.DrawRay(raycastResult[0].Position, newForward * 2, UnityEngine.Color.blue);
                    UnityEngine.Debug.DrawRay(raycastResult[0].Position, localToWorld.Forward * 2, UnityEngine.Color.magenta);  */

                    // determining movement direction in regards to y - direction : up- or downhill
                    var slopeFactor = movementInput.NewValue.y >= 0 ? 1f : -1f;

                    // * movement 
                    var moveOrder = math.normalizesafe(new float3(movementInput.NewValue.x, yDirection * slopeFactor, movementInput.NewValue.y));
                    UnityEngine.Debug.DrawRay(localToWorld.Position, moveOrder, UnityEngine.Color.green);

                    if (holdDurationInput.Value.Equals(float3.zero) && moveOrder.Equals(float3.zero))
                    {
                        physicsVelocity.Linear *= new float3(.7f, 1f, .7f); // * for faster stopp when no movement input is given
                    }
                    else
                    {
                        //physicsVelocity.Linear += moveOrder * holdDurationInput.Value;
                        ComponentExtensions.ApplyLinearImpulse(ref physicsVelocity, mass, moveOrder * baseMovementSpeed.Value);
                        physicsVelocity.Linear = math.clamp(physicsVelocity.Linear, -maxVelocity, maxVelocity);
                    }
                }
        )
        .WithDeallocateOnJobCompletion(raycastResult)
        .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, handle);
    }

    public struct MovePlayerJob : IJob
    {
        [ReadOnly] public ComponentDataFromEntity<PhysicsMass> GetMass;
        [ReadOnly] public ComponentDataFromEntity<MovementDirectionInputComponent> GetMovementDirectionInput;
        [ReadOnly] public ComponentDataFromEntity<InputHoldComponent> GetInputHoldComponent;
        [ReadOnly] public ComponentDataFromEntity<LocalToWorld> GetLocalToWorld;
        [ReadOnly] public RaycastHit raycastHit;
        [ReadOnly] public float3 maxVelocity;
        public ComponentDataFromEntity<PhysicsVelocity> GetPhysicsVelocity;
        public Entity Player;
        public void Execute()
        {
            var movementInput = GetMovementDirectionInput[Player];
            var holdDurationInput = GetInputHoldComponent[Player];
            var physicsVelocity = GetPhysicsVelocity[Player];
            var localToWorld = GetLocalToWorld[Player];
            var mass = GetMass[Player];

            // * getting raycast results 
            var groundSurfaceNormal = raycastHit.SurfaceNormal;
            var yDirection = math.normalizesafe(math.cross(groundSurfaceNormal, raycastHit.Position + new float3(1, 0, 0))).y;

            /* // debug
            UnityEngine.Debug.DrawRay(raycastResult[0].Position, groundNormal * 2, UnityEngine.Color.red);
            UnityEngine.Debug.DrawRay(raycastResult[0].Position, newForward * 2, UnityEngine.Color.blue);
            UnityEngine.Debug.DrawRay(raycastResult[0].Position, localToWorld.Forward * 2, UnityEngine.Color.magenta); */

            // determining movement direction in regards to y - direction : up- or downhill
            var slopeFactor = movementInput.NewValue.y >= 0 ? 1f : -1f;

            // * movement 
            var moveOrder = math.normalizesafe(new float3(movementInput.NewValue.x, yDirection * slopeFactor, movementInput.NewValue.y));
            UnityEngine.Debug.DrawRay(localToWorld.Position, moveOrder, UnityEngine.Color.green);

            if (holdDurationInput.Value.Equals(float3.zero) && moveOrder.Equals(float3.zero))
            {
                physicsVelocity.Linear *= 1f; // * for faster stopp when no movement input is given
            }
            else
            {
                //physicsVelocity.Linear += moveOrder * holdDurationInput.Value;
                ComponentExtensions.ApplyLinearImpulse(ref physicsVelocity, mass, moveOrder * 3);
                physicsVelocity.Linear = math.clamp(physicsVelocity.Linear, -maxVelocity, maxVelocity);
            }
        }
    }
}
