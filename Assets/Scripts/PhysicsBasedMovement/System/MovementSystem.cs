using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Physics;
using UnityEngine;

public class MovementSystem : SystemBase
{
    public EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        float3 maxVelocity = new float3(9.81f, 9.81f, 9.81f); // incorporating terminal velocity (no free fall)

        Entities.WithName("Move_Player")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (ref PhysicsVelocity physicsVelocity, in MovementDirectionInputComponent movementInput, in InputHoldComponent holdDurationInput) =>
                {
                    var moveOrder = new float3(movementInput.NewValue.x, 0, movementInput.NewValue.y);

                    if (holdDurationInput.Value.Equals(float3.zero) && moveOrder.Equals(float3.zero))
                    {
                        physicsVelocity.Linear *= .9f; // * for faster stopp when no movement input is given
                    }
                    else
                    {
                        physicsVelocity.Linear += moveOrder * holdDurationInput.Value;
                        physicsVelocity.Linear = math.clamp(physicsVelocity.Linear, -maxVelocity, maxVelocity);
                    }
                }
        ).Schedule();
    }
}
