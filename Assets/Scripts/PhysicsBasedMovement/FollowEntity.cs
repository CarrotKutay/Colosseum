using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;

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

        var entityLocalToWorld = manager.GetComponentData<LocalToWorld>(entityToFollow);

        var fwd = math.normalizesafe(entityLocalToWorld.Forward);
        var pos = entityLocalToWorld.Position;

        transform.position = pos - (fwd * orbitMultiplier) + heightOffset;
        transform.LookAt(pos + fwd);

    }
}
