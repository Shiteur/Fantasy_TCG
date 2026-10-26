using UnityEngine;
using System.Collections.Generic;

public class PlayerDeck: MonoBehaviour
{
    [SerializeField] private DeckData deckData;

    private List<CardData> drawStack = new List<CardData>();
    public int RemainingCards => drawStack.Count;

    public void InitializeDeck() { 
        drawStack.Clear();
        
        if(deckData == null)
        {
            Debug.LogError("Aucun DeckData assigné !");
            return;
        }

        drawStack.AddRange(deckData.cards);
        ShuffleDeck();

        Debug.Log($"Deck initialisé avec {drawStack.Count} cartes.");
    }

    public void ShuffleDeck() { 
        for (int i = 0; i < drawStack.Count; i++)
        {
            int randomIndex = Random.Range(i, drawStack.Count);
            
            CardData temp = drawStack[i];
            drawStack[i] = drawStack[randomIndex];
            drawStack[randomIndex] = temp;
        }
        Debug.Log("Deck mélangé.");
    }

    public CardData DrawCard() { 
        if(drawStack.Count == 0)
        {
            Debug.LogWarning("Il n'y a plus de carte dans le deck");
            return null;
        }

        int lastIndex = drawStack.Count - 1;
        CardData drawnCard = drawStack[lastIndex];
        drawStack.RemoveAt(lastIndex);
        return drawnCard;
    }
}
