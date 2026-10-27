using UnityEngine;

public static class LifeManager
{
    public static bool TakeLifeDamage(Transform lifeZone)
    {
        if(lifeZone == null)
        {
            Debug.LogError("LifeManager : la zone de vie est amquante.");
            return false;
        }

        if(lifeZone.childCount == 0)
        {
            Debug.Log("LifeManager : la zone de vie est vide.");
            return false;
        }

        Transform lastCard = lifeZone.GetChild(lifeZone.childCount - 1);
        lastCard.SetParent(null);
        return true;

    }
}
