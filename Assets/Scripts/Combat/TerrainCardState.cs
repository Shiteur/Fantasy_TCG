using System.Collections.Generic;
using UnityEngine;

public class TerrainCardState : MonoBehaviour
{
    [SerializeField] private bool isExhausted;

    private Quaternion baseRotation;

    private HashSet<int> usedOncePerTurnEffects = new HashSet<int>();

    public bool IsExhausted => isExhausted;

    public void Exhaust()
    {
        isExhausted = true;
        UpdateVisual();
    }
    public void Ready()
    {
        isExhausted = false;
        UpdateVisual();
    }
    public void Initialize()
    {
        baseRotation = transform.localRotation;
        isExhausted = false;
        usedOncePerTurnEffects.Clear();

        UpdateVisual();
    }
    public bool HasUsedEffectThisTurn(int effectIndex)
    {
        return usedOncePerTurnEffects.Contains(effectIndex);
    }
    public void MarkEffectAsUsed(int effectIndex)
    {
        usedOncePerTurnEffects.Add(effectIndex);
    }
    public void ResetOncePerTurnEffects()
    {
        usedOncePerTurnEffects.Clear();
    }
    private void UpdateVisual()
    {
        float exhaustionAngle = isExhausted ? 90f : 0f;

        transform.localRotation =
            baseRotation * Quaternion.Euler(0f, 0f, exhaustionAngle);
    }
}
