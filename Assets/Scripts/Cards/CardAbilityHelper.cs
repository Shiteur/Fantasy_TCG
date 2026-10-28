using UnityEngine;

public static class CardAbilityHelper
{
    public static bool HasAbility(CardView cardView, CardAbility ability)
    {
        if (cardView == null || cardView.Data == null)
            return false;

        CardAbility[] abilities = cardView.Data.abilities;

        if(abilities == null)
            return false;

        foreach (CardAbility currentAbility in abilities)
        {
            if(currentAbility == ability)
                return true;
        }
        return false;
    }

    public static bool HasAbility(UnitCardState unit, CardAbility ability)
    {
        if (unit == null)
            return false;

        return HasAbility(unit.GetComponent<CardView>(), ability);
    }
}
