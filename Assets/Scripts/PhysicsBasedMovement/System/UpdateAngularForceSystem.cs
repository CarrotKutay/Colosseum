using Unity.Entities;
using Unity.Physics;
using Unity.Jobs;
using Unity.Mathematics;

public class UpdateAngularForceSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        var resetThreshold = math.radians(70f);
        var updateAngularForceHandle = Entities.WithName("UpdateAngularForce")
            .WithNone<Prefab>()
            .ForEach(
                (
                    ref PhysicsVelocity velocity,
                    ref AngularVelocityControlComponent angularControlComponent
                ) =>
                {
                    velocity.Angular.y = angularControlComponent.resetXAxis || angularControlComponent.resetXAxis ? velocity.Angular.y : angularControlComponent.y;

                    // * get direction multiplier for x-/z-axis control
                    var multiplierX = angularControlComponent.directionX ? -1 : 1;
                    var multiplierZ = angularControlComponent.directionZ ? -1 : 1;
                    // * threshold reached on x - axis
                    velocity.Angular.x = angularControlComponent.resetXAxis ?
                        angularControlComponent.x * multiplierX : velocity.Angular.x;
                    // * threshold reached on z - axis
                    velocity.Angular.z = angularControlComponent.resetZAxis ?
                        angularControlComponent.z * multiplierZ : velocity.Angular.z;
                }
            ).ScheduleParallel(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, updateAngularForceHandle);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}