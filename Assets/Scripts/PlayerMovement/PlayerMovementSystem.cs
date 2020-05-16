using Unity.Entities;
using UnityEngine;

public class PlayerMovementSystem : MonoBehaviour
{
    [SerializeField] private float MovementSpeed;
    [SerializeField] [Range(0, 1)] private float MovementAnimationChangeFactor;
    private EntityManager manager;
    private Entity playerEntity;
    private PlayerAction playerAction;
    private Animator playerAnimator;
    private Vector2 AnimationMoveVector;

    private void Awake()
    {
        playerAction = new PlayerAction();
        playerAnimator = GetComponentInChildren<Animator>();
        manager = World
            .DefaultGameObjectInjectionWorld
            .EntityManager;
    }

    private void Start()
    {
        /* playerEntity = World
            .DefaultGameObjectInjectionWorld
            .GetOrCreateSystem<EndInitializationEntityCommandBufferSystem>()
            .GetSingletonEntity<PlayerTag>();
        */

        /* playerAction.Player.Move.performed += context => readMoveInput(context.ReadValue<Vector2>());
        playerAction.Player.Move.canceled += context => readMoveInput(context.ReadValue<Vector2>()); */
    }

    private void Update()
    {

        readMoveInput();

        animateMovement();

        Debug.Log("Move event triggered: " + playerAction.Player.Move.triggered);
    }

    private void OnEnable()
    {
        playerAction.Enable();
    }

    private void OnDisable()
    {
        playerAction.Disable();
    }

    private void readMoveInput()
    {
        var direction = playerAction.Player.Move.ReadValue<Vector2>();

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
