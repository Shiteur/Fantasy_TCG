using UnityEngine;
using UnityEngine.EventSystems;

public class TrashCardClickForwarder : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        TrashClickHandler handler =
            GetComponentInParent<TrashClickHandler>();

        if (handler != null)
        {
            handler.OpenTrash();
        }
    }
}
