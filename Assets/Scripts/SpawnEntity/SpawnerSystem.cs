using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

public class SpawnerSystem : SystemBase
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
        var ecb = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        //var entities = GetEntityQuery(typeof(SpawnComponent)).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getSpawnEntities);

        var handle = Entities.WithName("SpawnEntities")
            .WithAll<SpawnComponent>()
            .ForEach(
                (int entityInQueryIndex, in Entity entity) =>
                {
                    var EntityToSpawn = GetComponent<SpawnComponent>(entity).Value;
                    ecb.Instantiate(entityInQueryIndex, EntityToSpawn);

                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
            ).ScheduleParallel(Dependency);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(handle);
    }
}
