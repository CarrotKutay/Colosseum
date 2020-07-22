using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;

public class FollowEntity : MonoBehaviour
{
    public Entity entityToFollow;
    private EntityManager manager;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    [SerializeField] private float3 offset = new float3(0, 0, 0);

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

        Translation entityToFollowPosition = manager.GetComponentData<Translation>(entityToFollow);
        transform.position = offset + entityToFollowPosition.Value;
    }
}
