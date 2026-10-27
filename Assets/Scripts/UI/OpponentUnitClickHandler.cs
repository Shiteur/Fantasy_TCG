using UnityEngine;
using UnityEngine.EventSystems;

public class OpponentUnitClickHandler : MonoBehaviour, IPointerClickHandler
{
    private UnitCardState unitState;

    private void Awake()
    {
        unitState = GetComponent<UnitCardState>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        unitState = GetComponent<UnitCardState>();

        if (unitState == null)
        {
            Debug.LogWarning("Cette unité n'a pas de UnitCardState attaché.");
            return;
        }

        if(CombatManager.Instance == null)
        {
            Debug.LogWarning("CombatManager instance is null.");
            return;
        }

        CombatManager.Instance.SelectTarget(unitState);
    }
}
