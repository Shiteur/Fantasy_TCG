using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDeck", menuName = "TCG/Deck")]
public class DeckData : ScriptableObject
{
    public List<CardData> cards = new List<CardData>();
}
