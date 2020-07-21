using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Collections;
[UpdateBefore(typeof(MovementJumpSystem))]
public class TransformStateSystem : SystemBase
{

    public EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBuffer;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBuffer = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        /* var entityCommandBufferConcurrent = endSimulationEntityCommandBuffer
                .CreateCommandBuffer().ToConcurrent(); */

        /* var getEntities = GetEntityQuery(new EntityQueryDesc
        {
            None = new ComponentType[] { typeof(Prefab) },
            All = new ComponentType[] {
                ComponentType.ReadOnly<BufferCollisionEventElement>(),
                typeof(MovementState)
            }
        }).ToEntityArrayAsync(Allocator.TempJob, out JobHandle getEntitiesHandle);
        Dependency = JobHandle.CombineDependencies(Dependency, getEntitiesHandle); */

        // var newMovementState = new NativeArray<TransformState>(getEntities.Length, Allocator.TempJob);
        //var getMovementState = GetComponentDataFromEntity<MovementState>();

        /* var readJob = new TransformStateReadJob
        {
            Entities = getEntities,
            getCollisionBuffer = GetBufferFromEntity<BufferCollisionEventElement>(true),
            getMovementState = getMovementState,
            Results = newMovementState,
        };
        var readJobHandle = readJob.Schedule(getEntities.Length, 1, Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, readJobHandle); */

        var writeJobHandle = Entities.WithName("UpdateEntityMovementState")
            .WithNone<Prefab>()
            .ForEach(
                (
                    int entityInQueryIndex,
                    ref Entity entity,
                    ref MovementState movementState,
                    ref MovementJumpComponent jumpInputComponent,
                    ref DynamicBuffer<BufferCollisionEventElement> collisionBuffer
                ) =>
                {

                    if (jumpInputComponent.JumpTrigger && movementState.Value == TransformState.Grounded)
                    {
                        movementState.Value = TransformState.StartJumping;
                    }
                    // * one jump is still available and entity is in air
                    // * -> update state to InAir
                    else if (collisionBuffer.Length <= 0 && !jumpInputComponent.FirstJump && jumpInputComponent.SecondJump)
                    {
                        movementState.Value = TransformState.InAir;
                    }
                    // * no jumps are available and the entity is not touching / colliding with
                    // * any other objects -> update state to Landing
                    else if (collisionBuffer.Length <= 0 && !jumpInputComponent.FirstJump && !jumpInputComponent.SecondJump)
                    {
                        movementState.Value = TransformState.Landing;
                    }
                    // * if entity can jump and is currently not about to start jumping 
                    // * and is colliding with another object
                    // * -> update state to grounded
                    else if (collisionBuffer.Length > 0 && movementState.Value != TransformState.StartJumping)
                    {
                        movementState.Value = TransformState.Grounded;
                        jumpInputComponent.FirstJump = true;
                        jumpInputComponent.SecondJump = true;
                    }
                }
            ).Schedule(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, writeJobHandle);

    }

    public struct TransformStateWriteJob : IJob
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Entities;
        [DeallocateOnJobCompletion]
        public NativeArray<TransformState> ResultsToWrite;
        public ComponentDataFromEntity<MovementState> getMovementState;
        public ComponentDataFromEntity<MovementJumpComponent> getJumpComponent;
        public EntityCommandBuffer.Concurrent EntityCommandBufferConcurrent;

        public void Execute()
        {


            for (var i = 0; i < Entities.Length; i++)
            {
                var entity = Entities[i];

                // * update movement state
                var movementState = getMovementState[entity];

                movementState.Value = ResultsToWrite[i];
                EntityCommandBufferConcurrent.SetComponent<MovementState>(i, entity, movementState);

                // * if entity can jump -> update jump component
                if (ResultsToWrite[i] == TransformState.Grounded && getJumpComponent.HasComponent(entity))
                {
                    var jumpComponent = getJumpComponent[entity];
                    jumpComponent.FirstJump = true;
                    jumpComponent.SecondJump = true;
                    EntityCommandBufferConcurrent.SetComponent<MovementJumpComponent>(i + 1, entity, jumpComponent);
                }
            }
        }
    }

    public struct TransformStateReadJob : IJobParallelFor
    {
        [DeallocateOnJobCompletion]
        public NativeArray<Entity> Entities;
        public NativeArray<TransformState> Results;
        [ReadOnly]
        public BufferFromEntity<BufferCollisionEventElement> getCollisionBuffer;
        [ReadOnly]
        public ComponentDataFromEntity<MovementState> getMovementState;
        public void Execute(int index)
        {
            var entity = Entities[index];
            bool grounded = false;
            var collisionBuffer = getCollisionBuffer[entity];
            var movementState = getMovementState[entity];

            foreach (var collision in collisionBuffer)
            {
                if (getMovementState.HasComponent(entity))
                {
                    grounded = true;
                    break;
                }
            }

            // * check collision of entity with other objects
            // * if object is currently colliding TransformationState is set to Grounded
            // * if object is currently not colliding TransformationState is set to InAir
            // ? not final version as object could be colliding in air as well without being grounded
            var state = grounded ? TransformState.Grounded : TransformState.InAir;

            // * update entity state results array
            Results[index] = state;

        }
    }
}