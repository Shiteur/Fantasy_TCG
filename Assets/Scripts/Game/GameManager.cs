using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Deck et main")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayerDeck playerDeck;

    [Header("Plateau")]
    [SerializeField] private ChampionDisplay championDisplay;
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private Transform playerUnitZone;

    [Header("Configuration de la partie")]
    [SerializeField] private bool playerGoesFirst = true;
    [SerializeField] private int startingHandSize = 3;

    private bool gameStarted;
    private int turnNumber = 0;

    public int TurnNumber => turnNumber;
    public bool CanAttack => turnNumber > 1;


    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        if(gameStarted) return;

        if(playerHand == null || playerDeck == null || championDisplay == null || manaSystem == null)
        {
            Debug.LogError("GameManager n'est pas correctement configuré !");
            return;
        }

        gameStarted = true;

        // 1. Prépare le deck principal.
        playerDeck.InitializeDeck();

        //2. Prépare le deck de mana.
        manaSystem.Initialize();

        // 3. Affiche le champion du joueur.
        championDisplay.ShowChampion();

        // 4. Distribue la main de départ.
        for (int i = 0; i < startingHandSize; i++)
        {
            DrawCard();
        }

        // 5. Ajoute le mana initial.
        int initialMana = playerGoesFirst ? 1 : 2;
        manaSystem.AddManaCards(initialMana);

        // 6. Remet actif les unités du joueur.
        ReadyAllUnit();

        turnNumber = 1;

        Debug.Log($"Partie commencée. Tour {turnNumber}.");
        Debug.Log($"Attaque autorisée: {CanAttack}");
    }

    public void EndTurn() {
        if(!gameStarted) return;

        //Prépare le prochain tour.
        turnNumber++;

        // Réacitve tout le mana épuisé pour le nouveau tour.
        RefreshMana();

        bool shouldDraw = !(turnNumber == 1 && playerGoesFirst);

        if(shouldDraw)
        {
            DrawCard();
        }

        if(turnNumber > 1)
        {
            manaSystem.AddManaCards(2);
        }

        Debug.Log($"Tour {turnNumber} commencé.");
        Debug.Log($"Attaque autorisée: {CanAttack}");
    }
    public void DrawCard() {
        CardData drawnCard = playerDeck.DrawCard();

        if (drawnCard != null)
        {
            playerHand.AddCard(drawnCard);
        }
    }

    public void PlaySelectedUnit()
    {
        if (!gameStarted)
        {
            Debug.LogWarning("La partie n'a pas commencé.");
            return;
        }

        CardView selectedCard = playerHand.SelectedCard;

        if (selectedCard == null)
        {
            Debug.Log("Sélectionne d'abord une carte dans ta main.");
            return;
        }

        CardData data = selectedCard.Data;

        if (data == null)
        {
            Debug.LogError("La carte sélectionnée n'a pas de données.");
            return;
        }

        if (data.cardType != CardType.Unit)
        {
            Debug.Log("Seules les cartes Unité peuvent être jouées ici.");
            return;
        }

        if (playerUnitZone == null)
        {
            Debug.LogError("La zone d'unités n'est pas assignée.");
            return;
        }

        if (manaSystem.AvailableMana < data.manaCost)
        {
            Debug.Log($"Pas assez de mana pour jouer {data.cardName}");
            return;
        }

        // Payer le coût de la carte.
        bool paid = manaSystem.ExhaustMana(data.manaCost);

        if (!paid)
        {
            Debug.Log("Impossible de payer le coût en mana.");
            return;
        }

        // Retirer la carte de la main.
        playerHand.RemoveCard(selectedCard);

        // Placer la même carte dans la zone d'unités.
        selectedCard.transform.SetParent(playerUnitZone, false);
        selectedCard.transform.localScale = Vector3.one;

        UnitCardState uniteState = selectedCard.GetComponent<UnitCardState>();
        if (uniteState == null)
        {
            uniteState = selectedCard.gameObject.AddComponent<UnitCardState>();
        }
        uniteState.Initialize(turnNumber);

        Debug.Log($"{data.cardName} a été jouée !");
    }


    public void AddOneMana()
    {
        manaSystem.AddManaCards(1);
    }

    public void RefreshMana() { 
        manaSystem.ReadyAllMana();
    }

    public void ExhaustOneMana()
    {
        bool success = manaSystem.ExhaustMana(1);

        if(!success)
        {
            Debug.LogWarning("Pas assez de mana disponible !");
        }
    }

    private void ReadyAllUnit()
    {
        if (playerUnitZone == null) return;

        foreach (Transform unitTransform in playerUnitZone)
        {
            UnitCardState unitState = unitTransform.GetComponent<UnitCardState>();
            if (unitState != null)
            {
                unitState.Ready();
            }

            Debug.Log($"Unité peut attaquer: {unitState.CanAttackOnTurn(turnNumber)}.");
        }
    }

}
