using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

[DisableAutoCreation]
public class RaycastPhysics : SystemBase
{

    #region On Create / Destroy
    protected override void OnCreate()
    {
    }

    protected override void OnDestroy()
    {
    }
    #endregion

    public struct RaycastJob : IJobParallelFor
    {
        [ReadOnly] public CollisionWorld world;
        [ReadOnly] [DeallocateOnJobCompletion] public NativeArray<RaycastInput> inputs;
        public NativeArray<RaycastHit> results;

        public void Execute(int index)
        {
            RaycastHit hit;
            world.CastRay(inputs[index], out hit);
            results[index] = hit;
        }
    }

    public static JobHandle ScheduleBatchRayCast(CollisionWorld world,
        NativeArray<RaycastInput> inputs, NativeArray<RaycastHit> results)
    {
        JobHandle rcj = new RaycastJob
        {
            inputs = inputs,
            results = results,
            world = world

        }.Schedule(inputs.Length, 4);
        return rcj;
    }

    public Entity Raycast(float3 RayFrom, float3 RayTo)
    {
        var physicsWorldSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        RaycastInput input = new RaycastInput()
        {
            Start = RayFrom,
            End = RayTo,
            Filter = new CollisionFilter()
            {
                BelongsTo = ~0u,
                CollidesWith = ~0u, // all 1s, so all layers, collide with everything
                GroupIndex = 0
            }
        };

        RaycastHit hit = new RaycastHit();

        bool haveHit = collisionWorld.CastRay(input, out hit);
        if (haveHit)
        {
            // see hit.Position
            // see hit.SurfaceNormal
            Entity e = physicsWorldSystem.PhysicsWorld.Bodies[hit.RigidBodyIndex].Entity;
            return e;
        }
        return Entity.Null;
    }

    protected override void OnUpdate()
    {
        throw new System.NotImplementedException();
    }
}