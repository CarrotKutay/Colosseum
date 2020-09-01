using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;

[UpdateAfter(typeof(MovementSystem))]
public class PlayerInputTurnSystem : SystemBase
{
    private Entity Player;
    private Unity.Physics.Systems.BuildPhysicsWorld physicsWorldSystem;

    protected override void OnCreate()
    {
        physicsWorldSystem = World
            .DefaultGameObjectInjectionWorld
            .GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
    }

    protected override void OnStartRunning()
    {
        Player = GetSingletonEntity<PlayerPhysicsTag>();
    }

    protected override void OnUpdate()
    {
        var getLookDirectionInput = GetComponentDataFromEntity<LookDirectionInputComponent>(true);

        var handle = Entities.WithName("TurnPlayerTowardsInput")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (ref LocalToWorld LocalToWorld,
                ref AngularVelocityControlComponent angularVelocityControl,
                in PhysicsMass Mass,
                in Rotation RotationData,
                in Translation translation,
                in LookDirectionInputComponent LookDirectionInput,
                in Entity entity) =>
                {
                    #region // * turning on y - axis according to player input

                    // * Turn velocity
                    var angularVelocityStrength = 15f;
                    // * normalized look input direction
                    // * as the vector from player to TurnInput 
                    var lookInput = math.normalizesafe(LookDirectionInput.WorldValue + LocalToWorld.Position);
                    /* UnityEngine.Debug.DrawRay(LocalToWorld.Position, LocalToWorld.Forward, UnityEngine.Color.red);
                    UnityEngine.Debug.DrawRay(LocalToWorld.Position, lookInput, UnityEngine.Color.blue); */
                    var playerForward = math.normalizesafe(LocalToWorld.Forward);

                    var radiansLookInput = math.atan2(lookInput.x, lookInput.z);
                    var radiansPlayerForward = math.atan2(playerForward.x, playerForward.z);

                    /* 
                       * Here we determine the difference between the angle value of the lookInput and
                       * the player forward vector. Furthermore, we alter the difference to always use the
                       * small difference, since we want the player to turn efficiently and as fast as possible
                     */
                    var inputToPlayerDifference = radiansLookInput - radiansPlayerForward;
                    if (math.abs(inputToPlayerDifference) > math.PI)
                    {
                        inputToPlayerDifference = inputToPlayerDifference < 0 ?
                            inputToPlayerDifference + math.PI * 2 :
                            (math.PI * 2 - inputToPlayerDifference) * -1;
                    }

                    /* 
                        * Changing the input strength depending on how big the angle between the forward vector
                        * of the player and the directional vector of the input is.
                        * We use a square root function to determine input strength and multiply it by angularVelocityStrength
                        * to furzher increase velocity for faster movespeed.
                    */

                    inputToPlayerDifference = inputToPlayerDifference > 0 ?
                        math.pow(math.abs(inputToPlayerDifference) / math.PI, .5f) * angularVelocityStrength :
                        math.pow(math.abs(inputToPlayerDifference) / math.PI, .5f) * angularVelocityStrength * -1;

                    // * Changing Angular Velocity to new value
                    angularVelocityControl.y = inputToPlayerDifference; // reset only y-directional angular force

                    #endregion
                }
            )
            .Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, handle);

        World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>()
            .AddJobHandleForProducer(Dependency);
    }
}