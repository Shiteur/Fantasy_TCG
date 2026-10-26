using System.Collections.Generic;
using UnityEngine;

public static class LifeManager
{
    public static bool TakeLifeDamage(List<CardInstance> lifeSatck, List<CardInstance> hand)
    {
        if(lifeSatck.Count == 0) return false;

        int lastIndex = lifeSatck.Count - 1;
        CardInstance lastCard = lifeSatck[lastIndex];
        lifeSatck.RemoveAt(lastIndex);
        hand.Add(lastCard);
        return true;
    }
}
