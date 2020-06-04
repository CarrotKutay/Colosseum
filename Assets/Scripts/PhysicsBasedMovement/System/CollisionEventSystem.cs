using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class CollisionEventSystem : SystemBase
{

    BuildPhysicsWorld _buildPhysicsWorldSystem;
    StepPhysicsWorld _stepPhysicsWorldSystem;

    protected override void OnCreate()
    {
        _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    public struct CollisionJob : ICollisionEventsJob
    {
        public void Execute(CollisionEvent collisionEvent)
        {
            UnityEngine.Debug.Log("Collision between: \n" + collisionEvent.Entities.EntityA.ToString() + ", " + collisionEvent.Entities.EntityB.ToString());
        }
    }

    protected override void OnUpdate()
    {
        var collisionJob = new CollisionJob { };
        var collisionJobHandle = collisionJob.Schedule(_stepPhysicsWorldSystem.Simulation, ref _buildPhysicsWorldSystem.PhysicsWorld, Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, collisionJobHandle);
    }
}