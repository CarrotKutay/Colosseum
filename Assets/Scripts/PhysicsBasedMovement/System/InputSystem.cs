using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
[UpdateBefore(typeof(TransformStateSystem))]
public class InputSystem : SystemBase
{
    private PlayerAction PlayerInput;
    private EntityManager Manager;
    private Entity PlayerPhysics;
    private float3 LookDirectionWorldSpace;
    private float3 LookDirectionScreenSpace;
    private float2 currentPointerDirection;
    private float2 currentMovementDirectionInput;
    private float screenHeight;
    private float screenWidth;

    protected override void OnCreate()
    {
        PlayerInput = new PlayerAction();

        Manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;

        activateDodgeEvents();
        activatingMovementEvents();
        activateLookEvents();
        activateJumpEvent();
    }

    protected override void OnDestroy()
    {
        deactivateDodgeEvents();
        deactivatingMovementEvents();
        deactivateLookEvents();
        deactivateJumpEvent();
    }

    #region Activation Behaviour
    protected override void OnStartRunning()
    {
        //Debug.Log("width: " + Camera.main.scaledPixelWidth + ", height: " + Camera.main.scaledPixelHeight);
        PlayerInput.Enable();

        var Player = GetSingletonEntity<PlayerTag>();
        var buffer = GetBufferFromEntity<LinkedEntityGroup>()[Player].Reinterpret<Entity>();
        foreach (var entity in buffer)
        {
            if (HasComponent<PhysicsVelocity>(entity))
            {
                PlayerPhysics = entity;
                Manager.AddComponentData(PlayerPhysics, new PlayerPhysicsTag { });
                break;
            }
        }

        screenHeight = Camera.main.scaledPixelHeight / 2;
        screenWidth = Camera.main.scaledPixelWidth / 2;
    }

    protected override void OnStopRunning()
    {
        PlayerInput.Disable();
    }

    #endregion

    #region Adding / Removing Input events methods

    private void activateDodgeEvents()
    {
        // Dodge Input
        PlayerInput.Player.DodgeForwardPressAsButton.performed += _ => readDodgeInput();
    }

    private void activatingMovementEvents()
    {
        // basic movement
        PlayerInput.Player.MoveDownPressAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveDownReleaseAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveUpPressAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveUpReleaseAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveRightPressAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveRightReleaseAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveLeftPressAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveLeftReleaseAsButton.performed +=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());

    }

    private void activateJumpEvent()
    {
        // jump input
        PlayerInput.Player.Jump.performed += _ => readJumpInput();
    }

    private void deactivateJumpEvent()
    {
        PlayerInput.Player.Jump.performed -= _ => readJumpInput();
    }

    private void activateLookEvents()
    {
        // look input
        PlayerInput.Player.Look.performed +=
            _ => readLookInput(PlayerInput.Player.Look.ReadValue<Vector2>());
    }

    private void deactivateDodgeEvents()
    {
        // DodgeInput
        PlayerInput.Player.DodgeForwardPressAsButton.performed -= _ => readDodgeInput();
    }

    private void deactivateLookEvents()
    {
        // look input
        PlayerInput.Player.Look.performed -=
            _ => readLookInput(PlayerInput.Player.Look.ReadValue<Vector2>());
    }

    private void deactivatingMovementEvents()
    {
        // basic movement
        PlayerInput.Player.MoveDownPressAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveDownReleaseAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveUpPressAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveUpReleaseAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveRightPressAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveRightReleaseAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveLeftPressAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        PlayerInput.Player.MoveLeftReleaseAsButton.performed -=
            _ => readMovementInput(PlayerInput.Player.MoveCompositeAsValue.ReadValue<Vector2>());
    }

    #endregion

    #region Reading input

    #region read movement input
    private void readMovementInput(float2 MovementDirection)
    {
        currentMovementDirectionInput = MovementDirection;
    }

    private void updatePlayerMovementinput()
    {
        var movementDirectionInputComponent = GetComponent<MovementDirectionInputComponent>(PlayerPhysics);
        movementDirectionInputComponent.NewValue = currentMovementDirectionInput;
        SetComponent<MovementDirectionInputComponent>(PlayerPhysics, movementDirectionInputComponent);
    }

    #endregion read movement input

    private void readJumpInput()
    {
        if (HasComponent<MovementJumpComponent>(PlayerPhysics))
        {
            var jumpInputComponent = GetComponent<MovementJumpComponent>(PlayerPhysics);
            jumpInputComponent.JumpTrigger = true;
            SetComponent<MovementJumpComponent>(PlayerPhysics, jumpInputComponent);
        }
    }

    private void readDodgeInput()
    {

    }

    #region read look input
    private void readLookInput(float2 Direction)
    {
        currentPointerDirection = Direction;
    }

    private void getNewLookDirection()
    {
        var playerPosition = GetComponentDataFromEntity<Translation>(true)[PlayerPhysics].Value;
        var zCoord = Camera.main.farClipPlane;
        var inputDirection = currentPointerDirection;

        // * screen space look input direction 
        inputDirection.x = (inputDirection.x - screenWidth) / screenWidth;
        inputDirection.y = (inputDirection.y - screenHeight) / screenHeight;
        LookDirectionScreenSpace = new float3(inputDirection.x, inputDirection.y, zCoord);

        // * world space look input direction
        LookDirectionWorldSpace = new float3(Camera.main.ScreenToWorldPoint(
            new float3(currentPointerDirection.x, currentPointerDirection.y, zCoord)));

        // TODO: turn on/off debug 
        Debug.DrawLine(LookDirectionWorldSpace, LookDirectionWorldSpace + new float3(0, 10f, 0), Color.red);
    }

    private void updatePlayerLookDirection()
    {
        var lookInput = GetComponent<LookDirectionInputComponent>(PlayerPhysics);
        lookInput.WorldValue = LookDirectionWorldSpace;
        lookInput.ScreenValue = LookDirectionScreenSpace;
        SetComponent<LookDirectionInputComponent>(PlayerPhysics, lookInput);
    }


    #endregion lookInput

    #endregion


    protected override void OnUpdate()
    {
        var maxDurationValue = new float3(.8f, 0, .8f);
        var DeltaTime = Time.DeltaTime;

        // ? could test if it will sharpen performance to run following three tasks 
        // ? inside seperate job
        getNewLookDirection();
        updatePlayerLookDirection();
        updatePlayerMovementinput();


        // * scheduling job to update the inputHold duration on movement related input
        var handle = Entities.WithName("GetInputHoldDuration")
            .WithAll<PlayerPhysicsTag>()
            .WithNone<Prefab>()
            .ForEach(
                (ref InputHoldComponent InputHoldComponent, ref MovementDirectionInputComponent movementInput) =>
                {
                    if (movementInput.NewValue.Equals(movementInput.OldValue)
                        && !(movementInput.NewValue.Equals(float2.zero)))
                    {
                        var holdValue = DeltaTime * 2; // increased value to make startup velocity a bit faster 

                        InputHoldComponent.Value.x += holdValue;
                        InputHoldComponent.Value.z += holdValue;
                        InputHoldComponent.Value = math.clamp(InputHoldComponent.Value, -maxDurationValue, maxDurationValue);
                    }
                    else
                    { InputHoldComponent.Value = float3.zero; }

                    movementInput.OldValue = movementInput.NewValue;
                }
        ).Schedule(Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, handle);

        //CompleteDependency();
        World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>()
            .AddJobHandleForProducer(Dependency);
    }
}


// read movement options

// * version 1
/* var ecb_concurrent = endSimulationEntityCommandBufferSystem
    .CreateCommandBuffer()
    .ToConcurrent();
var ReadMovementComponent = GetComponentDataFromEntity<MovementDirectionInputComponent>();

var readMovementInputJob = new ReadMovementInputJob
{
    EntityCommandBuffer = ecb_concurrent,
    GetMovementInput = ReadMovementComponent,
    newMovementInput = MovementDirection,
    Player = PlayerPhysics,
};
var handle = readMovementInputJob.Schedule(Dependency);
handle.Complete(); */

// * version 2
/*  var ecb_concurrent = endSimulationEntityCommandBufferSystem
     .CreateCommandBuffer()
     .ToConcurrent();
 var ReadMovementComponent = GetComponentDataFromEntity<MovementDirectionInputComponent>();
 var Player = PlayerPhysics;

 var readMovementJobHandle = Job.WithName("ReadMovementInput")
     .WithCode(() =>
     {
         var movementInput = ReadMovementComponent[Player];
         movementInput.NewValue = MovementDirection;
         ecb_concurrent.SetComponent<MovementDirectionInputComponent>(0, Player, movementInput);
     }
 ).Schedule(Dependency);

 Dependency = JobHandle.CombineDependencies(Dependency, readMovementJobHandle); */

// * version 3
/* Entities.WithName("ReadMovementInput")
    .WithAll<PlayerPhysicsTag>()
    .WithNone<Prefab>()
    .ForEach(
        (ref MovementDirectionInputComponent movementInput) =>
        {
            movementInput.NewValue = MovementDirection;
        }
    ).Schedule(); */