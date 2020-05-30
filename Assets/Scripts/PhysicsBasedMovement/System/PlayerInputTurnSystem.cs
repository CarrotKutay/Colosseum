using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Extensions;

[UpdateAfter(typeof(MovementSystem))]
public class PlayerInputTurnSystem : SystemBase
{

    protected override void OnCreate()
    {
    }
    protected override void OnUpdate()
    {
        /* var PlayerPhysics = GetSingletonEntity<PlayerPhysicsTag>();
        var getPlayerPosition = GetComponentDataFromEntity<Translation>(true); */

        Entities.WithName("TurnPlayerTowardsInput")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (ref PhysicsVelocity Veclocity,
                ref LocalToWorld LocalToWorld,
                in PhysicsMass Mass,
                in LookDirectionInputComponent LookDirectionInput,
                in Rotation RotationData) =>
                {
                    // * Turn velocity
                    var angularVelocityStrength = 15f;
                    // * normalized look input direction
                    var lookInput = math.normalizesafe(new float3(LookDirectionInput.Value.x, 0, LookDirectionInput.Value.y));
                    // * normalized forward vector of player
                    var playerForward = math.normalizesafe(LocalToWorld.Forward);
                    //var debugPlayerPosition = getPlayerPosition[PlayerPhysics].Value;

                    var radiansLookInput = math.atan2(lookInput.x, lookInput.z);
                    var radiansPlayerForward = math.atan2(playerForward.x, playerForward.z);

                    /* 
                       * Here we determine the difference between the angle value of the lookInput and
                       * the player forward vector. Furthermore, we alter the difference to always use the
                       * small difference, since we want the player to turn efficiently
                     */
                    var inputToPlayerDifference = radiansLookInput - radiansPlayerForward;
                    if (math.abs(inputToPlayerDifference) > math.PI)
                    {
                        if (inputToPlayerDifference < 0)
                        {
                            inputToPlayerDifference = (inputToPlayerDifference + math.PI * 2);

                        }
                        else if (inputToPlayerDifference > 0)
                        {
                            inputToPlayerDifference = (math.PI * 2 - inputToPlayerDifference) * -1;
                        }
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
                    ComponentExtensions.SetAngularVelocity(ref Veclocity, Mass, RotationData, new float3(0, inputToPlayerDifference, 0));
                    //Debug.DrawRay(debugPlayerPosition, playerForward, Color.red, .2f);
                }
            )
            //.WithReadOnly(getPlayerPosition)
            .Schedule();


    }


}