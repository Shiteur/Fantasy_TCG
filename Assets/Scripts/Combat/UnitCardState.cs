using UnityEngine;

public class UnitCardState : MonoBehaviour
{
    [SerializeField] private bool isExhausted;
    [SerializeField] private int summonedOnTurn;
    [SerializeField] private int currentHealth;

    public int MaxHealth { get; private set; }

    private Quaternion baseRotation;

    public bool IsExhausted => isExhausted;
    public int SummonedOnTurn => summonedOnTurn;
    public int CurrentHealth => currentHealth;

    public bool CanAttackOnTurn(int currentTurn)
    {
        return !isExhausted && currentTurn > summonedOnTurn;
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
            Destroy(gameObject);
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
