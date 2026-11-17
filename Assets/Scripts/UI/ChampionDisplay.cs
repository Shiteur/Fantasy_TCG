using UnityEngine;

public class ChampionDisplay : MonoBehaviour
{
    [Header("Champion ")]
    [SerializeField] private CardData championData;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform championZone;

    private CardView championView;

    private UnitCardState championState;
    public UnitCardState ChampionState => championState;
    public CardView ChampionView => championView;

    public int CurrentAttack { get; private set; }

    public void ModifyAttack(int amount)
    {
        CurrentAttack += amount;
        if (championView != null)
        {
            championView.UpdateAttackText(CurrentAttack);
        }
        Debug.Log($"Attaque du champion : {CurrentAttack}");
    }
    public void ResetAttack()
    {
        if (championView == null || championView.Data == null)
            return;

        CurrentAttack = championView.Data.attack;
        championView.UpdateAttackText(CurrentAttack);

        Debug.Log($"Attaque du champion réinitialisée : {CurrentAttack}");
    }
    public void ShowChampion() {
        if (championData == null) {
            Debug.LogError("Aucun champion assigné !");
            return;
        }

        if (championData.cardType != CardType.Champion)
        {
            Debug.LogError("Le champion assigné n'est pas de type Champion !");
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

        if (championView.GetComponent<ChampionClickHandler>() == null)
        {
            championView.gameObject.AddComponent<ChampionClickHandler>();
        }

        CurrentAttack = championData.attack;
        championState = championView.GetComponent<UnitCardState>();

        if (championState == null)
        {
            championState = championView.gameObject.AddComponent<UnitCardState>();
        }

        championState.Initialize(0);
    }

}
