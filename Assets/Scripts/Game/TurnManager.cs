using UnityEngine;

public class TurnManager
{
    public int TurnNumber { get; private set; } = 1;
    public int CurrrentPlayerIndex { get; private set; } = 0;

    public bool IsFirstTurn => TurnNumber == 1;

    public void EndTurn()
    {
        CurrrentPlayerIndex = 1 - CurrrentPlayerIndex; // cycle between 0 and 1 for two players

        if (CurrrentPlayerIndex == 0)
        {
            TurnNumber++;
        }
    }
}
