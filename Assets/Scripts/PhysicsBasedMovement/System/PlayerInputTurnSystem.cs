using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Extensions;

public class PlayerInputTurnSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithName("TurnPlayerTowardsInput")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (ref PhysicsVelocity Veclocity, in PhysicsMass Mass) =>
                {
                    //ComponentExtensions.ApplyAngularImpulse(ref Veclocity, Mass, float3.zero);
                }
            ).Schedule();
    }
}