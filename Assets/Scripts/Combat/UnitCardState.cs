using UnityEngine;

public class UnitCardState : MonoBehaviour
{
    [SerializeField] private bool isExhausted;
    [SerializeField] private int summonedOnTurn;
    [SerializeField] private int currentHealth;
    [SerializeField] private TrashManager trashManager;
    [SerializeField] private GameManager gameManager;

    public int MaxHealth { get; private set; }

    private Quaternion baseRotation;

    public bool IsExhausted => isExhausted;
    public int SummonedOnTurn => summonedOnTurn;
    public int CurrentHealth => currentHealth;

    public bool CanAttackOnTurn(int currentTurn)
    {
        if (IsExhausted)
            {
            return false;
        }

        CardView cardView = GetComponent<CardView>();
        if(cardView == null || cardView.Data == null)
        {
            return false;
        }

        CardAbility[] abilities = cardView.Data.abilities;
        bool hasRush = false;

        if (abilities != null)
        {
            foreach (CardAbility ability in abilities)
            {
                if (ability == CardAbility.Rush)
                {
                    hasRush = true;
                    break;
                }
            }
        }

        //Rush permet d'attaquer le tour où la carte est invoquée.
        if (currentTurn == summonedOnTurn && hasRush)
        {
            return true;
        }

        return currentTurn > summonedOnTurn;
    }

    public bool HasAbility(CardAbility ability)
    {
        CardView cardView = GetComponent<CardView>();
        if(cardView == null || cardView.Data == null)
        {
            return false;
        }

        CardAbility[] abilities = cardView.Data.abilities;

        if(abilities == null)
        {
            return false;
        }

        
        foreach (CardAbility a in abilities)
        {
            if (a == ability)
            {
                return true;
            }
        }
        
        return false;
    }

    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
    }
    public bool CanIntercept()
    {
        return !IsExhausted && HasAbility(CardAbility.Blocker);
    }

    public void Initialize(int currentTurn)
    {
        baseRotation = transform.localRotation;

        summonedOnTurn = currentTurn;
        isExhausted = false;

        CardView cardView = GetComponent<CardView>();
        if(cardView != null && cardView.Data != null)
        {
            MaxHealth = cardView.Data.maxHealth;
            currentHealth = MaxHealth;
            GetComponent<CardView>()?.UpdateHealthText(currentHealth, MaxHealth);
        }
        else
        {
            Debug.LogError("Impossible d'initialiser les PV : CardView ou CardData est null");
        }

        UpdateVisual();
    }

    public void SetTrashManager(TrashManager manager)
    {
        trashManager = manager;
    }

    public void TakeDamage(int damage)
    {
        if(damage <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        Debug.Log($"{gameObject.name} a subi {damage} points de dégâts. PV restants : {currentHealth} / {MaxHealth}");
        GetComponent<CardView>()?.UpdateHealthText(currentHealth, MaxHealth);

        if (currentHealth <= 0)
        {

            CardView card = GetComponent<CardView>();

            if(trashManager != null && card != null)
            {
                gameManager?.ResolveCardEffectsOnDeath(GetComponent<CardView>().Data);

                trashManager.AddCard(card);
            }
            else
            {
                Debug.LogError("Impossible d'envoyer la carte à la défausse : TrashManager ou CardView est null");
            }
        }
    }

    public void Exhaust()
    {
        isExhausted = true;
        UpdateVisual();
    }

    public void Ready()
    {
        isExhausted = false;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        float exhaustionAngle = isExhausted ? 90f : 0f;


        transform.localRotation = baseRotation  * Quaternion.Euler(0f, 0f, exhaustionAngle);
    }
}
