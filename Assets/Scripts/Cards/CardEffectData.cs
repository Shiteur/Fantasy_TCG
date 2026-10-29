using UnityEngine;

[System.Serializable]
public class CardEffectData
{
    public EffectTrigger trigger;
    public CardEffectType effectType;

    [Min(1)]public int value = 1;
}
