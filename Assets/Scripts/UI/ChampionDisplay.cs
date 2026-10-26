using UnityEngine;

public class ChampionDisplay : MonoBehaviour
{
    [Header("Champion ")]
    [SerializeField] private CardData championData;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform championZone;

    private CardView championView;

    public void ShowChampion() {
        if (championData == null) {
            Debug.LogError("Aucun champion assigné !");
            return;
        }

        if (cardPrefab == null || championZone == null) {
            Debug.LogError("CardPrefab ou ChampionZone n'est pas assigné !");
            return;
        }

        // Instancier une carte unique du champion
        if (championView != null) {
            Destroy(championView.gameObject);
        }

        championView = Instantiate(cardPrefab, championZone);
        championView.SetCardData(championData);
    }

}
