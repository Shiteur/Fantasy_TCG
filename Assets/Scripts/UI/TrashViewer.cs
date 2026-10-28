using TMPro;
using UnityEngine;

public class TrashViewer : MonoBehaviour
{
    [Header("Fenêtre")]
    [SerializeField] private GameObject trashPanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform cardsContainer;

    [Header("Cartes")]
    [SerializeField] private CardView cardPrefab;

    public void OpenTrash(TrashManager source, string title)
    {
        if (source == null || trashPanel == null ||
            cardsContainer == null || cardPrefab == null)
        {
            Debug.LogError("TrashViewer : référence manquante !");
            return;
        }

        // Supprime les anciennes copies affichées.
        foreach (Transform child in cardsContainer)
        {
            Destroy(child.gameObject);
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        // Crée une copie visuelle de chaque carte de la Trash choisie.
        foreach (CardData data in source.GetCardData())
        {
            if (data == null) continue;

            CardView copy = Instantiate(cardPrefab, cardsContainer);
            copy.SetCardData(data);
            copy.SetFaceDown(false);
            copy.transform.localScale = Vector3.one;
            copy.transform.localRotation = Quaternion.identity;
        }

        trashPanel.SetActive(true);
    }

    public void CloseTrash()
    {
        if (trashPanel != null)
        {
            trashPanel.SetActive(false);
        }
    }
}
