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
    private readonly float ControlThresholdTop = 25f;
    private readonly float ControlThresholdBottom = 10f;
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
        var angularControlTopThreshold = ControlThresholdTop;
        var angularControlBottomThreshold = ControlThresholdBottom;

        var angularControlActivationHandle = Entities.WithName("AngularControl_Activation_Deactivation_Update")
                .WithNone<Prefab>()
                .ForEach(
                    (
                        ref AngularVelocityControlComponent angularControlComponent,
                        ref PhysicsVelocity velocity,
                        in PhysicsMass mass,
                        in Rotation rotation
                    ) =>
                    {
                        angularControlComponent.ActiveOnXY = math.abs(angularControlComponent.DegreesFromYAxisToXAxis) > angularControlTopThreshold ?
                            true : angularControlComponent.ActiveOnXY;
                        angularControlComponent.ActiveOnZY = math.abs(angularControlComponent.DegreesFromYAxisToZAxis) > angularControlTopThreshold ?
                            true : angularControlComponent.ActiveOnZY;

                        var controlledVelocityX = math.abs(angularControlComponent.DegreesFromYAxisToXAxis) > angularControlTopThreshold ?
                            Angles.regulateVelocityStrength(math.radians(angularControlComponent.DegreesFromYAxisToXAxis))
                            : velocity.Angular.x;
                        var controlledVelocityZ = math.abs(angularControlComponent.DegreesFromYAxisToZAxis) > angularControlTopThreshold ?
                            Angles.regulateVelocityStrength(math.radians(angularControlComponent.DegreesFromYAxisToZAxis))
                            : velocity.Angular.z;


                        if ((math.abs(angularControlComponent.DegreesFromYAxisToXAxis) < angularControlBottomThreshold && angularControlComponent.ActiveOnXY)
                                    || (math.abs(angularControlComponent.DegreesFromYAxisToZAxis) < angularControlBottomThreshold && angularControlComponent.ActiveOnZY))
                        {
                            controlledVelocityX = angularControlComponent.ActiveOnXY ? 0 : velocity.Angular.x;
                            controlledVelocityZ = angularControlComponent.ActiveOnZY ? 0 : velocity.Angular.z;

                            angularControlComponent.ActiveOnXY = angularControlComponent.ActiveOnXY ? false : angularControlComponent.ActiveOnXY;
                            angularControlComponent.ActiveOnZY = angularControlComponent.ActiveOnZY ? false : angularControlComponent.ActiveOnZY;
                        }

                        velocity.Angular.x = controlledVelocityX;
                        velocity.Angular.z = controlledVelocityZ;
                    }
                ).ScheduleParallel(Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, angularControlActivationHandle);


        // * confirm surface normal on all controllable entities and add force
        // * accordingly to straighten entities back relative to surface normal
        var collisionWorld = physicsWorldSystem.PhysicsWorld.CollisionWorld;
        var entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().AsParallelWriter();
        var systemJobHandle = Entities.WithName("ControlAngularVelocitySystem")
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
                    collisionWorld.CastRay(input, out hit);

                    // * control angular spin relative to surface normal
                    // * get radians for surface normal and up-vector in between x and y axis
                    var surfaceNormal = hit.SurfaceNormal;
                    var radiansSurfaceNormalXY = Angles.getAngleBetweenAxisWithAtan(surfaceNormal.x, surfaceNormal.y);
                    var radiansEntityUpwardXY = Angles.getAngleBetweenAxisWithAtan(localToWorld.Up.x, localToWorld.Up.y);
                    var surfaceToEntityDifferenceXY = radiansSurfaceNormalXY - radiansEntityUpwardXY;

                    surfaceToEntityDifferenceXY = Angles.getSmallAngle(surfaceToEntityDifferenceXY);
                    //surfaceToEntityDifferenceXY = AngularVelocityControlSystem.regulateVelocityStrength(surfaceToEntityDifferenceXY);

                    // * get radians for surface normal and up-vector in between z and y axis
                    var radiansSurfaceNormalZY = Angles.getAngleBetweenAxisWithAtan(surfaceNormal.z, surfaceNormal.y);
                    var radiansEntityUpwardZY = Angles.getAngleBetweenAxisWithAtan(localToWorld.Up.z, localToWorld.Up.y);
                    var surfaceToEntityDifferenceZY = radiansSurfaceNormalZY - radiansEntityUpwardZY;

                    surfaceToEntityDifferenceZY = Angles.getSmallAngle(surfaceToEntityDifferenceZY);
                    //surfaceToEntityDifferenceZY = AngularVelocityControlSystem.regulateVelocityStrength(surfaceToEntityDifferenceZY);

                    angularControlComponent.DegreesFromYAxisToXAxis = math.degrees(surfaceToEntityDifferenceXY);
                    angularControlComponent.DegreesFromYAxisToZAxis = math.degrees(surfaceToEntityDifferenceZY);
                    /* UnityEngine.Debug.Log("xyDegrees: " + angularControlComponent.DegreesFromYAxisToXAxis + ", 
                                            zyDegrees: " + angularControlComponent.DegreesFromYAxisToZAxis);*/
                }
            )
            .WithReadOnly(collisionWorld)
            .ScheduleParallel(Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, systemJobHandle);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(systemJobHandle);
    }
}