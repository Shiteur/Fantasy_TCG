using UnityEngine;
using System.Collections.Generic;

public class PlayerHand : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] private CardView cardPrefab;

    private readonly List<CardView> cardsInGHand = new List<CardView>();

    public int CardCOunt => cardsInGHand.Count;
    public CardView SelectedCard { get; private set; }

    public void AddCard(CardData cardData)
    {
        if (cardData == null)
        {
            Debug.LogError("On ne peut pas ajouter une carte vide à la main!");
            return;
        }

        CardView newCard = Instantiate(cardPrefab, transform);
        newCard.SetCardData(cardData);
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
}
