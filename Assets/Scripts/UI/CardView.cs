using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    [Header("Card Data")]
    [SerializeField] private CardData cardData;

    [Header("UI Elements")]
    [SerializeField] private TMP_Text cardNameText;
    [SerializeField] private TMP_Text costText;
    [SerializeField] private TMP_Text attackText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text lifeText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private GameObject selectionBorder;
    [SerializeField] private GameObject cardBackOverlay;

    [Header("Panel")]
    [SerializeField] private GameObject costPanel;
    [SerializeField] private GameObject attackPanel;
    [SerializeField] private GameObject healthPanel;
    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject colorPanel;
    [SerializeField] private GameObject manaColorPanel;

    [Header("Card Colors")]
    [SerializeField] private Image color1Image;
    [SerializeField] private Image color2Image;
    [SerializeField] private Image manaColorImage;

    public CardData Data { get; private set; }
    public event Action<CardView> Clicked;

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke(this);
    }


    private void Start()
    {
        RefreshCard();
    }

    public void SetRestMode(bool isResting) { 
        transform.localRotation = Quaternion.Euler(0f, 0f, isResting ? 90f : 0f);
    }

    public void SetCardData(CardData data)
    {
        Data = data;
        cardData = data;
        RefreshCard();
    }

    public void SetSelected(bool selected)
    {
        if(selectionBorder != null)
        {
            selectionBorder.SetActive(selected);
        }
    }

    public void UpdateHealthText(int currentHealth, int maxHealth)
    {
        if(healthText != null)
        {
            healthText.text = currentHealth.ToString();
        }
    }

    public void SetFaceDown(bool faceDawn)
    {
        if(cardBackOverlay != null)
        {
            cardBackOverlay.SetActive(faceDawn);
        }
    }

    public void RefreshCard()
    {
        if(cardData == null)
        {
            Debug.LogWarning("Aucun CardData assignée à " + gameObject.name);
            return;
        }
        // info carte de base
        cardNameText.text = cardData.cardName;
        descriptionText.text = cardData.description;

        costText.text = cardData.manaCost.ToString();
        attackText.text = cardData.attack.ToString();
        healthText.text = cardData.maxHealth.ToString();
        lifeText.text = cardData.initialLifeCard.ToString();

        // couleur de la carte
        color1Image.color = GetColor(cardData.cardColor);
        color2Image.color = GetColor(cardData.secondaryColor);
        manaColorImage.color = GetColor(cardData.cardColor);

        color2Image.gameObject.SetActive(cardData.hasSecondaryColor);
        colorPanel.SetActive(cardData.cardType != CardType.Mana);
        manaColorPanel.SetActive(cardData.cardType == CardType.Mana);

        // Affichage des panneaux en fonction du type de carte
        bool isManaCard = cardData.cardType == CardType.Mana;
        bool isChampionCard = cardData.cardType == CardType.Champion;
        bool isUnitCard = cardData.cardType == CardType.Unit;

        costPanel.SetActive(!isManaCard && !isChampionCard);
        attackPanel.SetActive(isUnitCard || isChampionCard);
        healthPanel.SetActive(isUnitCard);
        lifePanel.SetActive(isChampionCard);
    }

    private Color GetColor(CardColor cardColor)
    {
        return cardColor switch
        {
            CardColor.Red => Color.red,
            CardColor.Blue => Color.blue,
            CardColor.Green => Color.green,
            CardColor.Yellow => Color.yellow,
            CardColor.Purple => new Color(0.5f, 0f, 0.5f), // Purple
            CardColor.Black => Color.black,
            _ => Color.gray,
        };
    }
}
