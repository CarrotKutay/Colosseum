using Unity.Entities;
using UnityEngine;

public class PlayerMovementAnimationSystem : MonoBehaviour
{
    #region Local Variables
    [SerializeField] private float MovementSpeed;
    [SerializeField] [Range(0, 1)] private float MovementAnimationChangeFactor;
    private EntityManager manager;
    private Entity playerEntity;
    private PlayerAction playerAction;
    private Animator playerAnimator;
    private Vector2 AnimationMoveVector;
    [SerializeField] private bool DebugOn = false;
    private TestPlayerMovementAnimation debugMovementAnimation;

    #endregion

    #region Start functionality
    private void Awake()
    {
        playerAction = new PlayerAction();
        manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;
    }

    private void Start()
    {
        playerAnimator = GetComponentInChildren<Animator>();
        debugMovementAnimation = new TestPlayerMovementAnimation(playerAction);

        /* playerEntity = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>()
            .GetSingletonEntity<PlayerTag>();
        */

        activatingMovementEvents();
    }

    private void activatingMovementEvents()
    {
        playerAction.Player.MoveDownPressAsButton.performed +=
            _ => readMoveInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveDownReleaseAsButton.performed +=
            _ => readMoveInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
    }

    #endregion

    #region Activation Behaviour
    private void OnEnable()
    {
        playerAction.Enable();
    }

    private void OnDisable()
    {
        playerAction.Disable();
    }

    #endregion
    private void Update()
    {
        if (DebugOn)
        {
            // testing access points
            debugMovementAnimation.testActionAccessPoints();
        }
        animateMovement();
    }

    private void readMoveInput(Vector2 direction)
    {
        var currentX = playerAnimator.GetFloat("PosX");
        var currentY = playerAnimator.GetFloat("PosY");


        AnimationMoveVector = direction == Vector2.zero ?
            new Vector2(currentX, currentY) * MovementAnimationChangeFactor :
            direction * MovementAnimationChangeFactor + new Vector2(currentX, currentY);

        AnimationMoveVector.x = Mathf.Clamp(AnimationMoveVector.x, -1f, 1f);
        AnimationMoveVector.y = Mathf.Clamp(AnimationMoveVector.y, -1f, 1f);
    }

    private void animateMovement()
    {
        playerAnimator.SetFloat("PosX", AnimationMoveVector.x);
        playerAnimator.SetFloat("PosY", AnimationMoveVector.y);
    }

    /* private void OnDestroy()
    {
        playerAction.Player.Move.performed -= context => readMoveInput(context.ReadValue<Vector2>());
        playerAction.Player.Move.canceled -= context => readMoveInput(context.ReadValue<Vector2>());
    } */
}
