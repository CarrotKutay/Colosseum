using Unity.Entities;
using Unity.Mathematics;
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
    private Vector2 PreviousAnimationMoveVector;
    private Vector2 Momentum;
    private Vector2 TransitionVectorValue;
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
        activateDodgeEvents();
    }
    private void activateDodgeEvents()
    {
        playerAction.Player.DodgeForwardPressAsButton.performed += _ => readDodgeAnimation();
    }
    private void activatingMovementEvents()
    {
        // basic movement
        playerAction.Player.MoveDownPressAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveDownReleaseAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveUpPressAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveUpReleaseAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveRightPressAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveRightReleaseAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveLeftPressAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveLeftReleaseAsButton.performed +=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());

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
            debugMovementAnimation.testDodgeTriggered();
        }
        animateMovement();
    }

    private Vector2 transitionIntoAnimationValue(float transitionSpeed = .002f)
    {
        if (AnimationMoveVector == Vector2.zero) { return Vector2.zero; }

        var currentX = playerAnimator.GetFloat("PosX");
        var currentY = playerAnimator.GetFloat("PosY");

        Vector2 directionTransformationValue = AnimationMoveVector;
        //Debug.Log("accessing x animation movement -> " + TransitionVectorValue.x);

        if (AnimationMoveVector.x != currentX)
        {
            // get direction sign from animtion direction to move into
            var transitionDirection = AnimationMoveVector.x < 0 ? -1 : 1;
            // max negative move animation direction
            var maxMoveDir = AnimationMoveVector.x < 0 ? AnimationMoveVector.x : AnimationMoveVector.x * -1f;
            // functionality to control growth of input value transition
            directionTransformationValue.x = math.clamp((math.abs(currentX) + transitionSpeed + Momentum.x) * transitionDirection, maxMoveDir, -maxMoveDir);
        }
        if (AnimationMoveVector.y != currentY)
        {
            // get direction sign from animtion direction to move into
            var transitionDirection = AnimationMoveVector.y < 0 ? -1 : 1;
            // max negative move animation direction
            var maxMoveDir = AnimationMoveVector.y < 0 ? AnimationMoveVector.y : AnimationMoveVector.y * -1f;
            // functionality to control growth of input value transition
            directionTransformationValue.y = math.clamp((math.abs(currentY) + transitionSpeed + Momentum.y) * transitionDirection, maxMoveDir, -maxMoveDir);
        }

        return directionTransformationValue;
    }

    private void compareToPreviousAnimation()
    {
        if (AnimationMoveVector.x == PreviousAnimationMoveVector.x) { Momentum.x += Time.deltaTime; Momentum.x *= 2; }
        else { Momentum.x = 0; }

        if (AnimationMoveVector.y == PreviousAnimationMoveVector.y) { Momentum.y += Time.deltaTime; Momentum.y *= 2; }
        else { Momentum.y = 0; }
    }

    /// <summary>
    /// <para> Keep Movement input received up to date inside local variable AnimationMoveVector </para>
    /// </summary>
    private void readMovementInput(Vector2 direction)
    {
        // before changing the movement animation vector,
        // we compare it to the previous animation transition vector.
        // If it is the same animation vector we gain momentum while
        // performing the animation  
        compareToPreviousAnimation();
        AnimationMoveVector = direction;
    }

    private void readDodgeAnimation()
    {
        playerAnimator.SetTrigger("TriggerDodge");
    }

    private void animateMovement()
    {
        var transformationVector = transitionIntoAnimationValue();
        playerAnimator.SetFloat("PosX", transformationVector.x);
        playerAnimator.SetFloat("PosY", transformationVector.y);
    }

    #region onDestroy
    private void OnDestroy()
    {
        deactivateMovementEvents();
        deactivateDodgeEvents();
    }

    private void deactivateMovementEvents()
    {
        playerAction.Player.MoveDownPressAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveDownReleaseAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveUpPressAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveUpReleaseAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveRightPressAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveRightReleaseAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveLeftPressAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
        playerAction.Player.MoveLeftReleaseAsButton.performed -=
            _ => readMovementInput(playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>());
    }

    private void deactivateDodgeEvents()
    {
        playerAction.Player.DodgeForwardPressAsButton.performed -= _ => readDodgeAnimation();
    }

    #endregion
}
