using UnityEngine;

public class OpponentUnitSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CardData unitToSpawn;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform opponentUnitZone;

    [Header("Défausse")]
    [SerializeField] private TrashManager trashManager;

    public UnitCardState LastSpawnedUnit { get; private set; }

    public void SpawnUnit()
    {
        if (unitToSpawn == null)
        {
            Debug.LogError("Aucune unité de test assignée à l'adversaire!");
            return;
        }
        
        if(unitToSpawn.cardType != CardType.Unit)
        {
            Debug.LogError("L'unité de test assignée n'est pas de type Unit !");
            return;
        }

        if(cardPrefab == null || opponentUnitZone == null)
        {
            Debug.LogError("Aucun prefab de carte ou zone d'unité adverse assigné !");
            return;
        }

        CardView unit = Instantiate(cardPrefab, opponentUnitZone);
        unit.SetCardData(unitToSpawn);

        unit.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        unit.transform.localScale = Vector3.one;

        UnitCardState state = unit.GetComponent<UnitCardState>();

        if (state == null)
        {
            state = unit.gameObject.AddComponent<UnitCardState>();
        }

        if(unit.GetComponent<OpponentUnitClickHandler>() == null)
        {
            unit.gameObject.AddComponent<OpponentUnitClickHandler>();
        }

        state.SetTrashManager(trashManager);
        state.Initialize(0);

        LastSpawnedUnit = state;
        Debug.Log($"Unité adverse créée: {unitToSpawn.cardName}, rotation : {unit.transform.localEulerAngles}  ");
    }


    public void ReadyAllOpponentUnits()
    {
        if (opponentUnitZone == null)
        {
            Debug.LogWarning("La zone des unités adverses n'est pas assignée.");
            return;
        }

        foreach (Transform child in opponentUnitZone)
        {
            UnitCardState state = child.GetComponent<UnitCardState>();

            if (state != null)
            {
                state.Ready();
            }
        }

        Debug.Log("Toutes les unités adverses sont prêtes.");
    }


    public UnitCardState[] GetOpponentUnits()
    {
        if (opponentUnitZone == null)
        {
            Debug.LogWarning("La zone des unités adverses n'est pas assignée.");
            return System.Array.Empty<UnitCardState>();
        }

        System.Collections.Generic.List<UnitCardState> units =
            new System.Collections.Generic.List<UnitCardState>();

        foreach (Transform child in opponentUnitZone)
        {
            UnitCardState state = child.GetComponent<UnitCardState>();

            if (state != null)
                units.Add(state);
        }

        return units.ToArray();
    }

}
