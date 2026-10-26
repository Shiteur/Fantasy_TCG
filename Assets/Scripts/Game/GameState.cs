using UnityEngine;

[System.Serializable]
public class GameState
{
    public PlayerState playerOne;
    public PlayerState playerTwo;

    public int currentPlayerIndex;
    public bool isGameOver;

    public PlayerState CurrentPlayer
    {
        get { 
            return currentPlayerIndex == 0 ? playerOne : playerTwo;
        }
    }

    public PlayerState Opponent
    {
        get { 
            return currentPlayerIndex == 0 ? playerTwo : playerOne;
        }
    }
}
