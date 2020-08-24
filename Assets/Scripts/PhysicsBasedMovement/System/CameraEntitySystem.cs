using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;

[UpdateAfter(typeof(InputSystem))]
public class CameraEntitySystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private Entity playerEntity;
    private UnityEngine.GameObject playerCameraObject;
    private float3 cameraPosition;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    protected override void OnStartRunning()
    {
        playerEntity = GetSingletonEntity<PlayerPhysicsTag>();

        playerCameraObject = UnityEngine.Camera.main.gameObject;
        var cameraTransform = playerCameraObject.transform;
        var cameraEntity = GetSingletonEntity<CameraComponent>(); // ? not very flexible solution -> what if we want multiple cameras per player or scene?
        var camera = GetComponent<CameraComponent>(cameraEntity);
        var cameraForward = math.normalizesafe(cameraTransform.forward);
        var pos = GetComponent<LocalToWorld>(playerEntity).Position;
        cameraPosition = pos - (cameraForward * camera.orbitMultiplier) + camera.offset;
        SetComponent<Translation>(cameraEntity, new Translation { Value = cameraPosition });
    }
    protected override void OnUpdate()
    {
        playerCameraObject = UnityEngine.Camera.main.gameObject;
        var cameraTransform = playerCameraObject.transform;

        var lookInput = GetComponent<LookDirectionInputComponent>(playerEntity).ScreenValue;
        var pos = GetComponent<LocalToWorld>(playerEntity).Position;
        var cameraEntity = GetSingletonEntity<CameraComponent>(); // ? not very flexible solution -> what if we want multiple cameras per player or scene?
        var cameraComponent = GetComponent<CameraComponent>(cameraEntity);

        // * old camera position
        var new_cameraPosition = pos - (cameraTransform.forward * cameraComponent.orbitMultiplier) + cameraComponent.offset;
        // * map distance between 0 and 1, as the positions are normalized the distance will never be larger then 1
        // * create non-linear interpolation between old and new camera position
        var distance = math.distance(math.normalizesafe(new_cameraPosition), math.normalizesafe(cameraPosition));
        distance = math.clamp(math.pow(cameraComponent.movementSpeedAdjustment + distance, 2f), 0, 1);
        var cameraMovement = (distance) * (new_cameraPosition - cameraPosition);
        cameraPosition += cameraMovement;

        // * update camera position
        SetComponent<Translation>(cameraEntity, new Translation { Value = cameraPosition });

        // * get components
        var localToWorld = GetComponent<LocalToWorld>(cameraEntity);
        var velocity = GetComponent<PhysicsVelocity>(cameraEntity);

        var horizontalForceIsPositive = lookInput.x > 0;
        var verticalForceIsPositive = lookInput.y > 0;

        // * get threshold above which turning is allowed 
        var thresholdInDeg = cameraComponent.turnInputThreshold;
        var threshold = math.radians(thresholdInDeg) / math.PI;

        // * negate threshold if necessary 
        var horizontalThreshold = horizontalForceIsPositive ? threshold : -threshold;
        var verticalThreshold = verticalForceIsPositive ? threshold : -threshold;

        // * get maximum vertical rotation
        var maxVerticalRotation = math.radians(cameraComponent.maxRotationXAxis);
        var currentVerticalRotation = Angles.getSmallAngle(math.radians(cameraTransform.rotation.eulerAngles.x));
        var insideMaxVerticalRotation =
            (math.abs(currentVerticalRotation) < maxVerticalRotation)
            || (currentVerticalRotation < -maxVerticalRotation && !verticalForceIsPositive)
            || (currentVerticalRotation > maxVerticalRotation && verticalForceIsPositive);

        // * decide if turning is possible
        var canTurnHorizontal = horizontalForceIsPositive ? lookInput.x > horizontalThreshold : lookInput.x < horizontalThreshold;
        var canTurnVertical = verticalForceIsPositive ?
            (lookInput.y > verticalThreshold && insideMaxVerticalRotation)
            : (lookInput.y < verticalThreshold && insideMaxVerticalRotation);

        // * calculate force if turning is possible, else return 0 as force
        // * inverting vertical force as positive vertical force applies 'down-turning' of camera
        var horizontalForce = canTurnHorizontal ? math.pow(cameraComponent.rotationSpeed * (lookInput.x - horizontalThreshold), 2f) : 0;
        horizontalForce = horizontalForceIsPositive ? horizontalForce : -horizontalForce;
        var verticalForce = canTurnVertical ? -math.pow(cameraComponent.rotationSpeed * (lookInput.y - verticalThreshold), 2f) : 0; // here vertical force is inverted
        verticalForce = verticalForceIsPositive ? verticalForce : -verticalForce; // here the force is assigned negative or positive direction

        // * apply force
        velocity.Angular = new float3(verticalForce, horizontalForce, 0);

        SetComponent<CameraComponent>(cameraEntity, cameraComponent);
        SetComponent<PhysicsVelocity>(cameraEntity, velocity);

        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(Dependency);
    }
}