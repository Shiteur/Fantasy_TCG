using UnityEngine;

public class UnitCardState : MonoBehaviour
{
    [SerializeField] private bool isExhausted;
    [SerializeField] private int summonedOnTurn;

    public bool IsExhausted => isExhausted;
    public int SummonedOnTurn => summonedOnTurn;

    public bool CanAttackOnTurn(int currentTurn)
    {
        return !isExhausted && currentTurn > summonedOnTurn;
    }

    public void Initialize(int currentTurn)
    {
        summonedOnTurn = currentTurn;
        isExhausted = false;
        UpdateVisual();
    }

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

    private void UpdateVisual()
    {
        transform.localRotation = IsExhausted
            ? Quaternion.Euler(0f, 0f, 90f)
            : Quaternion.identity;
    }
}
