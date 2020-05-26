using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics;
using UnityEngine;

public class MovementSystem : SystemBase
{
    private InputSystem InputSystem;
    public EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

        InputSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<InputSystem>();
    }

    protected override void OnUpdate()
    {
        var Player = GetSingletonEntity<PlayerTag>();
        var getHoldInputDuration = GetComponentDataFromEntity<InputHoldComponent>(true);
        var getNextMovementInput = GetComponentDataFromEntity<MovementDirectionInputComponent>(true);
        float3 maxVelocity = new float3(9.81f, 9.81f, 9.81f); // incorporating terminal velocity (no free fall)

        Entities.WithName("Move_Player")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (int entityInQueryIndex, ref PhysicsVelocity physicsVelocity) =>
                {
                    var movementInput = getNextMovementInput[Player];
                    var moveOrder = new float3(movementInput.NewValue.x, 0, movementInput.NewValue.y);
                    var holdDuration = getHoldInputDuration[Player];

                    if (holdDuration.Value.Equals(float3.zero) && moveOrder.Equals(float3.zero))
                    {
                        physicsVelocity.Linear *= .9f; // * for faster stopps when no movement input is given
                    }
                    else
                    {
                        physicsVelocity.Linear += moveOrder * holdDuration.Value;
                        physicsVelocity.Linear = math.clamp(physicsVelocity.Linear, -maxVelocity, maxVelocity);
                    }
                }
        )
        .WithReadOnly(getNextMovementInput)
        .WithReadOnly(getHoldInputDuration)
        .Schedule();
    }
}
