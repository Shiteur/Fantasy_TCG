using UnityEngine;
using UnityEngine.EventSystems;

public class TerrainClickHandler : MonoBehaviour, IPointerClickHandler
{
    private GameManager gameManager;

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        TerrainCardState terrainState = GetComponent<TerrainCardState>();

        if (terrainState == null || gameManager == null)
            return;

        gameManager.SelectTerrain(terrainState);
    }
}
