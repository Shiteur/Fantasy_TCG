using UnityEngine;

public class OpponentUnitSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CardData unitToSpawn;
    [SerializeField] private CardView cardPrefab;
    [SerializeField] private Transform opponentUnitZone;
    [SerializeField] private GameManager gameManager;

    [Header("Terrain de test")]
    [SerializeField] private CardData terrainToSpawn;
    [SerializeField] private Transform opponentTerrainZone;

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

        OpponentUnitClickHandler clickHandler = unit.GetComponent<OpponentUnitClickHandler>();

        if (clickHandler == null)
        {
            clickHandler = unit.gameObject.AddComponent<OpponentUnitClickHandler>();
        }

        clickHandler.SetGameManager(gameManager);

        state.SetTrashManager(trashManager);
        state.SetGameManager(gameManager);
        state.Initialize(0);

        LastSpawnedUnit = state;
        Debug.Log($"Unité adverse créée: {unitToSpawn.cardName}, rotation : {unit.transform.localEulerAngles}  ");
    }

    public void SpawnTerrain()
    {
        if (terrainToSpawn == null)
        {
            Debug.LogError("Aucun terrain de test assigné à l'adversaire !");
            return;
        }

        if (terrainToSpawn.cardType != CardType.Terrain)
        {
            Debug.LogError("La carte assignée n'est pas de type Terrain !");
            return;
        }

        if (cardPrefab == null || opponentTerrainZone == null)
        {
            Debug.LogError("Le prefab ou la zone Terrain adverse n'est pas assigné !");
            return;
        }

        // Vérifier s'il y a un terrain déjà actif.
        CardView previousTerrain = opponentTerrainZone.GetComponentInChildren<CardView>();

        if (previousTerrain != null && trashManager == null)
        {
            Debug.LogError("Le TrashManager adverse n'est pas assigné !");
            return;
        }

        // Envoyer l'ancien terrain dans la Trash adverse s'il existe.
        if (previousTerrain != null)
        {
            Debug.Log($"{previousTerrain.Data.cardName} est remplacé et envoyé dans la Trash adverse.");
            trashManager.AddCard(previousTerrain);
        }

        // Ajouter le nouveau terrain.
        CardView terrain = Instantiate(cardPrefab, opponentTerrainZone);
        terrain.SetCardData(terrainToSpawn);

        terrain.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);
        terrain.transform.localScale = Vector3.one;

        TerrainCardState terrainState = terrain.GetComponent<TerrainCardState>();

        if (terrainState == null)
        {
            terrainState = terrain.gameObject.AddComponent<TerrainCardState>();
        }

        terrainState.Initialize();
        Debug.Log($"Terrain adverse créé : {terrainToSpawn.cardName}");

        if (gameManager != null)
        {
            gameManager.ResolveOpponentTerrainOnPlay(terrainToSpawn);
        }
        else
        {
            Debug.LogError("GameManager n'est pas assigné dans OpponentUnitSpawner.");
        }
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
                state.ResetOncePerTurnEffects();
                state.ResetAttack();
                state.ResetManaCost();
                Debug.Log($"Attaque de l'unité adverse réinitialisée : {state.CurrentAttack}");
            }
        }

        Debug.Log("Toutes les unités adverses sont prêtes.");
    }

    public bool DestroyOpponentTerrain()
    {
        if (opponentTerrainZone == null)
        {
            Debug.LogError("La zone Terrain adverse n'est pas assignée.");
            return false;
        }

        if (trashManager == null)
        {
            Debug.LogError("Le TrashManager adverse n'est pas assigné.");
            return false;
        }

        CardView terrain = opponentTerrainZone.GetComponentInChildren<CardView>();

        if (terrain == null)
        {
            Debug.Log("L'adversaire ne possède aucun terrain actif.");
            return false;
        }

        string terrainName = terrain.Data != null
            ? terrain.Data.cardName
            : terrain.gameObject.name;

        // Retirer l'état spécifique au terrain.
        TerrainCardState terrainState = terrain.GetComponent<TerrainCardState>();

        if (terrainState != null)
            Destroy(terrainState);

        // Envoyer le terrain dans la Trash adverse.
        trashManager.AddCard(terrain);

        Debug.Log($"{terrainName} a été détruit et envoyé dans la Trash adverse.");

        return true;
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
