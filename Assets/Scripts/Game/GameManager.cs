using UnityEngine;
using System.Collections.Generic;

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
    [SerializeField] private Transform opponentUnitZone;
    [SerializeField] private LifeZoneManager playerLifeManager;
    [SerializeField] private LifeZoneManager opponentLifeManager;
    [SerializeField] private GameObject cancelEffectButton;
    [SerializeField] private GameObject activateEffectButton;
    [SerializeField] private TrashViewer trashViewer;
    [SerializeField] private Transform playerTerrainZone;
    [SerializeField] private Transform opponentTerrainZone;
    [SerializeField] private TrashManager opponentTrashManager;

    [Header("Configuration de la partie")]
    [SerializeField] private bool playerGoesFirst = true;
    [SerializeField] private int startingHandSize = 3;
    [SerializeField] private CombatManager combatManager;

    // Variable pour gérer les dégats ciblés
    private bool waitingForEffectTarget;
    private int pendingEffectDamage;
    private CardData pendingEffectCard;
    // Variable pour gérer les soins ciblés
    private bool waitingForHealTarget;
    private bool healTargetsOpponent;
    private int pendingHealAmount;
    private CardData pendingHealCard;
    private UnitCardState pendingHealSource;
    // Variable pour gérer les modif d'attaque
    private bool waitingForAttackTarget;
    private bool attackTargetsOpponent;
    private int pendingAttackAmount;
    // Variable pour gérer les effets de redressement et d'épuisement
    private bool pendingReadyEffect;
    private bool pendingExhaustEffect;
    // Variable pour gérer les effets de coût en mana
    private bool pendingManaCostEffect;
    // Variable pour gérer les effets de KO par coût en mana
    private bool pendingKnockOutEffect;
    private int pendingKnockOutManaThreshold;
    // Variables pour les effets de récupération depuis la Trash
    private bool waitingForTrashCard;
    private bool recoverTrashCardToHand;
    private bool recoverTrashCardToDeck;

    //Variable pour supprimer une carde de sa main
    private bool waitingForHandDiscard;

    // Variable pour gérer les effets "une fois par tour"
    private UnitCardState pendingOncePerTurnSource;
    private bool resolvingOncePerTurnEffect;
    private int pendingOncePerTurnEffectIndex = -1;

    // Variable pour les sorts
    private CardView pendingSpellCard;
    private bool counterWindowActive = false;
    public bool CounterWindowActive => counterWindowActive;
    public bool IsResolvingCounterSpell { get; private set; }

    // Variable pour les terrains
    private TerrainCardState selectedTerrain;
    private TerrainCardState pendingOncePerTurnTerrainSource;

    // Queue pour gérer les effets en attente
    private Queue<System.Action> pendingEffects = new Queue<System.Action>();
    private bool resolvingEffectQueue;

    private bool waitingToFinishPlayerTurn;
    private bool waitingToCompletePlayerTurnStart;

    private bool gameStarted;
    private int turnNumber = 0;

    public TurnOwner CurrentTurnOwner { get; private set; } = TurnOwner.Player;
    public bool IsPlayerTurn => CurrentTurnOwner == TurnOwner.Player;
    public bool IsResolvingPlayerTurnStart => waitingToCompletePlayerTurnStart;

    public bool IsResolvingOncePerTurnEffect => resolvingOncePerTurnEffect;

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

        playerHand.CardSelected -= OnHandCardSelected;
        playerHand.CardSelected += OnHandCardSelected;

        if (cancelEffectButton != null)
        {
            cancelEffectButton.SetActive(false);
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

        if (resolvingOncePerTurnEffect)
        {
            Debug.Log("Impossible de terminer le tour pendant l'activation d'un effet.");
            return;
        }

        if (CurrentTurnOwner == TurnOwner.Player)
        {
            if (waitingToFinishPlayerTurn)
                return;

            CancelPendingEffect();
            CancelPendingHeal();

            waitingToFinishPlayerTurn = true;

            ResolveTurnEffects(EffectTrigger.EndOfTurn);

            if(waitingForEffectTarget || waitingForHealTarget)
            {
                Debug.Log("Fin du tour en attente de la résolution des effets.");
                return;
            }
            CompletePlayerTurn();
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
        championDisplay.ResetAttack();
        turnNumber++;

        RefreshMana();
        ReadyAllUnit();

        waitingToCompletePlayerTurnStart = true;
        ResolveTurnEffects(EffectTrigger.StartOfTurn);

        if (waitingForEffectTarget || waitingForHealTarget)
        {
            Debug.Log("Début du tour en attente de la résolution des effets.");
            return;
        }

        waitingToCompletePlayerTurnStart = false;
        CompletePlayerTurnStart();
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
        if (waitingToCompletePlayerTurnStart)
        {
            Debug.Log("Impossible de jouer une unité pendant la résolution des effets de début de tour.");
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

        if (data.cardType == CardType.Terrain)
        {
            PlaySelectedTerrain();
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
        uniteState.SetGameManager(this);
        uniteState.Initialize(turnNumber);

        UnitClickHandler clickHandler = selectedCard.GetComponent<UnitClickHandler>();

        if (clickHandler == null)
            clickHandler = selectedCard.gameObject.AddComponent<UnitClickHandler>();

        clickHandler.SetGameManager(this);

        Debug.Log($"{data.cardName} a été jouée !");
        ResolveCardEffects(data, EffectTrigger.OnPlay, uniteState);
    }
    public void PlaySelectedTerrain()
    {
        if (!gameStarted)
        {
            Debug.LogWarning("La partie n'a pas commencé.");
            return;
        }

        if (waitingToCompletePlayerTurnStart)
        {
            Debug.Log("Impossible de jouer un terrain pendant la résolution des effets de début de tour.");
            return;
        }

        if (!IsPlayerTurn || counterWindowActive)
        {
            Debug.Log("Impossible de jouer un terrain en dehors de ton tour.");
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

        if (data.cardType != CardType.Terrain)
        {
            Debug.Log("Seules les cartes Terrain peuvent être jouées ici.");
            return;
        }

        if (playerTerrainZone == null)
        {
            Debug.LogError("La zone Terrain du joueur n'est pas assignée.");
            return;
        }

        if (manaSystem == null || manaSystem.AvailableMana < data.manaCost)
        {
            Debug.Log("Pas assez de mana pour jouer ce terrain.");
            return;
        }

        // Récupérer l'ancien terrain, s'il existe.
        CardView previousTerrain = playerTerrainZone.GetComponentInChildren<CardView>();

        if (previousTerrain != null && playerTrashManager == null)
        {
            Debug.LogError("Le TrashManager du joueur n'est pas assigné.");
            return;
        }

        // Payer le coût en mana.
        bool paid = manaSystem.ExhaustMana(data.manaCost);

        if (!paid)
        {
            Debug.Log("Impossible de payer le coût en mana.");
            return;
        }

        // Retirer le nouveau terrain de la main.
        playerHand.RemoveCard(selectedCard);

        // Envoyer l'ancien terrain dans la Trash.
        if (previousTerrain != null)
        {
            Debug.Log($"{previousTerrain.Data.cardName} est remplacé et envoyé dans la Trash.");
            playerTrashManager.AddCard(previousTerrain);
        }

        // Installer le nouveau terrain.
        selectedCard.transform.SetParent(playerTerrainZone, false);
        selectedCard.transform.localScale = Vector3.one;

        TerrainCardState terrainState = selectedCard.GetComponent<TerrainCardState>();

        if (terrainState == null)
        {
            terrainState = selectedCard.gameObject.AddComponent<TerrainCardState>();
        }

        terrainState.Initialize();

        TerrainClickHandler clickHandler =selectedCard.GetComponent<TerrainClickHandler>();

        if (clickHandler == null)
        {
            clickHandler = selectedCard.gameObject.AddComponent<TerrainClickHandler>();
        }

        clickHandler.SetGameManager(this);

        Debug.Log($"{data.cardName} est maintenant ton terrain actif !");

        // Résoudre ses effets OnPlay.
        ResolveCardEffects(data, EffectTrigger.OnPlay);
    }
    public void PlaySelectedSpell()
    {
        if (!gameStarted)
        {
            Debug.LogWarning("La partie n'a pas commencé.");
            return;
        }

        if (IsResolvingCounterSpell)
        {
            Debug.Log("Impossible de jouer un autre sort Counter pendant la résolution du précédent.");
            return;
        }

        if (waitingToCompletePlayerTurnStart)
        {
            Debug.Log("Impossible de jouer un sort pendant la résolution des effets de début de tour.");
            return;
        }

        CardView selectedCard = playerHand.SelectedCard;

        if (selectedCard == null)
        {
            Debug.Log("Sélectionne d'abord une carte dans ta main.");
            return;
        }

        CardData data = selectedCard.Data;

        if (data == null || data.cardType != CardType.Spell)
        {
            Debug.Log("La carte sélectionnée n'est pas un sort.");
            return;
        }

        if (!counterWindowActive && !IsPlayerTurn)
        {
            Debug.Log("Impossible de jouer un sort OnPlay pendant le tour adverse.");
            return;
        }

        EffectTrigger requiredTrigger = counterWindowActive ? EffectTrigger.Counter : EffectTrigger.OnPlay;
        bool hasRequiredTriggerEffect = false;
        if(data.effects != null)
        {
            foreach (CardEffectData effect in data.effects)
            {
                if (effect != null && effect.trigger == requiredTrigger)
                {
                    hasRequiredTriggerEffect = true;
                    break;
                }
            }
        }
        if (!hasRequiredTriggerEffect)
        {
            Debug.Log(counterWindowActive ? "Ce sort ne possède aucun effet Counter." : "Ce sort ne possède aucun effet OnPlay.");
            return;
        }

        if (data.effects != null)
        {
            foreach (CardEffectData effect in data.effects)
            {
                EffectTrigger currentTrigger = counterWindowActive ? EffectTrigger.Counter : EffectTrigger.OnPlay;
                if (effect == null || effect.trigger != currentTrigger)
                    continue;

                if (effect.mustResolve &&  effect.effectType == CardEffectType.ReturnManaCardsToDeck && (manaSystem == null || manaSystem.TotalManaOnBoard < effect.value))
                {
                    Debug.Log("Impossible de jouer ce sort : pas assez de cartes mana sur le terrain pour résoudre tous ses effets.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.AddManaCardsFromDeck && (manaSystem == null || manaSystem.RemainingManaCards < effect.value))
                {
                    Debug.Log("Impossible de jouer ce sort : pas assez de cartes dans le Mana Deck.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.ReadyManaCards && (manaSystem == null || manaSystem.ExhaustedManaCount < effect.value))
                {
                    Debug.Log("Impossible de jouer ce sort : pas assez de cartes mana épuisées à redresser.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.RemoveOpponentLife && (opponentLifeManager == null || opponentLifeManager.RemainingLives <= effect.conditionValue))
                {
                    Debug.Log("Impossible de jouer ce sort : l'adversaire ne possède pas assez de cartes vie.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.GainLife && (playerDeck == null || playerDeck.RemainingCards < effect.value))
                {
                    Debug.Log("Impossible de jouer ce sort : pas assez de cartes dans le deck pour gagner toutes les cartes vie.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.LoseLife && (playerLifeManager == null || playerLifeManager.RemainingLives < effect.value))
                {
                    Debug.Log("Impossible de jouer ce sort : pas assez de cartes vie pour subir toute la perte.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.DrawCard && (playerDeck == null || playerDeck.RemainingCards < effect.value))
                {
                    Debug.Log("Impossible de jouer ce sort : pas assez de cartes dans le deck pour effectuer toute la pioche.");
                    return;
                }

                if (effect.mustResolve && effect.effectType == CardEffectType.DamageEnemyUnit && (opponentUnitSpawner == null || opponentUnitSpawner.GetOpponentUnits().Length == 0))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune unité adverse disponible pour subir les dégâts.");
                    return;
                }
                if (effect.mustResolve && effect.effectType == CardEffectType.KnockOutEnemyUnitByManaCost && !HasKnockOutTarget(effect.conditionValue))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune unité adverse ne respecte le coût en mana requis pour être mise KO.");
                    return;
                }

                if (effect.effectType == CardEffectType.DamageTargetEnemyUnit && (opponentUnitSpawner == null || opponentUnitSpawner.GetOpponentUnits().Length == 0))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune unité adverse à cibler.");
                    return;
                }

                if (effect.effectType == CardEffectType.HealFriendlyUnit && (playerUnitZone == null ||playerUnitZone.GetComponentsInChildren<UnitCardState>().Length == 0))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune unité alliée à soigner.");
                    return;
                }

                if (effect.mustResolve && (effect.effectType == CardEffectType.RecoverCardFromTrashToHand || effect.effectType == CardEffectType.RecoverCardFromTrashToDeck) && (playerTrashManager == null || playerTrashManager.CardCount == 0))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune carte disponible dans la Trash.");
                    return;
                }

                if (effect.mustResolve && (effect.effectType == CardEffectType.KnockOutEnemyUnitByManaCost || effect.effectType == CardEffectType.ExhaustEnemyUnit || effect.effectType == CardEffectType.IncreaseEnemyUnitAttack || effect.effectType == CardEffectType.DecreaseEnemyUnitAttack) && (opponentUnitSpawner == null || opponentUnitSpawner.GetOpponentUnits().Length == 0))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune unité adverse à cibler.");
                    return;
                }

                if (effect.mustResolve && (effect.effectType == CardEffectType.ReadyFriendlyUnit || effect.effectType == CardEffectType.IncreaseFriendlyUnitAttack || effect.effectType == CardEffectType.DecreaseFriendlyUnitAttack || effect.effectType == CardEffectType.HealFriendlyUnit || effect.effectType == CardEffectType.IncreaseFriendlyUnitManaCost) && (playerUnitZone == null || playerUnitZone.GetComponentsInChildren<UnitCardState>().Length == 0))
                {
                    Debug.Log("Impossible de jouer ce sort : aucune unité alliée à cibler.");
                    return;
                }
            }
        }

        if (manaSystem.AvailableMana < data.manaCost)
        {
            Debug.Log($"Pas assez de mana pour jouer {data.cardName}.");
            return;
        }

        bool paid = manaSystem.ExhaustMana(data.manaCost);

        if (!paid)
        {
            Debug.Log("Impossible de payer le coût en mana.");
            return;
        }

        playerHand.RemoveCard(selectedCard);

        pendingSpellCard = selectedCard;
        IsResolvingCounterSpell = counterWindowActive;

        Debug.Log($"{data.cardName} est joué. Résolution de ses effets...");

        ResolveSpellEffects(data, counterWindowActive ? EffectTrigger.Counter : EffectTrigger.OnPlay);
    }
    public void OpenCounterWindow()
    {
        counterWindowActive = true;
        Debug.Log("Fenêtre Counter ouverte : tu peux jouer tes sorts Counter.");
    }
    public void CloseCounterWindow()
    {
        counterWindowActive = false;
        Debug.Log("Fenêtre Counter fermée.");
    }
    public void FinishCounterWindow()
    {
        if (!counterWindowActive)
            return;

        if (IsResolvingCounterSpell)
        {
            Debug.Log("Termine d'abord la résolution du Counter en cours.");
            return;
        }

        counterWindowActive = false;

        Debug.Log("Fenêtre Counter terminée. L'attaque adverse peut reprendre.");
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
    public void ActivateSelectedOncePerTurnEffect()
    {
        if (resolvingOncePerTurnEffect)
            return;

        UnitCardState unitSource = GetSelectedOncePerTurnSource();

        TerrainCardState terrainSource = unitSource == null ? selectedTerrain : null;

        CardView cardView = null;
        bool canActivate = false;

        if (unitSource != null)
        {
            cardView = unitSource.GetComponent<CardView>();
            canActivate = CanActivateOncePerTurn(unitSource);
        }
        else if (terrainSource != null)
        {
            cardView = terrainSource.GetComponent<CardView>();
            canActivate = CanActivateOncePerTurn(terrainSource);
        }

        if (!canActivate || cardView == null || cardView.Data == null)
        {
            Debug.Log("Aucun effet OncePerTurn disponible pour cette carte.");
            return;
        }

        pendingOncePerTurnEffectIndex = -1;

        for (int i = 0; i < cardView.Data.effects.Length; i++)
        {
            CardEffectData effect = cardView.Data.effects[i];

            if (effect != null && effect.trigger == EffectTrigger.OncePerTurn)
            {
                pendingOncePerTurnEffectIndex = i;
                break;
            }
        }

        if (pendingOncePerTurnEffectIndex < 0)
            return;

        pendingOncePerTurnSource = unitSource;
        pendingOncePerTurnTerrainSource = terrainSource;
        resolvingOncePerTurnEffect = true;

        UpdateActivateEffectButton();

        ResolveCardEffects(cardView.Data,EffectTrigger.OncePerTurn,unitSource);

        if (!waitingForEffectTarget && !waitingForHealTarget && !waitingForAttackTarget && !waitingForTrashCard)
        {
            CompleteOncePerTurnActivation();
        }

        Debug.Log("Activation de l'effet OncePerTurn en cours.");
    }
    public void UpdateActivateEffectButton()
    {
        bool canActivate = false;

        if (!resolvingOncePerTurnEffect)
        {
            if (selectedTerrain != null)
            {
                canActivate = CanActivateOncePerTurn(selectedTerrain);
            }
            else
            {
                UnitCardState source = GetSelectedOncePerTurnSource();
                canActivate = CanActivateOncePerTurn(source);
            }
        }

        if (activateEffectButton != null)
        {
            activateEffectButton.SetActive(canActivate);
        }
    }
    public bool IsWaitingForEffectTarget()
    {
        return waitingForEffectTarget;
    }
    public bool IsWaitingForHealTarget()
    {
        return waitingForHealTarget;
    }
    public bool IsWaitingForAttackTarget()
    {
        return waitingForAttackTarget;
    }
    public void ActivateOpponentTerrainOncePerTurn()
    {
        if (opponentTerrainZone == null)
            return;

        TerrainCardState terrainState =
            opponentTerrainZone.GetComponentInChildren<TerrainCardState>();

        if (terrainState == null)
            return;

        CardView cardView = terrainState.GetComponent<CardView>();

        if (cardView == null || cardView.Data == null ||
            cardView.Data.effects == null)
            return;

        for (int i = 0; i < cardView.Data.effects.Length; i++)
        {
            CardEffectData effect = cardView.Data.effects[i];

            if (effect == null || effect.trigger != EffectTrigger.OncePerTurn)
                continue;

            if (terrainState.HasUsedEffectThisTurn(i))
            {
                Debug.Log("L'effet OncePerTurn du terrain adverse a déjà été utilisé.");
                return;
            }

            if (effect.activationCost == ActivationCostType.ExhaustCard &&
                terrainState.IsExhausted)
            {
                Debug.Log("Le terrain adverse est déjà épuisé.");
                return;
            }

            if (effect.activationCost == ActivationCostType.SpendMana)
            {
                Debug.LogWarning(
                    "Activation impossible : le système de mana adverse n'est pas encore disponible."
                );
                return;
            }

            switch (effect.effectType)
            {
                case CardEffectType.IncreaseFriendlyChampionAttack:
                    opponentChampionDisplay.ModifyAttack(effect.value);
                    break;

                case CardEffectType.DecreaseFriendlyChampionAttack:
                    opponentChampionDisplay.ModifyAttack(-effect.value);
                    break;

                case CardEffectType.IncreaseEnemyChampionAttack:
                    championDisplay.ModifyAttack(effect.value);
                    break;

                case CardEffectType.DecreaseEnemyChampionAttack:
                    championDisplay.ModifyAttack(-effect.value);
                    break;

                default:
                    Debug.LogWarning(
                        $"Effet OncePerTurn non encore pris en charge pour le terrain adverse : {effect.effectType}"
                    );
                    return;
            }

            if (effect.activationCost == ActivationCostType.ExhaustCard)
                terrainState.Exhaust();

            terrainState.MarkEffectAsUsed(i);

            Debug.Log(
                $"Le terrain adverse {cardView.Data.cardName} a activé son effet OncePerTurn."
            );

            return;
        }
    }
    public void ReadyOpponentTerrain()
    {
        if (opponentTerrainZone == null)
        {
            Debug.LogWarning("La zone Terrain adverse n'est pas assignée.");
            return;
        }

        TerrainCardState terrainState =
            opponentTerrainZone.GetComponentInChildren<TerrainCardState>();

        if (terrainState == null)
            return;

        terrainState.Ready();
        terrainState.ResetOncePerTurnEffects();

        Debug.Log("Terrain adverse redressé : effets OncePerTurn réinitialisés.");
    }
    public void DestroyAllTerrains()
    {
        // Détruire le terrain du joueur.
        if (playerTerrainZone != null)
        {
            CardView playerTerrain =
                playerTerrainZone.GetComponentInChildren<CardView>();

            if (playerTerrain != null)
            {
                if (playerTrashManager == null)
                {
                    Debug.LogError("TrashManager du joueur non assigné.");
                }
                else
                {
                    TerrainCardState terrainState =
                        playerTerrain.GetComponent<TerrainCardState>();

                    if (selectedTerrain != null &&
                        selectedTerrain.gameObject == playerTerrain.gameObject)
                    {
                        selectedTerrain.GetComponent<CardView>()?.SetSelected(false);
                        selectedTerrain = null;
                    }

                    if (terrainState != null)
                        Destroy(terrainState);

                    TerrainClickHandler clickHandler =
                        playerTerrain.GetComponent<TerrainClickHandler>();

                    if (clickHandler != null)
                        Destroy(clickHandler);

                    playerTrashManager.AddCard(playerTerrain);

                    Debug.Log("Ton terrain a été envoyé dans ta Trash.");
                }
            }
        }

        // Détruire le terrain adverse.
        if (opponentUnitSpawner != null)
        {
            opponentUnitSpawner.DestroyOpponentTerrain();
        }
        else
        {
            Debug.LogError("OpponentUnitSpawner non assigné.");
        }

        UpdateActivateEffectButton();

        Debug.Log("Effet DestroyAllTerrains résolu.");
    }
    public void ResolveOpponentTerrainOnPlay(CardData data)
    {
        if (data == null || data.effects == null)
            return;

        foreach (CardEffectData effect in data.effects)
        {
            if (effect == null || effect.trigger != EffectTrigger.OnPlay)
                continue;

            switch (effect.effectType)
            {
                case CardEffectType.IncreaseFriendlyChampionAttack:
                    if (opponentChampionDisplay != null)
                        opponentChampionDisplay.ModifyAttack(effect.value);
                    break;

                case CardEffectType.DecreaseFriendlyChampionAttack:
                    if (opponentChampionDisplay != null)
                        opponentChampionDisplay.ModifyAttack(-effect.value);
                    break;

                case CardEffectType.IncreaseEnemyChampionAttack:
                    if (championDisplay != null)
                        championDisplay.ModifyAttack(effect.value);
                    break;

                case CardEffectType.DecreaseEnemyChampionAttack:
                    if (championDisplay != null)
                        championDisplay.ModifyAttack(-effect.value);
                    break;
            }
        }

        Debug.Log($"Effets OnPlay du terrain adverse résolus : {data.cardName}.");
    }
    public void ResolveCardEffectsOnDeath(CardData data, UnitCardState unit)
    {
        ResolveCardEffects(data, EffectTrigger.OnDeath, unit);
    }
    public void ResolveTurnEffects(EffectTrigger trigger)
    {
        ResolveTurnEffectsForZone(playerUnitZone, trigger);
    }
    public void SelectAttackTarget(UnitCardState target)
    {
        if (!waitingForAttackTarget || target == null)
            return;

        bool targetIsOpponent = target.transform.IsChildOf(opponentUnitZone);

        if (targetIsOpponent != attackTargetsOpponent)
        {
            Debug.LogWarning("Choisis une unité de la bonne équipe.");
            return;
        }

        if (pendingReadyEffect)
        {
            target.Ready();
            Debug.Log("L'unité alliée ciblée a été redressée.");
        }
        else if (pendingExhaustEffect)
        {
            target.Exhaust();
            Debug.Log("L'unité adverse ciblée a été épuisée.");
        }
        else if (pendingManaCostEffect)
        {
            target.IncreaseTemporaryManaCost(pendingAttackAmount);
            Debug.Log("Le coût en mana de l'unité alliée a été augmenté.");
        }
        else if (pendingKnockOutEffect)
        {
            if (target.CurrentManaCost >= pendingKnockOutManaThreshold)
            {
                Debug.LogWarning(
                    $"Cette unité coûte {target.CurrentManaCost} mana. " +
                    $"Son coût doit être strictement inférieur à {pendingKnockOutManaThreshold}."
                );
                return;
            }

            target.TakeDamage(target.MaxHealth);
            Debug.Log("L'unité adverse a été mise KO par un effet.");
        }
        else
        {
            target.ModifyAttack(pendingAttackAmount);
            Debug.Log("L'attaque de l'unité ciblée a été modifiée.");
        }

        waitingForAttackTarget = false;
        pendingAttackAmount = 0;
        pendingReadyEffect = false;
        pendingExhaustEffect = false;
        pendingManaCostEffect = false;
        pendingKnockOutEffect = false;
        pendingKnockOutManaThreshold = 0;

        CompleteOncePerTurnActivation();

        cancelEffectButton?.SetActive(false);

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
    }
    public void SelectHealTarget(UnitCardState target)
    {
        if (!waitingForHealTarget)
            return;

        if (target == null)
        {
            Debug.LogWarning("La cible de soin n'existe plus.");
            CancelPendingHeal();
            return;
        }
        if (target == pendingHealSource)
        {
            Debug.Log("Impossible de soigner l'unité qui déclenche cet effet.");
            return;
        }

        bool targetIsOpponent = target.transform.IsChildOf(opponentUnitZone);

        if(targetIsOpponent != healTargetsOpponent)
        {
            Debug.Log("Tu dois sélectionner une unité de la même équipe.");
            return;
        }

        waitingForHealTarget = false;
        cancelEffectButton?.SetActive(false);

        target.Heal(pendingHealAmount);
        CompleteOncePerTurnActivation();

        Debug.Log($"Effet de soin de {pendingHealCard.cardName} : {pendingHealAmount} PV sur {target.name}.");

        pendingHealCard = null;
        pendingHealAmount = 0;
        pendingHealSource = null;
        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
    }
    public void SelectEffectTarget(UnitCardState target)
    {
        if (!waitingForEffectTarget)
            return;

        if (target == null)
        {
            Debug.LogWarning("La cible de l'effet n'existe plus.");
            CancelPendingEffect();
            return;
        }

        waitingForEffectTarget = false;
        cancelEffectButton?.SetActive(false);

        target.TakeDamage(pendingEffectDamage);
        CompleteOncePerTurnActivation();

        Debug.Log($"Effet de {pendingEffectCard.cardName} : {pendingEffectDamage} dégâts infligés à {target.name}.");

        pendingEffectCard = null;
        pendingEffectDamage = 0;

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
    }
    public void SelectTerrain(TerrainCardState terrain)
    {
        if (terrain == null)
            return;

        if (playerTerrainZone == null ||
            !terrain.transform.IsChildOf(playerTerrainZone))
        {
            Debug.LogWarning("Ce n'est pas ton terrain actif.");
            return;
        }

        if (resolvingOncePerTurnEffect)
        {
            Debug.Log("Sélection impossible pendant la résolution d'un effet.");
            return;
        }

        // Annuler la sélection du champion et des unités.
        if (combatManager != null)
            combatManager.ClearPlayerChampionSelection();

        if (UnitSlectionManager.Instance != null)
            UnitSlectionManager.Instance.ClearSelection();

        if (selectedTerrain == terrain)
        {
            terrain.GetComponent<CardView>()?.SetSelected(false);
            selectedTerrain = null;

            Debug.Log("Sélection du terrain annulée.");
        }
        else
        {
            if (selectedTerrain != null)
                selectedTerrain.GetComponent<CardView>()?.SetSelected(false);

            selectedTerrain = terrain;
            selectedTerrain.GetComponent<CardView>()?.SetSelected(true);

            Debug.Log("Terrain sélectionné : " +
                terrain.GetComponent<CardView>()?.Data.cardName);
        }

        UpdateActivateEffectButton();
    }
    public void CancelCurrentEffect()
    {
        if (waitingForEffectTarget)
        {
            CancelPendingEffect();
            CancelOncePerTurnActivation();
            return;
        }

        if (waitingForHealTarget)
        {
            CancelPendingHeal();
            CancelOncePerTurnActivation();
            return;
        }

        if (waitingForAttackTarget)
        {
            CancelPendingBoostAttack();
            CancelOncePerTurnActivation();
            return;
        }

        if (waitingForTrashCard)
        {
            CancelPendingTrashRecovery();
            CancelOncePerTurnActivation();
            return;
        }

        if (waitingForHandDiscard)
        {
            CancelPendingHandDiscard();
            CancelOncePerTurnActivation();
            return;
        }

        Debug.Log("Aucun effet ne peut être annulé actuellement.");
    }
    private void EnqueueEffect(System.Action effectAction)
    {
        pendingEffects.Enqueue(effectAction);
    }
    private void ProcessNextPendingEffect()
    {
        if (resolvingEffectQueue)
            return;

        resolvingEffectQueue = true;

        while (pendingEffects.Count > 0)
        {
            System.Action nextEffect = pendingEffects.Dequeue();

            nextEffect?.Invoke();

            if (waitingForEffectTarget || waitingForHealTarget || waitingForAttackTarget || waitingForTrashCard || waitingForHandDiscard)
            {
                resolvingEffectQueue = false;
                return;
            }
        }

        resolvingEffectQueue = false;
        if (pendingSpellCard != null && pendingEffects.Count == 0 && !waitingForEffectTarget && !waitingForHealTarget && !waitingForAttackTarget && !waitingForTrashCard && !waitingForHandDiscard)
        {
            CompleteSpellResolution();
        }
    }
    private void CompletePlayerTurn()
    {
        waitingToFinishPlayerTurn = false;

        CurrentTurnOwner = TurnOwner.Opponent;
        opponentChampionDisplay.ResetAttack();

        if (combatManager != null)
        {
            combatManager.ClearCombatSelections();
        }

        Debug.Log("Tour de l'adversaire commencé.");
        GetComponent<OpponentTurnManager>()?.StartOpponentTurn();

    }
    private void CheckPendingPlayerTurnCompletion()
    {
        if (waitingForEffectTarget || waitingForHealTarget)
            return;

        if (waitingToFinishPlayerTurn)
        {
            CompletePlayerTurn();
        }

        if (waitingToCompletePlayerTurnStart)
        {
            waitingToCompletePlayerTurnStart = false;
            CompletePlayerTurnStart();
        }
    }
    private void BeginTargetedDamageEffect(CardData data, int damage)
    {
        if (opponentUnitSpawner == null ||
            opponentUnitSpawner.GetOpponentUnits().Length == 0)
        {
            Debug.Log("Aucune unité adverse à cibler.");
            return;
        }

        waitingForEffectTarget = true;
        cancelEffectButton?.SetActive(true);
        pendingEffectDamage = damage;
        pendingEffectCard = data;

        Debug.Log(
            $"Choisis une unité adverse pour l'effet de {data.cardName}."
        );
    }
    private void BeginTargetedHealEffect(CardData data, int healAmount, UnitCardState sourceUnit)
    {
        if (playerUnitZone == null || opponentUnitZone == null)
        {
            Debug.LogError("Une des zones d'unités n'est pas assignée.");
            return;
        }

        Transform friendlyZone = sourceUnit != null ? (sourceUnit.transform.IsChildOf(playerUnitZone) ? playerUnitZone : opponentUnitZone) : playerUnitZone;

        if (friendlyZone == null)
        {
            Debug.LogError("La zone d'unités à soigner n'est pas assignée.");
            return;
        }

        UnitCardState[] friendlyUnits = friendlyZone.GetComponentsInChildren<UnitCardState>();

        int validTargets = 0;

        foreach (UnitCardState unit in friendlyUnits)
        {
            if (unit != null && unit != sourceUnit && unit.CurrentHealth > 0)
            {
                validTargets++;
            }
        }

        if (validTargets == 0)
        {
            Debug.Log("Aucune autre unité alliée à soigner.");
            return;
        }

        waitingForHealTarget = true;
        cancelEffectButton?.SetActive(true);
        pendingHealSource = sourceUnit;
        pendingHealAmount = healAmount;
        pendingHealCard = data;
        healTargetsOpponent = sourceUnit != null && !sourceUnit.transform.IsChildOf(playerUnitZone);

        Debug.Log($"Choisis une unité alliée à soigner avec l'effet de {data.cardName}.");
    }
    private void BeginTargetedAttackEffect(CardData data, int amount, bool targetOpponent)
    {
        waitingForAttackTarget = true;
        attackTargetsOpponent = targetOpponent;
        pendingAttackAmount = amount;

        cancelEffectButton?.SetActive(true);

        string team = targetOpponent ? "adverse" : "alliée";

        Debug.Log($"Choisis une unité {team} pour modifier son attaque de {amount}.");
    }
    private void BeginTargetedReadyEffect(CardData data)
    {
        pendingReadyEffect = true;
        pendingExhaustEffect = false;
        waitingForAttackTarget = true;
        attackTargetsOpponent = false;
        pendingAttackAmount = 0;

        cancelEffectButton?.SetActive(true);

        Debug.Log($"Choisis une unité alliée à redresser grâce à {data.cardName}.");
    }
    private void BeginTargetedExhaustEffect(CardData data)
    {
        pendingReadyEffect = false;
        pendingExhaustEffect = true;
        waitingForAttackTarget = true;
        attackTargetsOpponent = true;
        pendingAttackAmount = 0;

        cancelEffectButton?.SetActive(true);

        Debug.Log($"Choisis une unité adverse à épuiser grâce à {data.cardName}.");
    }
    private void BeginTargetedManaCostEffect(CardData data, int amount)
    {
        pendingManaCostEffect = true;
        pendingReadyEffect = false;
        pendingExhaustEffect = false;
        waitingForAttackTarget = true;
        attackTargetsOpponent = false;
        pendingAttackAmount = amount;

        cancelEffectButton?.SetActive(true);

        Debug.Log($"Choisis une unité alliée pour modifier son coût en mana de {amount} grâce à {data.cardName}.");

    }
    private void BeginTargetedKnockOutEffect(CardData data, int manaThreshold)
    {
        pendingKnockOutEffect = true;
        pendingKnockOutManaThreshold = manaThreshold;

        pendingReadyEffect = false;
        pendingExhaustEffect = false;
        pendingManaCostEffect = false;

        waitingForAttackTarget = true;
        attackTargetsOpponent = true;
        pendingAttackAmount = 0;

        cancelEffectButton?.SetActive(true);
        Debug.Log($"Choisis une unité adverse dont le coût en mana est strictement inférieur à {manaThreshold} pour la mettre KO grâce à {data.cardName}.");
    }
    private void BeginTrashRecoveryEffect(CardData data, bool toHand)
    {
        if (playerTrashManager == null)
        {
            Debug.LogError("GameManager : PlayerTrashManager n'est pas assigné.");
            return;
        }

        if (playerTrashManager.CardCount == 0)
        {
            Debug.Log("La Trash est vide : impossible de récupérer une carte.");
            return;
        }

        waitingForTrashCard = true;
        recoverTrashCardToHand = toHand;
        recoverTrashCardToDeck = !toHand;

        trashViewer.OnCardSelected -= OnTrashCardSelected;
        trashViewer.OnCardSelected += OnTrashCardSelected;

        string destination = toHand ? "ta main" : "le dessus de ton deck";

        trashViewer.OpenTrash(playerTrashManager, "Choisis une carte pour " + destination);

        cancelEffectButton?.SetActive(true);

        Debug.Log($"{data.cardName} : choisis une carte dans ta Trash pour la récupérer vers {destination}.");
    }
    private void BeginHandDiscardEffect(CardData data, bool mustResolve)
    {
        if (playerHand == null || playerHand.CardCOunt == 0)
        {
            if (mustResolve)
            {
                Debug.Log("Impossible de résoudre l'effet : ta main ne contient aucune carte à défausser.");
            }
            else
            {
                Debug.Log("Effet facultatif ignoré : aucune carte dans ta main.");
            }

            return;
        }

        waitingForHandDiscard = true;

        cancelEffectButton?.SetActive(true);

        Debug.Log(
            $"{data.cardName} : sélectionne une carte de ta main à envoyer dans la Trash."
        );
    }
    private void ResolveCardEffects(CardData data, EffectTrigger trigger, UnitCardState sourceUnit = null)
    {
        if(data == null || data.effects == null)
        {
            return;
        }

        foreach (CardEffectData effect in data.effects)
        {
            if(effect == null || effect.trigger != trigger)
            {
                continue;
            }

            switch (effect.effectType)
            {
                case CardEffectType.DrawCard:
                    for (int i = 0; i < effect.value; i++)
                    {
                        DrawCard();
                    }
                    Debug.Log($"{data.cardName} déclenche un effet : pioche de {effect.value} carte(s)");
                    break;

                case CardEffectType.AddManaCardsFromDeck:
                    if (manaSystem == null)
                    {
                        Debug.LogError("ManaSystem n'est pas assigné dans GameManager.");
                        break;
                    }
                    manaSystem.AddManaCards(effect.value);
                    Debug.Log($"{data.cardName} ajoute {effect.value} carte(s) mana depuis le Mana Deck.");
                    break;

                case CardEffectType.ReadyManaCards:
                    if (manaSystem == null)
                    {
                        Debug.LogError("ManaSystem n'est pas assigné dans GameManager.");
                        break;
                    }

                    manaSystem.ReadyManaCards(effect.value);
                    break;

                case CardEffectType.ReturnManaCardsToDeck:
                    if (manaSystem == null)
                    {
                        Debug.LogError("ManaSystem n'est pas assigné dans GameManager.");
                        break;
                    }

                    manaSystem.ReturnManaCardsToDeck(effect.value);
                    break;

                case CardEffectType.DamageEnemyUnit:
                    DamageFirstEnemyUnit(effect.value);
                    break;

                case CardEffectType.DamageTargetEnemyUnit:
                    BeginTargetedDamageEffect(data, effect.value);
                    break;

                case CardEffectType.HealFriendlyUnit:
                    BeginTargetedHealEffect(data, effect.value, sourceUnit);
                    break;

                case CardEffectType.IncreaseFriendlyChampionAttack:
                    championDisplay.ModifyAttack(effect.value);
                    Debug.Log($"L'attaque de ton champion a augmentée de {effect.value}");
                    break;

                case CardEffectType.DecreaseFriendlyChampionAttack:
                    championDisplay.ModifyAttack(-effect.value);
                    Debug.Log($"L'attaque de ton champion a diminuée de {effect.value}");
                    break;

                case CardEffectType.IncreaseEnemyChampionAttack:
                    opponentChampionDisplay.ModifyAttack(effect.value);
                    Debug.Log($"L'attaque du Champion adverse a augmentée de {effect.value}.");
                    break;

                case CardEffectType.DecreaseEnemyChampionAttack:
                    opponentChampionDisplay.ModifyAttack(-effect.value);
                    Debug.Log($"L'attaque du Champion adverse a diminuée de {effect.value}.");
                    break;

                case CardEffectType.IncreaseFriendlyUnitAttack:
                    BeginTargetedAttackEffect(data, effect.value, false);
                    break;

                case CardEffectType.DecreaseFriendlyUnitAttack:
                    BeginTargetedAttackEffect(data, -effect.value, false);
                    break;

                case CardEffectType.IncreaseEnemyUnitAttack:
                    BeginTargetedAttackEffect(data, effect.value, true);
                    break;

                case CardEffectType.DecreaseEnemyUnitAttack:
                    BeginTargetedAttackEffect(data, -effect.value, true);
                    break;

                case CardEffectType.ReadyFriendlyUnit:
                    BeginTargetedReadyEffect(data);
                    break;

                case CardEffectType.ExhaustEnemyUnit:
                    BeginTargetedExhaustEffect(data);
                    break;

                case CardEffectType.IncreaseFriendlyUnitManaCost:
                    BeginTargetedManaCostEffect(data, effect.value);
                    break;

                case CardEffectType.KnockOutEnemyUnitByManaCost:
                    if (!effect.mustResolve && !HasKnockOutTarget(effect.conditionValue))
                    {
                        Debug.Log("Effet facultatif ignoré : aucune unité adverse ne respecte le coût en mana requis pour être mise KO.");
                        break;
                    }

                    BeginTargetedKnockOutEffect(data, effect.conditionValue);
                    break;

                case CardEffectType.RecoverCardFromTrashToHand:
                    BeginTrashRecoveryEffect(data, true);
                    break;

                case CardEffectType.RecoverCardFromTrashToDeck:
                    BeginTrashRecoveryEffect(data, false);
                    break;

                case CardEffectType.DiscardCardFromHandToTrash:
                    BeginHandDiscardEffect(data, effect.mustResolve);
                    break;

                case CardEffectType.DestroyOpponentTerrain:
                    if (opponentUnitSpawner != null)
                    {
                        opponentUnitSpawner.DestroyOpponentTerrain();
                    }
                    else
                    {
                        Debug.LogError("OpponentUnitSpawner n'est pas assigné.");
                    }
                    break;

                case CardEffectType.DestroyAllTerrains:
                    DestroyAllTerrains();
                    break;

                case CardEffectType.GainLife:
                    for (int i = 0; i < effect.value; i++)
                    {
                        if (!playerLifeManager.AddLifeCard())
                            break;
                    }
                    Debug.Log($"{data.cardName} déclenche un effet : gain de {effect.value} carte(s) vie.");
                    break;

                case CardEffectType.LoseLife:
                    for (int i = 0; i < effect.value; i++)
                    {
                        if (!playerLifeManager.TakeLifeDamage())
                            break;
                    }

                    Debug.Log($"{data.cardName} déclenche un effet : perte de {effect.value} carte(s) vie.");
                    break;

                case CardEffectType.RemoveOpponentLife:
                    if (opponentLifeManager == null)
                    {
                        Debug.LogError("GameManager : la pile de vie adverse est manquante !");
                        break;
                    }
                    if (opponentLifeManager.RemainingLives <= effect.conditionValue)
                    {
                        Debug.Log("L'adversaire n'a pas assez de cartes vie : effet annulé.");
                        break;
                    }
                    for (int i = 0; i < effect.value; i++)
                    {
                        if (opponentLifeManager.RemainingLives <= effect.conditionValue)
                            break;
                        opponentLifeManager.TakeLifeDamage();
                    }
                    Debug.Log($"{data.cardName} retire jusqu'à {effect.value} carte(s) vie à l'adversaire.");
                    break;

                default:
                    break;
            }
        }
    }
    private void ResolveSingleCardEffect(CardData data, CardEffectData effect, UnitCardState sourceUnit)
    {
        if (data == null || effect == null)
            return;

        switch (effect.effectType)
        {
            case CardEffectType.DrawCard:
                for (int i = 0; i < effect.value; i++)
                {
                    DrawCard();
                }
                Debug.Log($"{data.cardName} déclenche un effet : pioche de {effect.value} carte(s)");
                break;

            case CardEffectType.AddManaCardsFromDeck:
                if (manaSystem == null)
                {
                    Debug.LogError("ManaSystem n'est pas assigné dans GameManager.");
                    break;
                }
                manaSystem.AddManaCards(effect.value);
                Debug.Log($"{data.cardName} ajoute {effect.value} carte(s) mana depuis le Mana Deck.");
                break;

            case CardEffectType.ReadyManaCards:
                if (manaSystem == null)
                {
                    Debug.LogError("ManaSystem n'est pas assigné dans GameManager.");
                    break;
                }

                manaSystem.ReadyManaCards(effect.value);
                break;

            case CardEffectType.ReturnManaCardsToDeck:
                if (manaSystem == null)
                {
                    Debug.LogError("ManaSystem n'est pas assigné dans GameManager.");
                    break;
                }

                manaSystem.ReturnManaCardsToDeck(effect.value);
                break;

            case CardEffectType.DamageEnemyUnit:
                DamageFirstEnemyUnit(effect.value);
                break;

            case CardEffectType.DamageTargetEnemyUnit:
                BeginTargetedDamageEffect(data, effect.value);
                break;

            case CardEffectType.HealFriendlyUnit:
                BeginTargetedHealEffect(data, effect.value, sourceUnit);
                break;

            case CardEffectType.IncreaseFriendlyChampionAttack:
                championDisplay.ModifyAttack(effect.value);
                Debug.Log($"L'attaque de ton champion a augmentée de {effect.value}");
                break;

            case CardEffectType.DecreaseFriendlyChampionAttack:
                championDisplay.ModifyAttack(-effect.value);
                Debug.Log($"L'attaque de ton champion a diminuée de {effect.value}");
                break;

            case CardEffectType.IncreaseEnemyChampionAttack:
                opponentChampionDisplay.ModifyAttack(effect.value);
                Debug.Log($"L'attaque du Champion adverse a augmentée de {effect.value}.");
                break;

            case CardEffectType.DecreaseEnemyChampionAttack:
                opponentChampionDisplay.ModifyAttack(-effect.value);
                Debug.Log($"L'attaque du Champion adverse a diminuée de {effect.value}.");
                break;

            case CardEffectType.IncreaseFriendlyUnitAttack:
                BeginTargetedAttackEffect(data, effect.value, false);
                break;

            case CardEffectType.DecreaseFriendlyUnitAttack:
                BeginTargetedAttackEffect(data, -effect.value, false);
                break;

            case CardEffectType.IncreaseEnemyUnitAttack:
                BeginTargetedAttackEffect(data, effect.value, true);
                break;

            case CardEffectType.DecreaseEnemyUnitAttack:
                BeginTargetedAttackEffect(data, -effect.value, true);
                break;

            case CardEffectType.ReadyFriendlyUnit:
                BeginTargetedReadyEffect(data);
                break;

            case CardEffectType.ExhaustEnemyUnit:
                BeginTargetedExhaustEffect(data);
                break;

            case CardEffectType.IncreaseFriendlyUnitManaCost:
                BeginTargetedManaCostEffect(data, effect.value);
                break;

            case CardEffectType.KnockOutEnemyUnitByManaCost:
                if (!effect.mustResolve && !HasKnockOutTarget(effect.conditionValue))
                {
                    Debug.Log("Effet facultatif ignoré : aucune unité adverse ne respecte le coût en mana requis pour être mise KO.");
                    break;
                }

                BeginTargetedKnockOutEffect(data, effect.conditionValue);
                break;

            case CardEffectType.RecoverCardFromTrashToHand:
                BeginTrashRecoveryEffect(data, true);
                break;

            case CardEffectType.RecoverCardFromTrashToDeck:
                BeginTrashRecoveryEffect(data, false);
                break;

            case CardEffectType.DiscardCardFromHandToTrash:
                BeginHandDiscardEffect(data, effect.mustResolve);
                break;

            case CardEffectType.DestroyOpponentTerrain:
                if (opponentUnitSpawner != null)
                {
                    opponentUnitSpawner.DestroyOpponentTerrain();
                }
                else
                {
                    Debug.LogError("OpponentUnitSpawner n'est pas assigné.");
                }
                break;

            case CardEffectType.DestroyAllTerrains:
                DestroyAllTerrains();
                break;

            case CardEffectType.GainLife:
                for (int i = 0; i < effect.value; i++)
                {
                    if (!playerLifeManager.AddLifeCard())
                        break;
                }
                Debug.Log($"{data.cardName} déclenche un effet : gain de {effect.value} carte(s) vie.");
                break;

            case CardEffectType.LoseLife:
                for (int i = 0; i < effect.value; i++)
                {
                    if (!playerLifeManager.TakeLifeDamage())
                        break;
                }

                Debug.Log($"{data.cardName} déclenche un effet : perte de {effect.value} carte(s) vie.");
                break;

            case CardEffectType.RemoveOpponentLife:
                if (opponentLifeManager == null)
                {
                    Debug.LogError("GameManager : la pile de vie adverse est manquante !");
                    break;
                }
                if (opponentLifeManager.RemainingLives <= effect.conditionValue)
                {
                    Debug.Log("L'adversaire n'a pas assez de cartes vie : effet annulé.");
                    break;
                }
                for (int i = 0; i < effect.value; i++)
                {
                    if (opponentLifeManager.RemainingLives <= effect.conditionValue)
                        break;
                    opponentLifeManager.TakeLifeDamage();
                }
                Debug.Log($"{data.cardName} retire jusqu'à {effect.value} carte(s) vie à l'adversaire.");
                break;

            default:
                break;
        }
    }
    private void ResolveSpellEffects(CardData data, EffectTrigger trigger)
    {
        if (data == null || data.effects == null)
        {
            CompleteSpellResolution();
            return;
        }

        foreach (CardEffectData effect in data.effects)
        {
            if (effect == null || effect.trigger != trigger)
                continue;

            CardEffectData currentEffect = effect;

            pendingEffects.Enqueue(() =>
                ResolveSingleCardEffect(data, currentEffect, null)
            );
        }

        ProcessNextPendingEffect();
    }
    private void DamageFirstEnemyUnit(int damage) {
        if (opponentUnitSpawner == null)
        {
            Debug.LogError("OpponentUnitSpawner n'est pas assigné.");
            return;
        }

        UnitCardState[] enemyUnits = opponentUnitSpawner.GetOpponentUnits();

        foreach (UnitCardState enemy in enemyUnits)
        {
            if (enemy == null)
                continue;

            enemy.TakeDamage(damage);

            Debug.Log($"{enemy.name} reçoit {damage} dégâts grâce à un effet.");
            return;
        }

        Debug.Log("Aucune unité adverse à cibler.");
    }
    private void OnHandCardSelected(CardView card)
    {
        if (!waitingForHandDiscard || card == null)
            return;

        if (playerTrashManager == null)
        {
            Debug.LogError("Le TrashManager du joueur n'est pas assigné.");
            return;
        }

        CardData data = card.Data;

        // Retirer la carte de la main.
        playerHand.RemoveCard(card);

        // Envoyer la carte dans la Trash.
        playerTrashManager.AddCard(card);

        waitingForHandDiscard = false;
        cancelEffectButton?.SetActive(false);

        Debug.Log($"{data.cardName} a été défaussée depuis la main.");

        CompleteOncePerTurnActivation();
        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
    }
    private void ResolveTurnEffectsForZone(Transform unitZone, EffectTrigger trigger)
    {
        if (unitZone == null) return;

        foreach (Transform unitTransform in unitZone)
        {
            UnitCardState unitState = unitTransform.GetComponent<UnitCardState>();
            CardView cardView = unitTransform.GetComponent<CardView>();

            if (unitState == null || cardView == null || cardView.Data == null)
                continue;

            CardData data = cardView.Data;
            if (data.effects == null) continue;

            foreach (CardEffectData effect in data.effects)
            {
                if (effect == null || effect.trigger != trigger)
                    continue;

                CardEffectData currentEffect = effect;
                CardData currentData = data;
                UnitCardState currentSource = unitState;

               EnqueueEffect(() => {ResolveSingleCardEffect(currentData, currentEffect, currentSource);});
            }
        }
        ProcessNextPendingEffect();
    }
    private void OnTrashCardSelected(CardData cardData)
    {
        if (!waitingForTrashCard || cardData == null)
            return;

        bool success = false;

        if (recoverTrashCardToHand)
        {
            success = playerTrashManager.RecoverCardToHand(cardData, playerHand);
        }
        else if (recoverTrashCardToDeck)
        {
            success = playerTrashManager.RecoverCardToDeck(cardData, playerDeck);
        }

        if (!success)
        {
            Debug.LogWarning("Impossible de récupérer cette carte depuis la Trash.");
            return;
        }

        CompleteOncePerTurnActivation();

        waitingForTrashCard = false;
        recoverTrashCardToHand = false;
        recoverTrashCardToDeck = false;

        trashViewer.OnCardSelected -= OnTrashCardSelected;
        trashViewer.CloseTrash();
        cancelEffectButton?.SetActive(false);

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
    }
    private void CancelPendingEffect()
    {
        if (!waitingForEffectTarget)
            return;

        waitingForEffectTarget = false;
        cancelEffectButton?.SetActive(false);
        pendingEffectDamage = 0;
        pendingEffectCard = null;

        if(pendingSpellCard != null)
        {
            CancelSpellResolution();
            Debug.Log("Le sort a été annulé.");
            CheckPendingPlayerTurnCompletion();
            return;
        }

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();

        Debug.Log("L'effet ciblé a été annulé : aucune cible sélectionnée.");
    }
    private void CancelSpellResolution()
    {
        if (pendingSpellCard == null)
            return;

        CardView cancelledSpell = pendingSpellCard;
        pendingSpellCard = null;
        IsResolvingCounterSpell = false;

        pendingEffects.Clear();

        playerHand.AddExistingCard(cancelledSpell);

        manaSystem.RefundMana(cancelledSpell.Data.manaCost);

        Debug.Log($"{cancelledSpell.Data.cardName} a été annulé et retourné dans la main.");
    }
    private void CancelPendingHeal()
    {
        if (!waitingForHealTarget)
            return;

        waitingForHealTarget = false;
        cancelEffectButton?.SetActive(false);
        pendingHealAmount = 0;
        pendingHealCard = null;
        pendingHealSource = null;

        if (pendingSpellCard != null)
        {
            CancelSpellResolution();
            Debug.Log("Le sort de soin a été annulé.");
            CheckPendingPlayerTurnCompletion();
            return;
        }

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();

        Debug.Log("L'effet de soin a été annulé.");
    }
    private void CancelPendingBoostAttack()
    {
        if (!waitingForAttackTarget)
            return;

        waitingForAttackTarget = false;
        pendingAttackAmount = 0;
        pendingReadyEffect = false;
        pendingExhaustEffect = false;
        pendingManaCostEffect = false;
        pendingKnockOutEffect = false;
        pendingKnockOutManaThreshold = 0;

        cancelEffectButton?.SetActive(false);

        if (pendingSpellCard != null)
        {
            CancelSpellResolution();
            Debug.Log("Le sort de modification d'état a été annulé.");
            CheckPendingPlayerTurnCompletion();
            return;
        }

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
        Debug.Log("Sélection de la cible pour la modification d'etat annulée.");
    }
    private void CancelPendingTrashRecovery()
    {
        if (!waitingForTrashCard)
            return;

        waitingForTrashCard = false;
        recoverTrashCardToHand = false;
        recoverTrashCardToDeck = false;

        trashViewer.OnCardSelected -= OnTrashCardSelected;
        trashViewer.CloseTrash();

        cancelEffectButton?.SetActive(false);

        if (pendingSpellCard != null)
        {
            CancelSpellResolution();
            Debug.Log("Le sort de récupération a été annulé.");
            CheckPendingPlayerTurnCompletion();
            return;
        }

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();

        Debug.Log("Récupération depuis la Trash annulée.");
    }
    private void CancelPendingHandDiscard()
    {
        if (!waitingForHandDiscard)
            return;

        waitingForHandDiscard = false;
        cancelEffectButton?.SetActive(false);

        if (pendingSpellCard != null)
        {
            CancelSpellResolution();
            Debug.Log("La défausse et la résolution du sort ont été annulées.");
            CheckPendingPlayerTurnCompletion();
            return;
        }

        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();

        Debug.Log("Défausse annulée : aucune carte n'a été envoyée dans la Trash.");
    }
    private void CancelOncePerTurnActivation()
    {
        if (!resolvingOncePerTurnEffect)
            return;

        resolvingOncePerTurnEffect = false;
        pendingOncePerTurnSource = null;
        pendingOncePerTurnTerrainSource = null;
        pendingOncePerTurnEffectIndex = -1;

        UpdateActivateEffectButton();

        Debug.Log("Activation OncePerTurn annulée : aucun coût payé.");
    }
    private void CompletePlayerTurnStart()
    {
        DrawCard();
        manaSystem.AddManaCards(2);

        if (combatManager != null)
            combatManager.ClearCombatSelections();

        Debug.Log($"Tour {turnNumber} du joueur commencé.");
        Debug.Log($"Attaque autorisée : {CanAttack}");
    }
    private void ReadyAllUnit()
    {
        // Remettre le champion du joueur en Ready.
        if (championDisplay != null && championDisplay.ChampionState != null)
        {
            championDisplay.ChampionState.Ready();
            championDisplay.ChampionState.ResetOncePerTurnEffects();
            Debug.Log("Champion du joueur remis en Ready.");
        }

        // Remettre les unités du joueur en Ready.
        if (playerUnitZone != null)
        {
            foreach (Transform unitTransform in playerUnitZone)
            {
                UnitCardState unitState = unitTransform.GetComponent<UnitCardState>();

                if (unitState != null)
                {
                    unitState.Ready();
                    unitState.ResetOncePerTurnEffects();
                    unitState.ResetAttack();
                    unitState.ResetManaCost();

                    Debug.Log($"Unité prête : attaque réinitialisée à {unitState.CurrentAttack}.");
                }
            }
        }

        // Redresser le terrain et réinitialiser ses effets OncePerTurn.
        if (playerTerrainZone != null)
        {
            TerrainCardState terrainState =
                playerTerrainZone.GetComponentInChildren<TerrainCardState>();

            if (terrainState != null)
            {
                terrainState.Ready();
                terrainState.ResetOncePerTurnEffects();

                Debug.Log("Terrain du joueur redressé : effets OncePerTurn réinitialisés.");
            }
        }

        UpdateActivateEffectButton();
    }
    private void CompleteOncePerTurnActivation()
    {
        if (!resolvingOncePerTurnEffect || (pendingOncePerTurnSource == null && pendingOncePerTurnTerrainSource == null))
            return;

        CardView cardView = pendingOncePerTurnTerrainSource != null ? pendingOncePerTurnTerrainSource.GetComponent<CardView>() : pendingOncePerTurnSource.GetComponent<CardView>();

        if (cardView == null || cardView.Data == null)
            return;

        if (cardView.Data.effects == null || pendingOncePerTurnEffectIndex < 0 || pendingOncePerTurnEffectIndex >= cardView.Data.effects.Length)
        {
            Debug.LogError("Index de l'effet OncePerTurn invalide.");
            return;
        }

        CardEffectData effect = cardView.Data.effects[pendingOncePerTurnEffectIndex];

        if (effect == null)
            return;

        if (effect.activationCost == ActivationCostType.ExhaustCard)
        {
            if(pendingOncePerTurnTerrainSource != null)
                pendingOncePerTurnTerrainSource.Exhaust();
            else
                pendingOncePerTurnSource.Exhaust();
        }
        else if (effect.activationCost == ActivationCostType.SpendMana)
        {
            if (manaSystem == null || !manaSystem.ExhaustMana(effect.activationManaCost))
            {
                Debug.LogError("Impossible de payer le coût en mana de l'effet OncePerTurn.");

                resolvingOncePerTurnEffect = false;
                pendingOncePerTurnSource = null;
                pendingOncePerTurnTerrainSource = null;
                pendingOncePerTurnEffectIndex = -1;

                return;
            }
        }

        if (pendingOncePerTurnTerrainSource != null)
            pendingOncePerTurnTerrainSource.MarkEffectAsUsed(pendingOncePerTurnEffectIndex);
        else
            pendingOncePerTurnSource.MarkEffectAsUsed(pendingOncePerTurnEffectIndex);

        resolvingOncePerTurnEffect = false;
        pendingOncePerTurnSource = null;
        pendingOncePerTurnTerrainSource = null;
        pendingOncePerTurnEffectIndex = -1;

        UpdateActivateEffectButton();

        Debug.Log("Effet OncePerTurn terminé et marqué comme utilisé.");
    }
    private void CompleteSpellResolution()
    {
        if (pendingSpellCard == null)
            return;

        playerTrashManager.AddCard(pendingSpellCard);

        Debug.Log($"{pendingSpellCard.Data.cardName} a été envoyé dans la Trash.");

        pendingSpellCard = null;
        IsResolvingCounterSpell = false;
    }
    private bool HasKnockOutTarget(int manaThreshold)
    {
        if (opponentUnitSpawner == null)
            return false;

        UnitCardState[] enemyUnits = opponentUnitSpawner.GetOpponentUnits();

        foreach (UnitCardState enemy in enemyUnits)
        {
            if (enemy != null && enemy.CurrentManaCost < manaThreshold)
                return true;
        }

        return false;
    }
    private bool CanActivateOncePerTurn(UnitCardState source)
    {
        if (source == null)
            return false;

        CardView cardView = source.GetComponent<CardView>();

        if (cardView == null || cardView.Data == null || cardView.Data.effects == null)
            return false;

        for (int i = 0; i < cardView.Data.effects.Length; i++)
        {
            CardEffectData effect = cardView.Data.effects[i];

            if (effect == null || effect.trigger != EffectTrigger.OncePerTurn)
                continue;

            if (source.HasUsedEffectThisTurn(i))
                return false;

            if (effect.activationCost == ActivationCostType.ExhaustCard && source.IsExhausted)
                return false;

            if (effect.activationCost == ActivationCostType.SpendMana)
            {
                if (manaSystem == null || manaSystem.AvailableMana < effect.activationManaCost)
                    return false;
            }

            return true;
        }

        return false;
    }
    private bool CanActivateOncePerTurn(TerrainCardState source)
    {
        if (source == null)
            return false;

        CardView cardView = source.GetComponent<CardView>();

        if (cardView == null || cardView.Data == null || cardView.Data.effects == null)
            return false;

        for (int i = 0; i < cardView.Data.effects.Length; i++)
        {
            CardEffectData effect = cardView.Data.effects[i];

            if (effect == null || effect.trigger != EffectTrigger.OncePerTurn)
                continue;

            if (source.HasUsedEffectThisTurn(i))
                return false;

            if (effect.activationCost == ActivationCostType.ExhaustCard && source.IsExhausted)
                return false;

            if (effect.activationCost == ActivationCostType.SpendMana)
            {
                if (manaSystem == null || manaSystem.AvailableMana < effect.activationManaCost)
                    return false;
            }

            return true;
        }

        return false;
    }
    private UnitCardState GetSelectedOncePerTurnSource()
    {
        if (combatManager != null && combatManager.PlayerChampionSelected)
        {
            return championDisplay != null ? championDisplay.ChampionState : null;
        }

        if (UnitSlectionManager.Instance != null)
        {
            return UnitSlectionManager.Instance.SelectedUnit;
        }

        return null;
    }

}
