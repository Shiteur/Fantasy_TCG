using UnityEngine;
using UnityEngine.EventSystems;

public class TrashClickHandler : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TrashManager trashManager;
    [SerializeField] private TrashViewer trashViewer;
    [SerializeField] private string trashTitle = "Cartes défaussées";

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenTrash();
    }

    public void OpenTrash()
    {
        if (trashManager == null || trashViewer == null)
        {
            Debug.LogError("TrashClickHandler : référence manquante !");
            return;
        }

        trashViewer.OpenTrash(trashManager, trashTitle);
    }
}
