using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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
        // Réinitialise la rotation locale de la carte dans la Trash.
        card.transform.localRotation = Quaternion.Euler(0f, 0f, cardRotation);

        RectTransform rect = card.GetComponent<RectTransform>();

        if (rect != null)
        {
            float targetRotation = card.transform.eulerAngles.z > 90f && card.transform.eulerAngles.z < 270f ? 180f : 0f;
            StartCoroutine(ResetTrashCardRotation(rect, targetRotation));
        }
        else
        {
            card.transform.rotation = Quaternion.Euler(0f, 0f, cardRotation);
        }

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

    public CardView RemoveCard(CardData data)
    {
        if (data == null || trashZone == null)
            return null;

        foreach (Transform child in trashZone)
        {
            CardView card = child.GetComponent<CardView>();

            if (card != null && card.Data == data)
            {
                TrashCardClickForwarder forwarder =
                    card.GetComponent<TrashCardClickForwarder>();

                if (forwarder != null)
                    Destroy(forwarder);

                card.transform.SetParent(null);

                return card;
            }
        }

        return null;
    }

    public bool RecoverCardToHand(CardData data, PlayerHand playerHand)
    {
        if (playerHand == null)
        {
            Debug.LogError("TrashManager : PlayerHand n'est pas assigné.");
            return false;
        }

        CardView card = RemoveCard(data);

        if (card == null)
            return false;

        Destroy(card.gameObject);
        playerHand.AddCard(data);

        Debug.Log($"Carte récupérée depuis la Trash vers la main : {data.cardName}");

        return true;
    }

    public bool RecoverCardToDeck(CardData data, PlayerDeck playerDeck)
    {
        if (playerDeck == null)
        {
            Debug.LogError("TrashManager : PlayerDeck n'est pas assigné.");
            return false;
        }

        CardView card = RemoveCard(data);

        if (card == null)
            return false;

        playerDeck.AddCardToTop(data);
        Destroy(card.gameObject);

        Debug.Log($"Carte récupérée depuis la Trash vers le dessus du deck : {data.cardName}");

        return true;
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

    private IEnumerator ResetTrashCardRotation(RectTransform cardTransform, float targetRotation)
    {
        yield return null;

        if (cardTransform != null)
        {
            cardTransform.rotation = Quaternion.Euler(0f, 0f, targetRotation);
        }
    }
}

