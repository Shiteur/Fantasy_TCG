using UnityEngine;

[CreateAssetMenu(fileName = "NewCardData", menuName = "TCG/Card")]
public class CardData : ScriptableObject
{
    public string cardId;
    public string cardName;
    public CardType cardType;

    public CardColor cardColor;
    public bool hasSecondaryColor;
    public CardColor secondaryColor;

    [Min(0)]public int manaCost;
    [Min(0)] public int attack;
    [Min(0)] public int maxHealth; // The life for unit cards
    [Min(0)] public int initialLifeCard; // The number of life cards the player starts with when this card is their champion.

    public CardAbility[] abilities;
    public CardEffectData[] effects;

    public string description;
}
