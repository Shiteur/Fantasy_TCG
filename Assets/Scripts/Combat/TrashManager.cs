using System.Collections.Generic;
using UnityEngine;

public class TrashManager : MonoBehaviour
{
    [Header("Zone de défausse")]
    [SerializeField] private Transform trashZone;
    [SerializeField] private float cardRotation = 0f;

    public int CardCount => trashZone != null ? trashZone.childCount : 0;

    public void AddCard(CardView card)
    {
        if (card == null)
        {
            Debug.LogError("TrashManager : impossible d'ajouter une carte null.");
            return;
        }

        if (trashZone == null)
        {
            Debug.LogError("TrashManager : la zone Trash n'est pas assignée !");
            return;
        }

        card.transform.SetParent(trashZone, false);
        card.gameObject.AddComponent<TrashCardClickForwarder>();
        card.transform.localScale = Vector3.one;
        card.transform.localRotation = Quaternion.Euler(0f, 0f, cardRotation);

        UnitClickHandler playerClickHandler = card.GetComponent<UnitClickHandler>();

        if (playerClickHandler != null)
        {
            Destroy(playerClickHandler);
        }

        OpponentUnitClickHandler opponentClickHandler = card.GetComponent<OpponentUnitClickHandler>();

        if (opponentClickHandler != null)
        {
            Destroy(opponentClickHandler);
        }

        // Une carte dans la Trash n'est plus une unité en combat.
        UnitCardState unitState = card.GetComponent<UnitCardState>();
        if (unitState != null)
        {
            Destroy(unitState);
        }

        Debug.Log("Carte envoyée dans la Trash : " + card.Data.cardName);
    }

    public List<CardData> GetCardData()
    {
        List<CardData> result = new List<CardData>();

        if (trashZone == null)
        {
            Debug.LogError("TrashManager : Trash Zone non assignée !");
            return result;
        }

        foreach (Transform child in trashZone)
        {
            CardView card = child.GetComponent<CardView>();

            if (card != null && card.Data != null)
            {
                result.Add(card.Data);
            }
        }

        return result;
    }
}

