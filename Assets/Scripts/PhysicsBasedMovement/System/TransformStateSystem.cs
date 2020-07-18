using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

public class TransformStateSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var getCollisionBuffer = GetBufferFromEntity<BufferCollisionEventElement>(true);

        var handle = Entities.WithName("TransformStateSystem_Job")
            .WithNone<Prefab>()
            .ForEach(
                (
                    ref MovementState movementState,
                    in Entity entity
                ) =>
                {
                    var collisionBuffer = getCollisionBuffer[entity];
                    bool grounded = false;
                    foreach (var colliderElement in collisionBuffer)
                    {
                        if (HasComponent<GroundTag>(colliderElement.Entity))
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

                    // * update entity state
                    movementState.Value = state;

                })
                .WithReadOnly(getCollisionBuffer)
                .ScheduleParallel(Dependency);

        Dependency = JobHandle.CombineDependencies(Dependency, handle);
    }
}