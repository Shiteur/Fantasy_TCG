using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardView> cardsInGHand = new List<CardView>();

    public int CardCOunt => cardsInGHand.Count;
    public CardView SelectedCard { get; private set; }

    public event System.Action<CardView> CardSelected;

    public void AddCard(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("On ne peut pas ajouter une carte vide à la main!");
            return;
        }

        CardView newCard = Instantiate(cardPrefab, transform);
        newCard.SetCardData(cardData);
        newCard.SetFaceDown(false);
        newCard.Clicked += OnCardClicked;
        cardsInGHand.Add(newCard);
    }

    private void OnCardClicked(CardView card)
    {
        if(SelectedCard != null)
        {
            SelectedCard.SetSelected(false);
        }

        SelectedCard = card;
        SelectedCard.SetSelected(true);

        Debug.Log($"Carte sélectionnée: {card.Data.cardName}");
        CardSelected?.Invoke(card);
    }

    public void RemoveCard(CardView card)
    {
        if (card == null) return;

        cardsInGHand.Remove(card);
        card.Clicked -= OnCardClicked;

        if (SelectedCard == card)
        {
            SelectedCard.SetSelected(false);
            SelectedCard = null;
        }
    }

    public void AddExistingCard(CardView card)
    {
        if (card == null)
            return;

        if (cardsInGHand.Contains(card))
            return;

        card.transform.SetParent(transform, false);
        card.SetFaceDown(false);
        card.SetSelected(false);
        card.Clicked += OnCardClicked;

        cardsInGHand.Add(card);

        Debug.Log($"Carte réintégrée dans la main : {card.Data.cardName}");
    }
}
