using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UICard : MonoBehaviour {
    [HideInInspector] public int numericValue;
    [HideInInspector] public Suit suit;
    [HideInInspector] public CardColor color;
    [HideInInspector] public bool isHeld = false;
    [HideInInspector] public Card referenceCard;
    [SerializeField] private Image cardType;
    [SerializeField] private Image suitLarge, suitSmall;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private GameObject heldText;
    [SerializeField] private Image deckImage;
    [SerializeField] private GameObject coverImage;
    [SerializeField] private GameController gameController;

    //assign UI card based on deck card
    public IEnumerator AssignCard(Card card, Sprite suitSprite, Sprite cardTypeSprite, Sprite deckSprite, float timeMultiplier) {
        DimCard(false);
        Hold(false);
        
        deckImage.sprite = deckSprite;
        deckImage.enabled = true;

        referenceCard = card;
        numericValue = card.numericValue;
        suit = card.suit;
        color = card.color;

        valueText.text = $"<color={card.color.ToString().ToLower()}>{card.GetStringValue()}</color>";
        
        suitLarge.sprite = suitSmall.sprite = suitSprite;
        cardType.sprite = cardTypeSprite;
        cardType.enabled = cardTypeSprite != null;

        //wait for 1/4s then show card after time based on order in hand
        yield return new WaitForSeconds(0.25f + (timeMultiplier*0.15f));
        
        gameController.PlayCardSound();
        deckImage.enabled = false;
    }

    public void Hold() {
        gameController.ButtonClick();
        isHeld = !isHeld;
        heldText.SetActive(isHeld);
    }

    public void Hold(bool set) {
        isHeld = set;
        heldText.SetActive(set);
    }

    public void DimCard(bool dim = true) {
        coverImage.SetActive(dim);
    }
}
