using UnityEngine;

public class UnitSlectionManager : MonoBehaviour
{
    public static UnitSlectionManager Instance { get; private set; }

    public UnitCardState SelectedUnit { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public void SelectUnit(UnitCardState unit)
    {
        if (unit == null) {
            return;
        }

        if(SelectedUnit == unit)
        {
            ClearSelection();
            return;
        }

        if(SelectedUnit != null) SelectedUnit.GetComponent<CardView>()?.SetSelected(false);

        SelectedUnit = unit;

        SelectedUnit.GetComponent<CardView>()?.SetSelected(true);

        Debug.Log($"Unité attaquante sélectionnée : {SelectedUnit.GetComponent<CardView>()?.Data.cardName}");
    }

    public void ExhaustUnit()
    {
        if (SelectedUnit == null)
        {
            Debug.LogWarning("Aucune unité sélectionnée pour l'épuiser.");
            return;
        }
        SelectedUnit.Exhaust();
        ClearSelection();
    }

    public void ClearSelection()
    {
        if(SelectedUnit != null)
        {
            SelectedUnit.GetComponent<CardView>()?.SetSelected(false);
        }

        SelectedUnit = null;
    }
}
