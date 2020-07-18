using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Transforms;
[UpdateBefore(typeof(PlayerInputTurnSystem))]
public class InputSystem : SystemBase
{
    private PlayerAction PlayerInput;
    private EntityManager Manager;
    private Entity PlayerPhysics;
    private float3 LookDirection;
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;

    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World
                .DefaultGameObjectInjectionWorld
                .GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();


        PlayerInput = new PlayerAction();

        Manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;

        activateDodgeEvents();
        activatingMovementEvents();
        activateLookEvents();
    }

    protected override void OnDestroy()
    {
        deactivateDodgeEvents();
        deactivatingMovementEvents();
        deactivateLookEvents();
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
        var ecb_concurrent = endSimulationEntityCommandBufferSystem
            .CreateCommandBuffer()
            .ToConcurrent();
        var ReadMovementComponent = GetComponentDataFromEntity<MovementDirectionInputComponent>();
        var Player = PlayerPhysics;

        Job.WithName("ReadMovementInput")
            .WithCode(() =>
            {
                var movementInput = ReadMovementComponent[Player];
                movementInput.NewValue = MovementDirection;
                ecb_concurrent.SetComponent<MovementDirectionInputComponent>(0, Player, movementInput);
            }
        ).Schedule();

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
    }

    public struct ReadMovementInputJob : IJob
    {
        public float2 newMovementInput;
        public Entity Player;
        public ComponentDataFromEntity<MovementDirectionInputComponent> GetMovementInput;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        public void Execute()
        {
            var movementInput = GetMovementInput[Player];
            movementInput.NewValue = newMovementInput;
            EntityCommandBuffer.SetComponent<MovementDirectionInputComponent>(0, Player, movementInput);
        }
    }

    #endregion read movement input

    private void readDodgeInput()
    {

    }
    #region read look input
    private void readLookInput(float2 Direction)
    {
        var playerPosition = GetComponentDataFromEntity<Translation>(true)[PlayerPhysics].Value;
        var playerScreenPosition = new float3(UnityEngine.Camera.main.WorldToScreenPoint(playerPosition));
        LookDirection = new float3(Direction.x - playerScreenPosition.x, 0, Direction.y - playerScreenPosition.y);
    }

    public struct ReadLookInputJob : IJob
    {
        public float3 newLookInput;
        public Entity Player;
        public ComponentDataFromEntity<LookDirectionInputComponent> GetLookInput;
        public EntityCommandBuffer.Concurrent EntityCommandBuffer;
        public void Execute()
        {
            var lookInput = GetLookInput[Player];
            lookInput.Value = newLookInput;
            EntityCommandBuffer.SetComponent<LookDirectionInputComponent>(0, Player, lookInput);
        }
    }
    #endregion lookInput

    #endregion


    protected override void OnUpdate()
    {
        var maxDurationValue = new float3(.8f, 0, .8f);
        var DeltaTime = Time.DeltaTime;
        var ecb_concurrent = endSimulationEntityCommandBufferSystem
            .CreateCommandBuffer()
            .ToConcurrent();

        // * scheduling job to update the inputLook direction
        var readLookInputJob = new ReadLookInputJob
        {
            newLookInput = LookDirection,
            Player = PlayerPhysics,
            EntityCommandBuffer = ecb_concurrent,
            GetLookInput = GetComponentDataFromEntity<LookDirectionInputComponent>(),
        };

        var lookHandle = readLookInputJob.Schedule(Dependency);
        Dependency = JobHandle.CombineDependencies(Dependency, lookHandle);

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

        CompleteDependency();
    }
}
