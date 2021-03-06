﻿using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

/* 
    * Taking inspiration from https://forum.unity.com/threads/sources-included-easy-to-use-trigger-collision-events-system.878203/
    * credits to PhilSA for this one

    * This collision system works by each entity possessing a collision-buffer.
    * The collision-buffer saves collisions for its entity inside its buffer elements.
 */

[UpdateAfter(typeof(EndFramePhysicsSystem))]
public class CollisionEventSystem : SystemBase
{

    private BuildPhysicsWorld _buildPhysicsWorldSystem;
    private StepPhysicsWorld _stepPhysicsWorldSystem;
    private EntityQuery EntityQuery;


    protected override void OnCreate()
    {
        _buildPhysicsWorldSystem = World.GetOrCreateSystem<BuildPhysicsWorld>();
        _stepPhysicsWorldSystem = World.GetOrCreateSystem<StepPhysicsWorld>();
    }

    struct CollisionEventsPreProcessJob : IJobChunk
    {
        public BufferTypeHandle<BufferCollisionEventElement> CollisionEventBufferType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            BufferAccessor<BufferCollisionEventElement> collisionEventsBufferAccessor = chunk.GetBufferAccessor(CollisionEventBufferType);

            for (int i = 0; i < chunk.Count; i++)
            {
                DynamicBuffer<BufferCollisionEventElement> collisionEventsBuffer = collisionEventsBufferAccessor[i];

                for (int j = collisionEventsBuffer.Length - 1; j >= 0; j--)
                {
                    BufferCollisionEventElement collisionEventElement = collisionEventsBuffer[j];
                    collisionEventElement.isStale = true;
                    collisionEventsBuffer[j] = collisionEventElement;
                }
            }
        }
    }

    struct CollisionEventsPostProcessJob : IJobChunk
    {
        public BufferTypeHandle<BufferCollisionEventElement> CollisionEventBufferType;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            if (chunk.Has(CollisionEventBufferType))
            {
                BufferAccessor<BufferCollisionEventElement> collisionEventsBufferAccessor = chunk.GetBufferAccessor(CollisionEventBufferType);

                for (int i = 0; i < chunk.Count; i++)
                {
                    DynamicBuffer<BufferCollisionEventElement> collisionEventsBuffer = collisionEventsBufferAccessor[i];

                    for (int j = collisionEventsBuffer.Length - 1; j >= 0; j--)
                    {
                        BufferCollisionEventElement collisionEvent = collisionEventsBuffer[j];

                        if (collisionEvent.isStale)
                        {
                            if (collisionEvent.State == PhysicsCollisionEventState.Exit)
                            {
                                collisionEventsBuffer.RemoveAt(j);
                            }
                            else
                            {
                                collisionEvent.State = PhysicsCollisionEventState.Exit;
                                collisionEventsBuffer[j] = collisionEvent;
                            }
                        }
                    }
                }
            }
        }
    }

    public struct CollisionJob : ICollisionEventsJob
    {
        [ReadOnly] public PhysicsWorld PhysicsWorld;
        public BufferFromEntity<BufferCollisionEventElement> CollisionEventBufferFromEntity;
        [ReadOnly] public ComponentDataFromEntity<CollisionEventsReceiverProperties> CollisionEventsReceiverPropertiesFromEntity;

        public void Execute(CollisionEvent collisionEvent)
        {
            CollisionEvent.Details collisionEventDetails = default;

            bool AHasDetails = false;
            bool BHasDetails = false;

            if (CollisionEventsReceiverPropertiesFromEntity.HasComponent(collisionEvent.EntityA))
            {
                AHasDetails = CollisionEventsReceiverPropertiesFromEntity[collisionEvent.EntityA].UsesCollisionDetails;
            }
            if (CollisionEventsReceiverPropertiesFromEntity.HasComponent(collisionEvent.EntityB))
            {
                BHasDetails = CollisionEventsReceiverPropertiesFromEntity[collisionEvent.EntityB].UsesCollisionDetails;
            }

            if (AHasDetails || BHasDetails)
            {
                collisionEventDetails = collisionEvent.CalculateDetails(ref PhysicsWorld);
            }

            if (CollisionEventBufferFromEntity.HasComponent(collisionEvent.EntityA))
            {
                ProcessForEntity(collisionEvent.EntityA, collisionEvent.EntityB, collisionEvent.Normal, AHasDetails, collisionEventDetails);
            }
            if (CollisionEventBufferFromEntity.HasComponent(collisionEvent.EntityB))
            {
                ProcessForEntity(collisionEvent.EntityB, collisionEvent.EntityA, collisionEvent.Normal, BHasDetails, collisionEventDetails);
            }
        }

        private void ProcessForEntity(Entity entity, Entity otherEntity, float3 normal, bool hasDetails, CollisionEvent.Details collisionEventDetails)
        {
            DynamicBuffer<BufferCollisionEventElement> collisionEventBuffer = CollisionEventBufferFromEntity[entity];

            bool foundMatch = false;
            for (int i = 0; i < collisionEventBuffer.Length; i++)
            {
                BufferCollisionEventElement collisionEvent = collisionEventBuffer[i];

                // If entity is already there, update to Stay
                if (collisionEvent.Entity == otherEntity)
                {
                    foundMatch = true;
                    collisionEvent.Normal = normal;
                    collisionEvent.HasCollisionDetails = hasDetails;
                    collisionEvent.AverageContactPointPosition = collisionEventDetails.AverageContactPointPosition;
                    collisionEvent.EstimatedImpulse = collisionEventDetails.EstimatedImpulse;
                    collisionEvent.State = PhysicsCollisionEventState.Stay;
                    collisionEvent.isStale = false;
                    collisionEventBuffer[i] = collisionEvent;

                    break;
                }
            }

            // If it's a new entity, add as Enter
            if (!foundMatch)
            {
                collisionEventBuffer.Add(new BufferCollisionEventElement
                {
                    Entity = otherEntity,
                    Normal = normal,
                    HasCollisionDetails = hasDetails,
                    AverageContactPointPosition = collisionEventDetails.AverageContactPointPosition,
                    EstimatedImpulse = collisionEventDetails.EstimatedImpulse,
                    State = PhysicsCollisionEventState.Enter,
                    isStale = false,
                });
            }
        }
    }

    protected override void OnUpdate()
    {
        // * get all new events 
        EntityQuery = GetEntityQuery(new EntityQueryDesc
        {
            All = new ComponentType[] { typeof(BufferCollisionEventElement), typeof(PhysicsCollider) }
        });

        // * pre-process them:
        // * - working them form the back to front
        // * - marking them as stale to be discarded after processing
        var preCollisionJob = new CollisionEventsPreProcessJob
        {
            CollisionEventBufferType = GetBufferTypeHandle<BufferCollisionEventElement>(),
        };
        var preCollisionJobHandle = preCollisionJob.ScheduleParallel(EntityQuery, Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, preCollisionJobHandle);

        // * processing the event
        var collisionJob = new CollisionJob
        {
            CollisionEventBufferFromEntity = GetBufferFromEntity<BufferCollisionEventElement>(),
            CollisionEventsReceiverPropertiesFromEntity = GetComponentDataFromEntity<CollisionEventsReceiverProperties>(true),
            PhysicsWorld = _buildPhysicsWorldSystem.PhysicsWorld,
        };
        var collisionJobHandle = collisionJob.Schedule(_stepPhysicsWorldSystem.Simulation, ref _buildPhysicsWorldSystem.PhysicsWorld, Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, collisionJobHandle);

        // * post-processing of all events:
        // * - stale marked events will be deleted from their buffers 
        var postCollisionJob = new CollisionEventsPostProcessJob
        {
            CollisionEventBufferType = GetBufferTypeHandle<BufferCollisionEventElement>(),
        };
        var postCollisionJobHandle = postCollisionJob.ScheduleParallel(EntityQuery, Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, postCollisionJobHandle);

    }
}