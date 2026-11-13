using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CardView))]
public class ChampionClickHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if(CombatManager.Instance == null)
        {
            Debug.LogWarning("CombatManager n'est pas présent dans la scène.");
            return;
        }

        CombatManager.Instance.SelectPlayerChampion();
    }
}
