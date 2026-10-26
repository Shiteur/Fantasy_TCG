using UnityEngine;
using System.Collections.Generic;

public class ManaSystem : MonoBehaviour
{
    [Header("Mana Configuration")]
    [SerializeField] private DeckData manaDeckData;
    [SerializeField] private CardView manaCardPrefab;
    [SerializeField] private Transform manaZone;

    private List<CardData> manaDrawStack = new List<CardData>();
    private List<ManaCardState> manaOnBoard = new List<ManaCardState>();

    public int RemainingManaCards => manaDrawStack.Count;
    public int TotalManaOnBoard => manaOnBoard.Count;

    public int AvailableMana
    {
        get {
            int count = 0;

            foreach(ManaCardState mana in manaOnBoard)
            {
                if(!mana.IsExhaused) {
                    count++;
                }
            }
            return count;
        }
    }

    public void Initialize()
    {
        manaDrawStack.Clear();
        manaOnBoard.Clear();

        if(manaDeckData == null || manaCardPrefab == null || manaZone == null)
        {
            Debug.LogError("Configuration du ManaSystem incomplet !");
            return;
        }

        foreach(CardData card in manaDeckData.cards)
        {
            if (card == null) continue;

            if(card.cardType != CardType.Mana)
            {
                Debug.LogError($"Le deck de mana contient une carte qui n'est pas de type Mana : {card.cardName}");
                continue;
            }
            manaDrawStack.Add(card);
        }

        ShuffleManaDeck();
        Debug.Log($"ManaSystem initialisé avec {manaDrawStack.Count} cartes de mana.");
    }

    private void ShuffleManaDeck()
    {
        for (int i = 0; i < manaDrawStack.Count; i++)
        {
            int randomIndex = Random.Range(i, manaDrawStack.Count);


            CardData temp = manaDrawStack[i];
            manaDrawStack[i] = manaDrawStack[randomIndex];
            manaDrawStack[randomIndex] = temp;
        }
    }

    public void AddManaCards(int amount)
    {
        for (int i = 0; i < amount; i++) { 
            if (manaDrawStack.Count == 0)
            {
                Debug.LogWarning("Il n'y a plus de carte mana.");
                return;
            }

            int lastInedex = manaDrawStack.Count - 1;
            CardData cardToAdd = manaDrawStack[lastInedex];
            manaDrawStack.RemoveAt(lastInedex);

            CardView view = Instantiate(manaCardPrefab, manaZone);
            view.SetCardData(cardToAdd);

            ManaCardState state = view.gameObject.AddComponent<ManaCardState>();

            manaOnBoard.Add(state);
        }

        Debug.Log($"Mana disponible {AvailableMana} / {TotalManaOnBoard}");
    }

    public bool ExhaustMana(int amount)
    {
        if (amount < 0 || AvailableMana < amount)
        {
            return false;
        }

        if( amount == 0) return true;

        int remaining = amount;

        foreach(ManaCardState mana in manaOnBoard)
        {
            if (mana.IsExhaused) continue;

            mana.Exhaust();
            remaining--;

            if (remaining == 0) break;
        }
        return remaining == 0; // pas censé etre faux, mais on retourne false si jamais il y a un bug
    }

    public void ReadyAllMana()
    {
        foreach(ManaCardState mana in manaOnBoard)
        {
            mana.Ready();
        }
    }
}
