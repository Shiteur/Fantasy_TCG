
using System.Collections;
using UnityEngine;

public class OpponentTurnManager : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private OpponentUnitSpawner opponentUnitSpawner;
    [SerializeField] private CombatManager combatManager;

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

            Debug.Log("L'adversaire attaque ton Champion.");

            combatManager.StartTestOpponentAttack(
                attacker,
                null,
                true
            );

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
}
