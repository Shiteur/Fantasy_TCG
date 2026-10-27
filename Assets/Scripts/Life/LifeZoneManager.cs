
using UnityEngine;

public class LifeZoneManager : MonoBehaviour
{
    [Header("Données du Champion")]
    [SerializeField] private CardData championData;

    [Header("Deck et main du propriétaire")]
    [SerializeField] private PlayerDeck playerDeck;
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private bool revealRecoveredCard = true;

    [Header("Affichage")]
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform lifeZone;

    [Header("Orientation des cartes")]
    [SerializeField] private float cardRotation = 0f;

    public int RemainingLives => lifeZone != null ? lifeZone.childCount : 0;

    public void InitializeLife()
    {
        if (championData == null || playerDeck == null || cardPrefab == null || lifeZone == null)
        {
            Debug.LogError("LifeZoneManager : référence manquante !");
            return;
        }

        for (int i = 0; i < championData.initialLifeCard; i++)
        {
            CardData lifeCardData = playerDeck.DrawLifeCard();

            if (lifeCardData == null)
            {
                Debug.LogWarning("Le deck ne contient plus assez de cartes pour la vie.");
                break;
            }

            CardView card = Instantiate(cardPrefab, lifeZone);
            card.SetCardData(lifeCardData);
            card.SetFaceDown(true);

            card.transform.localScale = Vector3.one;
            card.transform.localRotation = Quaternion.Euler(0f, 0f, cardRotation);
        }

        Debug.Log($"Pile de vie initialisée : {RemainingLives}");
    }

    public bool TakeLifeDamage()
    {
        if (lifeZone == null || lifeZone.childCount == 0)
        {
            return false;
        }

        if (playerHand == null)
        {
            Debug.LogError("LifeZoneManager : la main du joueur est manquante !");
            return false;
        }

        int lastIndex = lifeZone.childCount - 1;
        CardView lifeCard = lifeZone.GetChild(lastIndex).GetComponent<CardView>();

        if (lifeCard == null || lifeCard.Data == null)
        {
            Debug.LogError("La carte de vie ne possède pas de CardView ou CardData.");
            return false;
        }

        CardData recoveredCard = lifeCard.Data;

        Destroy(lifeCard.gameObject);
        playerHand.AddCard(recoveredCard);
        
        if (revealRecoveredCard)
        {
            Debug.Log("Une carte de vie a rejoint la main.");
        }

        Debug.Log("Une carte de vie a rejoint la main.");
        return true;
    }
}
