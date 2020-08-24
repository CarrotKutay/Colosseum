using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Jobs;
using Unity.Collections;

public class FollowEntity : MonoBehaviour
{
    public Entity entityToFollow;
    private EntityManager manager;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    private void Awake()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    private void LateUpdate()
    {
        entityToFollow = endSimulationEntityCommandBufferSystem.GetSingletonEntity<CameraTag>();
        if (entityToFollow == Entity.Null) { Debug.Log("entity to follow is null (not given)"); return; }
        else
        {
            var LocalToWorld = manager.GetComponentData<LocalToWorld>(entityToFollow);
            var camera = manager.GetComponentData<CameraTag>(entityToFollow);
            if (math.isfinite(LocalToWorld.Position.x) && math.isfinite(LocalToWorld.Position.y) && math.isfinite(LocalToWorld.Position.z))
            {
                transform.position = LocalToWorld.Position;
                transform.LookAt(LocalToWorld.Position + LocalToWorld.Forward);
            }

        }
    }


}
