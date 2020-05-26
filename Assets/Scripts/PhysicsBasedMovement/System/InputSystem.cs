using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEngine;
using Unity.Mathematics;

public class InputSystem : SystemBase
{
    private PlayerAction PlayerInput;
    private EntityManager Manager;
    private float2 OldMovementDirectionInput;
    private Entity Player;

    protected override void OnCreate()
    {
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

        Player = GetSingletonEntity<PlayerTag>();
        Manager.AddComponentData(Player, new InputHoldComponent { Value = 0 });
        var buffer = GetBufferFromEntity<LinkedEntityGroup>()[Player].Reinterpret<Entity>();
        foreach (var entity in buffer)
        {
            if (HasComponent<PhysicsVelocity>(entity))
            {
                var PlayerPhysics = entity;
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
    private void readMovementInput(float2 MovementDirection)
    {
        var movementInput = GetComponent<MovementDirectionInputComponent>(Player);
        movementInput.NewValue = MovementDirection;
        SetComponent<MovementDirectionInputComponent>(Player, movementInput);
    }

    private void readDodgeInput()
    {

    }

    private void readLookInput(float2 Direction)
    {
        var lookInput = GetComponent<LookDirectionInputComponent>(Player);
        lookInput.Value = Direction;
        SetComponent<LookDirectionInputComponent>(Player, lookInput);
    }

    #endregion


    protected override void OnUpdate()
    {
        var maxDurationValue = new float3(.8f, 0, .8f);

        var DeltaTime = Time.DeltaTime;
        var handle = Entities.WithName("GetInputHoldDuration")
            .WithAll<PlayerTag>()
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
        handle.Complete();
    }
}
