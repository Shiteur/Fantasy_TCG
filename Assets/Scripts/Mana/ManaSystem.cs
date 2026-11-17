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

    public bool ReturnManaCardToDeck(ManaCardState mana)
    {
        if (mana == null || !manaOnBoard.Contains(mana))
            return false;

        CardView cardView = mana.GetComponent<CardView>();

        if (cardView == null || cardView.Data == null)
        {
            Debug.LogError("Impossible de récupérer les données de la carte mana.");
            return false;
        }

        CardData cardData = cardView.Data;

        manaOnBoard.Remove(mana);
        manaDrawStack.Insert(0, cardData);

        Destroy(mana.gameObject);

        Debug.Log("Carte mana renvoyée sous le Mana Deck.");

        return true;
    }

    public bool ReadyManaCard(ManaCardState mana)
    {
        if (mana == null || !manaOnBoard.Contains(mana))
            return false;

        if (!mana.IsExhaused)
        {
            Debug.LogWarning("Cette carte mana est déjà redressée.");
            return false;
        }

        mana.Ready();

        Debug.Log("Carte mana redressée.");
        return true;
    }
    public int ReturnManaCardsToDeck(int amount)
    {
        if (amount <= 0)
            return 0;

        int returned = 0;

        while (returned < amount && manaOnBoard.Count > 0)
        {
            ManaCardState mana = manaOnBoard[0];

            if (ReturnManaCardToDeck(mana))
            {
                returned++;
            }
            else
            {
                break;
            }
        }

        Debug.Log($"{returned} carte(s) mana renvoyée(s) automatiquement sous le Mana Deck.");

        return returned;
    }
    public int ReadyManaCards(int amount)
    {
        if (amount <= 0)
            return 0;

        int readied = 0;

        foreach (ManaCardState mana in manaOnBoard)
        {
            if (mana == null || !mana.IsExhaused)
                continue;

            if (ReadyManaCard(mana))
            {
                readied++;

                if (readied >= amount)
                    break;
            }
        }

        Debug.Log($"{readied} carte(s) mana redressée(s) automatiquement.");

        return readied;
    }

    public void ReadyAllMana()
    {
        foreach(ManaCardState mana in manaOnBoard)
        {
            mana.Ready();
        }
    }
}
