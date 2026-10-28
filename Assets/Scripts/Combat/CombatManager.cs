using UnityEngine;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UnitSlectionManager unitSlectionManager;
    [SerializeField] private LifeZoneManager playerLifeManagerForBlocker;

    [SerializeField] private OpponentChampionDisplay opponentChampionDisplay;
    [SerializeField] private LifeZoneManager opponentLifeManager;
    [SerializeField] private OpponentUnitSpawner opponentUnitSpawner;

    private bool opponentChampionSelected;

    // Gestion de l'interception
    private bool waitingForBlocker;
    private UnitCardState incomingAttacker;
    private UnitCardState incomingTarget;
    private bool incomingAttackTargetsChampion;

    public static CombatManager Instance { get; private set; }

    public UnitCardState SelectedTarget { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }
    
    public void SelectOpponentChampion()
    {
        if (opponentChampionSelected)
        {
            ClearOpponentChampionSelection();
            return;
        }

        ClearTargetSelection();

        if(opponentChampionDisplay != null && opponentChampionDisplay.ChampionView != null)
        {
            opponentChampionDisplay.ChampionView.SetSelected(true);
        }
        opponentChampionSelected = true;
        Debug.Log("Champion adverse sélectionné comme cible !");
    }

    private void ClearOpponentChampionSelection()
    {
        if (opponentChampionDisplay != null && opponentChampionDisplay.ChampionView != null)
        {
            opponentChampionDisplay.ChampionView.SetSelected(false);
        }

        opponentChampionSelected = false;
    }

    public void SelectTarget(UnitCardState target)
    {
        if (target == null) return;

        ClearOpponentChampionSelection();

        if (SelectedTarget == target)
        {
            ClearTargetSelection();
            return;
        }

        // Retire la surbrillance de la cible précédemment sélectionnée
        if (SelectedTarget != null)
        {
            SelectedTarget.GetComponent<CardView>()?.SetSelected(false);
        }

        SelectedTarget = target;
        SelectedTarget.GetComponent<CardView>()?.SetSelected(true);

        CardView targetView = target.GetComponent<CardView>();
        Debug.Log($"Cible adverse sélectionnée : {(targetView != null && targetView.Data != null ? targetView.Data.cardName : target.gameObject.name)}");
    }

    public void ClearTargetSelection()
    {
        if(SelectedTarget != null)
        {
            SelectedTarget.GetComponent<CardView>()?.SetSelected(false);
        }
        SelectedTarget = null;
    }

    public void ExhaustEnemi()
    {
        if (SelectedTarget == null)
        {
            Debug.LogWarning("Aucune cible sélectionnée pour l'attaque.");
            return;
        }

        SelectedTarget.Exhaust();
    }

    public void StartTestOpponentAttack(UnitCardState attacker,UnitCardState target,bool targetsChampion)
    {
        if (attacker == null)
        {
            Debug.LogWarning("Aucun attaquant adverse fourni.");
            return;
        }

        if (!targetsChampion && target == null)
        {
            Debug.LogWarning("Aucune cible n'est définie pour l'attaque adverse.");
            return;
        }

        if (waitingForBlocker)
        {
            Debug.LogWarning("Une attaque est déjà en attente d'interception.");
            return;
        }

        incomingAttacker = attacker;
        incomingTarget = target;
        incomingAttackTargetsChampion = targetsChampion;

        waitingForBlocker = true;

        Debug.Log("Attaque adverse déclarée : clique sur une unité avec Blocker pour intercepter.");
    }

    public void ClearCombatSelections()
    {
        ClearTargetSelection();
        ClearOpponentChampionSelection();

        if (unitSlectionManager != null)
        {
            unitSlectionManager.ClearSelection();
        }

        Debug.Log("Sélections de combat réinitialisées.");
    }

    public void TryAttack(UnitCardState attacker)
    {
        if(attacker == null)
        {
            Debug.LogWarning("Aucune unité attaquante sélectionnée.");
            return;
        }
        if (gameManager == null) {
            Debug.LogError("GameManager n'est pas assigné dans le CombatManager.");
            return;
        }
        if (!gameManager.CanAttack)
        {
            Debug.Log("TU ne peux pas attaquer pendant ce tour!");
            return;
        }
        if(!attacker.CanAttackOnTurn(gameManager.TurnNumber))
        {
            Debug.Log("Cette unité ne peut pas attaquer ce tour.");
            return;
        }

        Debug.Log($"Attaque autorisé avec : {attacker.GetComponent<CardView>()?.Data.cardName} !");
    }

    public void TryAttackSelectedUnit()
    {
        if(unitSlectionManager == null || gameManager == null)
        {
            Debug.LogError("UnitSlectionManager ou GameManager n'est pas assigné dans le CombatManager.");
            return;
        }

        UnitCardState attacker = unitSlectionManager.SelectedUnit;

        if(attacker == null)
        {
            Debug.LogWarning("Aucune unité sélectionnée pour attaquer.");
            return;
        }

        if(!gameManager.CanAttack)
        {
            Debug.Log("Tu ne peux pas attaquer pendant ce tour!");
            return;
        }

        if(!attacker.CanAttackOnTurn(gameManager.TurnNumber))
        {
            Debug.Log("Cette unité ne peut pas attaquer ce tour.");
            return;
        }

        CardView attackerView = attacker.GetComponent<CardView>();

        if (attackerView == null || attackerView.Data == null)
        {
            Debug.LogError("Les données de l'attaquant ou de la cible sont manquantes.");
            return;
        }

        // Cas 1: Attaque du champion adverse
        if (opponentChampionSelected)
        {
            if(opponentLifeManager == null)
            {
                Debug.LogError("LifeZoneManager adverse non assigné !");
                return;
            }

            if (opponentLifeManager.RemainingLives <= 0)
            {
                attacker.Exhaust();
                ClearCombatSelections();
                Debug.Log("Victoire ! Le Champion adverse n'a plus de cartes de vie.");
                return;
            }

            bool lifeTaken = opponentLifeManager.TakeLifeDamage();

            if (!lifeTaken)
            {
                Debug.LogWarning("Impossible de retirer la carte de vie adverse.");
                return;
            }

            attacker.Exhaust();
            ClearCombatSelections();

            Debug.Log("Attaque directe réussie : une carte de vie adverse rejoint sa main.");
            return;
        }

        // Cas 2: Attaque d'une unité adverse sélectionnée
        if (SelectedTarget == null)
        {
            Debug.LogWarning("Aucune cible sélectionnée pour l'attaque.");
            return;
        }

        if (!SelectedTarget.IsExhausted)
        {
            Debug.Log("Cette unité n'est pas épuisée : elle ne peut pas être attaquée.");
            return;
        }

        CardView targetView = SelectedTarget.GetComponent<CardView>();
        Debug.Log($"Attaque autorisée : {attackerView?.Data.cardName} attaque {targetView?.Data.cardName} !");

        if (targetView == null || targetView.Data == null)
        {
            Debug.LogError("Les données de la cible sont manquantes.");
            return;
        }

        int damage = attackerView.Data.attack - targetView.Data.attack;

        if (damage <= 0)
        {
            Debug.Log($"L'attaque ({attackerView.Data.attack}) ne dépasse pas la défense de la cible ({targetView.Data.attack}). Aucune perte de vie.");
        }
        else
        {
            SelectedTarget.TakeDamage(damage);
            Debug.Log($"L'unité adverse subit {damage} dégâts.");
        }

        attacker.Exhaust();

        if(SelectedTarget != null && SelectedTarget.CurrentHealth <= 0)
        {
            ClearTargetSelection();
        }

        ClearCombatSelections();
        Debug.Log("Attaque terminée!");
    }

    public void TryInterceptWith(UnitCardState blocker)
    {
        if (!waitingForBlocker)
        {
            Debug.LogWarning("Aucune attaque n'attend d'interception.");
            return;
        }

        if (blocker == null || !blocker.CanIntercept())
        {
            Debug.Log("Cette unité ne peut pas intercepter : elle doit être dégagée et posséder Blocker.");
            return;
        }

        Debug.Log($"Attaque adverse interceptée par {blocker.GetComponent<CardView>()?.Data.cardName}");

        ResolveOpponentAttack(blocker, false);
    }

    private void ResolveOpponentAttack(UnitCardState target,bool targetsChampion)
    {
        if (!waitingForBlocker)
            return;

        if (incomingAttacker == null)
        {
            Debug.LogError("L'attaquant adverse est introuvable.");
            ResetIncomingAttack();
            return;
        }

        if (targetsChampion)
        {
            if (playerLifeManagerForBlocker == null)
            {
                Debug.LogError("LifeZoneManager du joueur non assigné.");
                ResetIncomingAttack();
                return;
            }

            if (playerLifeManagerForBlocker.RemainingLives <= 0)
            {
                Debug.Log("Défaite ! Ton Champion n'a plus de cartes de vie.");
            }
            else
            {
                playerLifeManagerForBlocker.TakeLifeDamage();
                Debug.Log("Ton Champion perd une carte de vie.");
            }
        }
        else
        {
            if (target == null)
            {
                Debug.LogError("La cible de l'attaque est introuvable.");
                ResetIncomingAttack();
                return;
            }

            CardView attackerView = incomingAttacker.GetComponent<CardView>();
            CardView targetView = target.GetComponent<CardView>();

            if (attackerView == null || attackerView.Data == null || targetView == null || targetView.Data == null)
            {
                Debug.LogError("Données de combat manquantes.");
                ResetIncomingAttack();
                return;
            }

            int damage = attackerView.Data.attack - targetView.Data.attack;

            if (damage > 0)
                target.TakeDamage(damage);

            Debug.Log($"Combat terminé. Différence d'attaque : {damage}.");
        }

        if (incomingAttacker != null)
            incomingAttacker.Exhaust();

        if (target != null && target != incomingAttacker && target.CurrentHealth > 0)
            target.Exhaust();

        ResetIncomingAttack();
    }

    private void ResetIncomingAttack()
    {
        waitingForBlocker = false;
        incomingAttacker = null;
        incomingTarget = null;
        incomingAttackTargetsChampion = false;
    }
    public bool IsWaitingForBlocker()
    {
        return waitingForBlocker;
    }

    public void ResolveIncomingAttackWithoutBlocker()
    {
        if (!waitingForBlocker)
        {
            Debug.LogWarning("Aucune attaque adverse en attente.");
            return;
        }

        if (incomingAttackTargetsChampion)
        {
            if (playerLifeManagerForBlocker == null)
            {
                Debug.LogError("La zone de vie du joueur n'est pas assignée.");
                ResetIncomingAttack();
                return;
            }

            if (playerLifeManagerForBlocker.RemainingLives <= 0)
            {
                Debug.Log("Défaite : ton Champion n'a plus de cartes de vie.");
            }
            else
            {
                playerLifeManagerForBlocker.TakeLifeDamage();
                Debug.Log("Attaque adverse : une carte de vie est perdue.");
            }
        }
        else
        {
            if (incomingTarget == null)
            {
                Debug.LogError("La cible de l'attaque adverse est introuvable.");
                ResetIncomingAttack();
                return;
            }

            CardView attackerView = incomingAttacker.GetComponent<CardView>();
            CardView targetView = incomingTarget.GetComponent<CardView>();

            if (attackerView == null || attackerView.Data == null ||
                targetView == null || targetView.Data == null)
            {
                Debug.LogError("Données de combat manquantes.");
                ResetIncomingAttack();
                return;
            }

            int damage = attackerView.Data.attack - targetView.Data.attack;

            if (damage > 0)
                incomingTarget.TakeDamage(damage);

            incomingTarget.Exhaust();

            Debug.Log($"Attaque adverse résolue. Différence d'attaque : {damage}.");
        }

        if (incomingAttacker != null)
            incomingAttacker.Exhaust();

        ResetIncomingAttack();
    }

    public void TestOpponentAttackChampion()
    {
        if (opponentUnitSpawner == null)
        {
            Debug.LogError("OpponentUnitSpawner n'est pas assigné dans CombatManager.");
            return;
        }

        UnitCardState attacker = opponentUnitSpawner.LastSpawnedUnit;

        if (attacker == null)
        {
            Debug.LogWarning("Crée d'abord une unité adverse pour tester son attaque.");
            return;
        }

        StartTestOpponentAttack(attacker, null, true);
    }
}
