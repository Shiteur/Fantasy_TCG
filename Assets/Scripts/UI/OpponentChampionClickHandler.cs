using UnityEngine;
using UnityEngine.EventSystems;

public class OpponentChampionClickHandler : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if(CombatManager.Instance != null)
        {
            CombatManager.Instance.SelectOpponentChampion();
        }
    }
}
