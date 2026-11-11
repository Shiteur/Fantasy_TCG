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

        if(CurrentTurnOwner == TurnOwner.Player)
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
    public bool IsWaitingForEffectTarget()
    {
        return waitingForEffectTarget;
    }
    public bool IsWaitingForHealTarget()
    {
        return waitingForHealTarget;
    }
    public void ResolveCardEffectsOnDeath(CardData data, UnitCardState unit)
    {
        ResolveCardEffects(data, EffectTrigger.OnDeath, unit);
    }
    public void ResolveTurnEffects(EffectTrigger trigger)
    {
        ResolveTurnEffectsForZone(playerUnitZone, trigger);
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

        Debug.Log($"Effet de {pendingEffectCard.cardName} : {pendingEffectDamage} dégâts infligés à {target.name}.");

        pendingEffectCard = null;
        pendingEffectDamage = 0;
        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();
    }
    public void CancelCurrentEffect()
    {
        if (waitingForEffectTarget)
        {
            CancelPendingEffect();
            return;
        }

        if (waitingForHealTarget)
        {
            CancelPendingHeal();
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

            if (waitingForEffectTarget || waitingForHealTarget)
            {
                resolvingEffectQueue = false;
                return;
            }
        }

        resolvingEffectQueue = false;
    }
    private void CompletePlayerTurn()
    {
        waitingToFinishPlayerTurn = false;

        CurrentTurnOwner = TurnOwner.Opponent;
        
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
        if (sourceUnit == null)
        {
            Debug.LogWarning("Impossible d'identifier l'unité source du soin.");
            return;
        }

        if (playerUnitZone == null || opponentUnitZone == null)
        {
            Debug.LogError("Une des zones d'unités n'est pas assignée.");
            return;
        }

        Transform friendlyZone = sourceUnit.transform.IsChildOf(playerUnitZone) ? playerUnitZone : opponentUnitZone;

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
        healTargetsOpponent = !sourceUnit.transform.IsChildOf(playerUnitZone);

        Debug.Log($"Choisis une unité alliée à soigner avec l'effet de {data.cardName}.");
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
                    for(int i=0; i< effect.value; i++)
                    {
                        DrawCard();
                    }
                    Debug.Log($"{data.cardName} déclenche un effet : pioche de {effect.value} carte(s)");
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

            case CardEffectType.DamageEnemyUnit:
                DamageFirstEnemyUnit(effect.value);
                break;

            case CardEffectType.DamageTargetEnemyUnit:
                BeginTargetedDamageEffect(data, effect.value);
                break;

            case CardEffectType.HealFriendlyUnit:
                BeginTargetedHealEffect(data, effect.value, sourceUnit);
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
    private void CancelPendingEffect()
    {
        if (!waitingForEffectTarget)
            return;

        waitingForEffectTarget = false;
        cancelEffectButton?.SetActive(false);
        pendingEffectDamage = 0;
        pendingEffectCard = null;
        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();

        Debug.Log("L'effet ciblé a été annulé : aucune cible sélectionnée.");
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
        ProcessNextPendingEffect();
        CheckPendingPlayerTurnCompletion();

        Debug.Log("L'effet de soin a été annulé.");
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
