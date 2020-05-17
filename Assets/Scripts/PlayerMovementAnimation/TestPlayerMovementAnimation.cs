using UnityEngine;

public class TestPlayerMovementAnimation
{
    private PlayerAction playerAction;
    public TestPlayerMovementAnimation(PlayerAction inputActions)
    {
        playerAction = inputActions;
    }

    public void testActionAccessPoints()
    {
        Debug.Log("Move Pressed event triggered: " + playerAction.Player.MovePress.triggered);
        Debug.Log("Move Released event triggered: " + playerAction.Player.MoveRelease.triggered);
    }

    public PlayerAction PlayerAction { get => playerAction; set => playerAction = value; }
}
