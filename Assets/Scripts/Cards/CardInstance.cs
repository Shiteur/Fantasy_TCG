using System;
using UnityEngine;

[Serializable]
public class CardInstance 
{
    public string instanceId;
    public CardData data;

    public int currentHealth;
    public CardMode mode;
    public bool canAttack;

    public CardInstance(CardData cardData)
    {
        instanceId = Guid.NewGuid().ToString();
        data = cardData;

        currentHealth = data.maxHealth;
        mode = CardMode.Active;
        canAttack = false;
    }
}
