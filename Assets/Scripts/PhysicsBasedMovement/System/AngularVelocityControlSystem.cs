using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;

///<summary>
/// This system is controlling moving objects to always be trying to follow an up-right moving position.
/// Control is used on following conditions:
/// <para>(1) The downwards vector on Raycast is hitting a surface. The surface normal can be used as orientation for new up vector</para>
/// <para>(2) The difference between th surface normal and the entities upwards vector is on either the x or z axis larger then the <see cref="ControlThresholdTop"/></para>
///</summary>
[UpdateAfter(typeof(MovementSystem))]
public class AngularVelocityControlSystem : SystemBase
{
    private Unity.Physics.Systems.BuildPhysicsWorld physicsWorldSystem;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private float ControlThresholdStart = 50f;
    private float ControlThresholdStop = 10f;
    private float controlMultiplier = 1f;
    protected override void OnCreate()
    {
        physicsWorldSystem = World
            .DefaultGameObjectInjectionWorld
            .GetExistingSystem<Unity.Physics.Systems.BuildPhysicsWorld>();
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
    }
    protected override void OnUpdate()
    {
        // * localization of thresholds for jobs
        var angularControlTopThreshold = math.radians(ControlThresholdStart);
        var angularControlBottomThreshold = math.radians(ControlThresholdStop);
        // * localize multiplier for jobs
        //var multiplier = controlMultiplier;

        // * confirm surface normal on all controllable entities and add force
        // * accordingly to straighten entities back relative to surface normal
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        var entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var systemJobHandle = Entities.WithName("AngularControl_Measurement")
            .WithNone<Prefab>()
            .ForEach(
                (
                    ref PhysicsVelocity velocity,
                    ref AngularVelocityControlComponent angularControlComponent,
                    in PhysicsMass mass,
                    in LocalToWorld localToWorld,
                    in Rotation rotationData
                ) =>
                {
                    // * perfom raycast
                    var rayDirection = -localToWorld.Up;

                    BitField32 filter = new BitField32();
                    filter.SetBits(1, true, 31);
                    filter.SetBits(0, false);

                    RaycastInput input = new RaycastInput()
                    {
                        Start = localToWorld.Position,
                        End = localToWorld.Position + rayDirection,
                        Filter = new CollisionFilter()
                        {
                            BelongsTo = ~0u,
                            CollidesWith = filter.GetBits(0, 32), // all 1s, so all layers, exept layer 0 = player layer
                            GroupIndex = 0
                        }
                    };

                    RaycastHit hit;
                    angularControlComponent.hasSurface = collisionWorld.CastRay(input, out hit);

                    // * control angular spin relative to surface normal
                    // * if no surface can be detected the normal (0, 0, 1)
                    var surfaceNormal = angularControlComponent.hasSurface ? hit.SurfaceNormal : new float3(0, 1, 0);

                    var entityUpward = localToWorld.Up;

                    var radiansSurfaceNormalZ = Angles.getAngleBetweenAxisWithAtan(surfaceNormal.x, surfaceNormal.y);
                    var radiansEntityUpwardZ = Angles.getAngleBetweenAxisWithAtan(entityUpward.x, entityUpward.y);
                    var surfaceToEntityDifferenceZ = radiansSurfaceNormalZ - radiansEntityUpwardZ;

                    surfaceToEntityDifferenceZ = Angles.getSmallAngle(surfaceToEntityDifferenceZ);

                    // * get radians for surface normal and up-vector in between z and y axis
                    var radiansSurfaceNormalX = Angles.getAngleBetweenAxisWithAtan(surfaceNormal.z, surfaceNormal.y);
                    var radiansEntityUpwardX = Angles.getAngleBetweenAxisWithAtan(entityUpward.z, entityUpward.y);
                    var surfaceToEntityDifferenceX = radiansSurfaceNormalX - radiansEntityUpwardX;

                    surfaceToEntityDifferenceX = Angles.getSmallAngle(surfaceToEntityDifferenceX);

                    // * define multiplier depending on measurement difference
                    var measurementDifference = (surfaceToEntityDifferenceX + surfaceToEntityDifferenceZ) / math.PI;
                    var addedControlSpeed = math.clamp(1 / (measurementDifference), 1f, 3f);
                    // * update control force
                    angularControlComponent.x = math.pow(surfaceToEntityDifferenceX, 2f) + addedControlSpeed;
                    angularControlComponent.z = math.pow(surfaceToEntityDifferenceZ, 2f) + addedControlSpeed;

                    // * update direction of control
                    // * update direction only while not resetting
                    angularControlComponent.directionX = angularControlComponent.resetXAxis ? angularControlComponent.directionX : surfaceToEntityDifferenceX > 0;
                    angularControlComponent.directionZ = angularControlComponent.resetZAxis ? angularControlComponent.directionZ : surfaceToEntityDifferenceZ > 0;

                    // * set control permission for x and z axis 
                    angularControlComponent.resetXAxis = angularControlTopThreshold < math.abs(surfaceToEntityDifferenceX) ?
                        true : angularControlComponent.resetXAxis;
                    angularControlComponent.resetZAxis = angularControlTopThreshold < math.abs(surfaceToEntityDifferenceZ) ?
                        true : angularControlComponent.resetZAxis;

                    // * reset permission value once control is sufficient
                    angularControlComponent.resetXAxis =
                        angularControlBottomThreshold > math.abs(surfaceToEntityDifferenceX) && angularControlComponent.resetXAxis ?
                        false : angularControlComponent.resetXAxis;
                    angularControlComponent.resetZAxis =
                        angularControlBottomThreshold > math.abs(surfaceToEntityDifferenceZ) && angularControlComponent.resetZAxis ?
                        false : angularControlComponent.resetZAxis;

                    /*  UnityEngine.Debug.Log("Reset x: " + angularControlComponent.resetXAxis + " (with " + angularControlTopThreshold + " - " + math.abs(surfaceToEntityDifferenceX) + ")" +
                     "\nReset Z: " + angularControlComponent.resetZAxis + " (with " + angularControlTopThreshold + " - " + math.abs(surfaceToEntityDifferenceZ) + ")"); */
                    //UnityEngine.Debug.DrawLine(localToWorld.Position, localToWorld.Up + localToWorld.Position, UnityEngine.Color.red);
                }
            )
            .WithReadOnly(collisionWorld)
            .ScheduleParallel(Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, systemJobHandle);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(systemJobHandle);
    }
}