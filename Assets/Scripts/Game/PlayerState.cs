using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PlayerState
{
    public string playerName;

    public CardData champion;

    public List<CardInstance> deck = new();
    public List<CardInstance> hand = new();
    public List<CardInstance> lifeStack = new();
    public List<CardInstance> manaDeck = new();
    public List<CardInstance> manaZone = new();
    public List<CardInstance> units = new();
    public List<CardInstance> discard = new();

    public CardInstance activeTerrain;
    public bool hasPlayedManaThisTurn;
}
