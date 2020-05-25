using Unity.Entities;
using Unity.Collections;
using Unity.Physics;
using UnityEngine;
using Unity.Mathematics;

public class InputSystem : SystemBase
{
    private PlayerAction PlayerInput;
    private EntityManager Manager;
    private float2 OldMovementDirectionOrder;
    public NativeArray<Vector2> MovementDirectionOrder;

    protected override void OnCreate()
    {
        PlayerInput = new PlayerAction();
        MovementDirectionOrder = new NativeArray<Vector2>(1, Allocator.Persistent);
        Manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;
        activateDodgeEvents();
        activatingMovementEvents();
    }

    protected override void OnDestroy()
    {
        MovementDirectionOrder.Dispose();
        deactivateDodgeEvents();
        deactivatingMovementEvents();
    }

    #region Activation Behaviour
    protected override void OnStartRunning()
    {
        PlayerInput.Enable();

        var Player = GetSingletonEntity<PlayerTag>();
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

    private void deactivateDodgeEvents()
    {
        PlayerInput.Player.DodgeForwardPressAsButton.performed -= _ => readDodgeInput();
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
    private void readMovementInput(Vector2 MovementDirection)
    {
        MovementDirectionOrder[0] = MovementDirection;
    }

    private void readDodgeInput()
    {

    }

    #endregion


    protected override void OnUpdate()
    {
        var maxDurationValue = new float3(.8f, 0, .8f);
        var nextMoveOrder = new float2(MovementDirectionOrder[0]);
        var oldOrder = new NativeArray<float2>(1, Allocator.TempJob);
        oldOrder[0] = OldMovementDirectionOrder;
        var DeltaTime = Time.DeltaTime;
        var handle = Entities.WithName("GetInputHoldDuration")
            .WithAll<PlayerTag>()
            .WithNone<Prefab>()
            .ForEach(
                (ref InputHoldComponent InputHoldComponent) =>
                {
                    if (nextMoveOrder.Equals(oldOrder[0])
                        && !(nextMoveOrder.Equals(float2.zero)))
                    {
                        var holdValue = DeltaTime * 2; // increased value to make startup velocity a bit faster 

                        InputHoldComponent.Value.x += holdValue;
                        InputHoldComponent.Value.z += holdValue;
                        InputHoldComponent.Value = math.clamp(InputHoldComponent.Value, -maxDurationValue, maxDurationValue);
                    }
                    else
                    { InputHoldComponent.Value = float3.zero; }

                    oldOrder[0] = nextMoveOrder;
                }
        ).Schedule(Dependency);
        handle.Complete();
        OldMovementDirectionOrder = oldOrder[0];
        oldOrder.Dispose();
    }
}
