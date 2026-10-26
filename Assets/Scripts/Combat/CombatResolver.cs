using UnityEngine;

public static class CombatResolver
{
    public static int CalculateDamage(int attackerAttack, int defenderAttack)
    {
        int damage = attackerAttack - defenderAttack;
        return damage > 0 ? damage : 0;
    }

    public static bool IsDestored(int health)
    {
        return health <= 0;
    }
}
