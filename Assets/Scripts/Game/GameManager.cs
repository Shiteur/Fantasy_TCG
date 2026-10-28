using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Deck et main")]
    [SerializeField] private PlayerHand playerHand;
    [SerializeField] private PlayerDeck playerDeck;
    [SerializeField] private PlayerDeck opponentDeck;

    [Header("Plateau")]
    [SerializeField] private ChampionDisplay championDisplay;
    [SerializeField] private OpponentChampionDisplay opponentChampionDisplay;
    [SerializeField] private ManaSystem manaSystem;
    [SerializeField] private Transform playerUnitZone;
    [SerializeField] private TrashManager playerTrashManager;
    [SerializeField] private OpponentUnitSpawner opponentUnitSpawner;
    [SerializeField] private LifeZoneManager playerLifeManager;
    [SerializeField] private LifeZoneManager opponentLifeManager;

    [Header("Configuration de la partie")]
    [SerializeField] private bool playerGoesFirst = true;
    [SerializeField] private int startingHandSize = 3;
    [SerializeField] private CombatManager combatManager;

    private bool gameStarted;
    private int turnNumber = 0;

    public TurnOwner CurrentTurnOwner { get; private set; } = TurnOwner.Player;
    public bool IsPlayerTurn => CurrentTurnOwner == TurnOwner.Player;

    public int TurnNumber => turnNumber;
    public bool CanAttack => turnNumber > 1;


    void Start()
    {
        StartGame();
    }

    public void StartGame()
    {
        if(gameStarted) return;

        if(playerHand == null || playerDeck == null || championDisplay == null || manaSystem == null || opponentChampionDisplay == null || playerLifeManager == null || opponentLifeManager == null || opponentDeck == null || opponentUnitSpawner == null || combatManager == null || playerUnitZone == null)
        {
            Debug.LogError("GameManager n'est pas correctement configuré !");
            return;
        }

        gameStarted = true;

        // 1. Prépare les decks.
        playerDeck.InitializeDeck();
        opponentDeck.InitializeDeck();

        //2. Prépare le deck de mana du joueur.
        manaSystem.Initialize();

        // 3. Affiche les champions du joueur et de l'adversaire.
        championDisplay.ShowChampion();
        opponentChampionDisplay.ShowChampion();

        // 4. Initialise la pile de vie du joueur et de l'adversaire.
        playerLifeManager.InitializeLife();
        opponentLifeManager.InitializeLife();
        playerDeck.CountCards();
        opponentDeck.CountCards();

        // 5. Distribue la main de départ.
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
        CurrentTurnOwner = playerGoesFirst ? TurnOwner.Player : TurnOwner.Opponent;

        Debug.Log($"Partie commencée. Tour {turnNumber}.");
        Debug.Log($"Attaque autorisée: {CanAttack}");
    }

    public void EndTurn() {
        if(!gameStarted) return;

        if(CurrentTurnOwner == TurnOwner.Player)
        {
            CurrentTurnOwner = TurnOwner.Opponent;
            if(combatManager != null)
            {
                combatManager.ClearCombatSelections();
            }

            Debug.Log("Tour de l'adversaire commencé.");
            GetComponent<OpponentTurnManager>()?.StartOpponentTurn();
        }
        else
        {
            Debug.LogWarning("Le tour adverse doit maintenant se terminer automatiquement.");
        }
    }

    public void EndOpponentTurn()
    {
        if (!gameStarted)
            return;

        if (CurrentTurnOwner != TurnOwner.Opponent)
        {
            Debug.LogWarning("Ce n'est pas le tour de l'adversaire.");
            return;
        }

        CurrentTurnOwner = TurnOwner.Player;
        turnNumber++;

        RefreshMana();
        ReadyAllUnit();

        if (opponentUnitSpawner != null)
            opponentUnitSpawner.ReadyAllOpponentUnits();

        DrawCard();
        manaSystem.AddManaCards(2);

        if (combatManager != null)
            combatManager.ClearCombatSelections();

        Debug.Log($"Tour {turnNumber} du joueur commencé.");
        Debug.Log($"Attaque autorisée : {CanAttack}");
    }

    public void DrawCard() {
        CardData drawnCard = playerDeck.DrawCard();

        if (drawnCard != null)
        {
            playerHand.AddCard(drawnCard);
            playerDeck.CountCards();
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
        uniteState.SetTrashManager(playerTrashManager);
        uniteState.Initialize(turnNumber);

        if(selectedCard.GetComponent<UnitClickHandler>() == null)
        {
            selectedCard.gameObject.AddComponent<UnitClickHandler>();
        }

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
                Debug.Log($"Unité peut attaquer: {unitState.CanAttackOnTurn(turnNumber)}.");
            }

        }
    }

}
