using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;

public class FollowEntity : MonoBehaviour
{
    public Entity entityToFollow;
    private EntityManager manager;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    [SerializeField] private float3 heightOffset = new float3(0, 0, 0);
    [SerializeField] private float3 orbitMultiplier = new float3(4f, 2f, 4f);

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    private void LateUpdate()
    {
        entityToFollow = endSimulationEntityCommandBufferSystem.GetSingletonEntity<PlayerPhysicsTag>();
        if (entityToFollow == Entity.Null) { Debug.Log("entity is null"); return; }

        var entityLookDirection = manager.GetComponentData<LookDirectionInputComponent>(entityToFollow);
        var entityTranslation = manager.GetComponentData<Translation>(entityToFollow);
        Debug.DrawLine(entityTranslation.Value, entityLookDirection.Value, Color.red);
        transform.position = entityTranslation.Value - (math.normalizesafe(entityLookDirection.Value) * orbitMultiplier) + heightOffset;
        transform.LookAt(entityLookDirection.Value);
    }
}
