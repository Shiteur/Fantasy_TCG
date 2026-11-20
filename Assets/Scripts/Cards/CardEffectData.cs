using UnityEngine;

[System.Serializable]
public class CardEffectData
{
    public EffectTrigger trigger;
    public CardEffectType effectType;

    [Min(1)]public int value = 1;
    [Min(0)]public int conditionValue = 0;

    public ActivationCostType activationCost;
    [Min(0)] public int activationManaCost = 0;

    public bool mustResolve = true;
}
