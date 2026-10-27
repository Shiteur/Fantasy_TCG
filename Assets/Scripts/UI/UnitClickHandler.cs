using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardView))]
public class UnitClickHandler : MonoBehaviour, IPointerClickHandler
{

    public void OnPointerClick(PointerEventData enventData) {
        UnitCardState unitState = GetComponent<UnitCardState>();

        if (unitState == null)
        {
            Debug.LogWarning("Cette carte n'a pas de UnitCardState.");
            return;
        }

        if(UnitSlectionManager.Instance == null)
        {
            Debug.LogWarning("UnitSlectionManager n'est pas présent dans la scène.");
            return;
        }

        UnitSlectionManager.Instance?.SelectUnit(unitState);
    }
}
