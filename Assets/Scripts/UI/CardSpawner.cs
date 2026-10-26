using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform handPanel;
    [SerializeField] private CardData cardToDisplay;

    public void SpawnCard()
    {
        CardView card= Instantiate(cardPrefab, handPanel);

        card.SetCardData(cardToDisplay);
    }
}
