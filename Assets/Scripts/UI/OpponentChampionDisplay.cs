using UnityEngine;

public class OpponentChampionDisplay : MonoBehaviour
{
    [Header("Champion adverse")]
    [SerializeField] private CardData championData;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform championZone;

    private CardView championView;

    public CardData ChampionData => championData;
    public CardView ChampionView => championView;

    public int CurrentAttack { get; private set; }

    public void ModifyAttack(int amount)
    {
        CurrentAttack += amount;
        Debug.Log($"Attaque du champion adverse : {CurrentAttack}");
    }

    public void ShowChampion()
    {
        if (championData == null)
        {
            Debug.LogError("Aucun champion assigné à l'adversaire!");
            return;
        }

        if(championData.cardType != CardType.Champion)
        {
            Debug.LogError("Le champion adverse assigné n'est pas de type Champion !");
            return;
        }

        if (cardPrefab == null || championZone == null)
        {
            Debug.LogError("CardPrefab ou ChampionZone adverse n'est pas assigné !");
            return;
        }

        // Instancier une carte unique du champion
        if (championView != null)
        {
            Destroy(championView.gameObject);
        }

        championView = Instantiate(cardPrefab, championZone);
        championView.SetCardData(championData);
        CurrentAttack = championData.attack;

        if (championView.GetComponent<OpponentChampionClickHandler>() == null)
        {
            championView.gameObject.AddComponent<OpponentChampionClickHandler>();
        }

        championView.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        Debug.Log($"Champion adverse affiché: {championData.cardName}");
    }
}
