using UnityEngine;
using Unity.Mathematics;

public class TestPlayerMovementAnimation
{
    private PlayerAction playerAction;
    public TestPlayerMovementAnimation(PlayerAction inputActions)
    {
        playerAction = inputActions;
    }

    public void testActionAccessPoints()
    {
        Debug.Log("Move Pressed event triggered: " + movementPressEventTriggered());
        Debug.Log("Move Released event triggered: " + movementReleasedEventTriggered());
    }

    public void testDodgeTriggered()
    {
        Debug.Log("Dodge input triggered: " + playerAction.Player.DodgeForwardPressAsButton.triggered);
    }

    public void testDirectionSpeedInput()
    {
        Debug.Log("Speed at x: " + playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>().x);
        Debug.Log("Speed at y: " + playerAction.Player.MoveCompositeAsValue.ReadValue<Vector2>().y);
    }

    private bool movementPressEventTriggered()
    {
        return
            playerAction.Player.MoveLeftPressAsButton.triggered
            || playerAction.Player.MoveRightPressAsButton.triggered
            || playerAction.Player.MoveUpPressAsButton.triggered
            || playerAction.Player.MoveDownPressAsButton.triggered;
    }

    private bool movementReleasedEventTriggered()
    {
        return
            playerAction.Player.MoveLeftReleaseAsButton.triggered
            || playerAction.Player.MoveRightReleaseAsButton.triggered
            || playerAction.Player.MoveUpReleaseAsButton.triggered
            || playerAction.Player.MoveDownReleaseAsButton.triggered;
    }

    public PlayerAction PlayerAction { get => playerAction; set => playerAction = value; }
}
