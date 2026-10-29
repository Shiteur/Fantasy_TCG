
using System.Collections;
using UnityEngine;

public class OpponentTurnManager : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private OpponentUnitSpawner opponentUnitSpawner;
    [SerializeField] private CombatManager combatManager;
    [SerializeField] private Transform playerUnitZone;

    [Header("Délais")]
    [SerializeField] private float delayBeforeAttack = 1f;
    [SerializeField] private float delayBetweenAttacks = 1.5f;

    private Coroutine opponentTurnCoroutine;

    public void StartOpponentTurn()
    {
        if (gameManager == null || opponentUnitSpawner == null || combatManager == null)
        {
            Debug.LogError("GameManager, OpponentUnitSpawner ou CombatManager n'est pas assigné.");
            return;
        }

        if (gameManager.CurrentTurnOwner != TurnOwner.Opponent)
            return;

        opponentUnitSpawner.ReadyAllOpponentUnits();

        if (opponentTurnCoroutine != null)
            StopCoroutine(opponentTurnCoroutine);

        opponentTurnCoroutine = StartCoroutine(ResolveOpponentTurn());
    }

    private IEnumerator ResolveOpponentTurn()
    {
        Debug.Log("L'adversaire réfléchit...");

        yield return new WaitForSeconds(delayBeforeAttack);

        UnitCardState[] units = opponentUnitSpawner.GetOpponentUnits();

        foreach (UnitCardState attacker in units)
        {
            if (attacker == null)
                continue;

            if (gameManager.CurrentTurnOwner != TurnOwner.Opponent)
                break;

            if (attacker.IsExhausted || !attacker.CanAttackOnTurn(gameManager.TurnNumber))
                continue;

            UnitCardState target = FindExhaustedPlayerUnit();

            if(target != null)
            {
                Debug.Log("L'adversaire attaque une Unité épuisée.");
                combatManager.StartTestOpponentAttack(attacker, target, false);
            }
            else
            {
                Debug.Log("L'adversaire attaque ton Champion.");

                combatManager.StartTestOpponentAttack(attacker, null, true);
            }

                // On attend la résolution de l'attaque avant de poursuivre.
                while (combatManager.IsWaitingForBlocker())
                    yield return null;

            yield return new WaitForSeconds(delayBetweenAttacks);
        }

        if (gameManager != null && gameManager.CurrentTurnOwner == TurnOwner.Opponent)
        {
            gameManager.EndOpponentTurn();
        }

        opponentTurnCoroutine = null;
    }

    private UnitCardState FindExhaustedPlayerUnit()
    {
        if(playerUnitZone == null)
        {
            Debug.LogWarning("playerUnitZone n'est pas assigné.");
            return null;
        }

        foreach(Transform child in playerUnitZone)
        {
            UnitCardState unit = child.GetComponent<UnitCardState>();

            if(unit != null && unit.IsExhausted)
            {
                return unit;
            }
        }

        return null;
    }
}
