using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {
    private List<Card> deck;
    private List<Card> currentHand;
    [SerializeField] private UICard[] gameCards;
    [SerializeField] private Sprite[] suitSprite;
    [SerializeField] private Sprite[] cardTypeSprite;
    [SerializeField] private Sprite[] deckSprites;
    private Sprite currentDeckSprite;
    private List<Card> badCards = null;
    private int handTurn = 0;
    private WinCondition gameOverCondition = WinCondition.YouLost;
    public TextMeshProUGUI debugText;

    void Start() {
        Initialize();
    }

    //initialize the game
    void Initialize() {
        currentDeckSprite = deckSprites[Random.Range(0, deckSprites.Length)];

        badCards = null;
        gameOverCondition = WinCondition.YouLost;
        debugText.text = "";
        handTurn = 0;
        deck = new List<Card>();
        currentHand = new List<Card>();

        Suit currentSuit = Suit.Hearts;

        //set up the deck (4 sets of 13 cards (2-A))
        for (int i = 0; i < 4; i++) {
            for (int j = 2; j < 15; j++) {
                currentSuit = (Suit)i;

                deck.Add(new Card() {
                    suit = currentSuit,
                    color = (currentSuit == Suit.Hearts || currentSuit == Suit.Diamonds) ? CardColor.Red : CardColor.Black,
                    numericValue = j
                });
            }
        }

        //"shuffle" the deck
        deck = deck.OrderBy(x => System.Guid.NewGuid()).ToList();

        PlayHand();
    }

    public void DrawHand() {
        PlayHand();
    }

    void PlayHand() {
        //select 5 "random" cards and show them to the player
        for (int i = 0; i < gameCards.Length; i++) {
            if (handTurn == 0 || (handTurn == 1 && !gameCards[i].isHeld)) {
                Card deckCard = deck[0];
                deck.RemoveAt(0);

                if (handTurn == 0) {
                    currentHand.Add(deckCard);
                } else {
                    currentHand[i] = deckCard;
                }

                //animate
                StartCoroutine(gameCards[i].AssignCard(deckCard, GetSuitSprite(deckCard), GetCardTypeSprite(deckCard), currentDeckSprite, i));
            }
        }

        if (handTurn < 2) {
            handTurn++;
        } else {
            Initialize();
        }

        //wait for 1 second for animation before hand analysis
        StartCoroutine(WaitForCardAnalysis());
    }

    //get the sprite for the suit
    Sprite GetSuitSprite(Card card) {
        return suitSprite[(int)card.suit];
    }

    //get the sprite for J, Q, or K
    Sprite GetCardTypeSprite(Card card) {
        return (card.numericValue > 10 && card.numericValue < 14) ? cardTypeSprite[card.numericValue-11] : null;
    }

    //wait to analyze the hand
    IEnumerator WaitForCardAnalysis() {
        yield return new WaitForSeconds(1f);
        AnalyzeHand();
    }

    //analyze the hand in increasing point order
    void AnalyzeHand() {
        gameOverCondition = WinCondition.YouLost;

        bool straightExists = false;
        bool flushExists = false;
        bool pairExists = false;
        bool threeOfAKindExists = false;

        //check for a pair of jacks (if there are 1x of any 2 numbers, and both are 11 (Jack))
        if (currentHand.GroupBy(x => x.numericValue).Count(y => y.Count() == 2) == 1) {
            pairExists = true;

            if (currentHand.GroupBy(x => x.numericValue)
                .Where(y => y.Count() == 2)
                .FirstOrDefault()
                .FirstOrDefault()
                .numericValue == 11) {
                    
                badCards = currentHand.Where(x => x.numericValue != 11).ToList();

                gameOverCondition = WinCondition.JacksOrBetter;
            } else {
                badCards = currentHand.GroupBy(x => x.numericValue).Where(y => y.Count() == 1).SelectMany(x => x).ToList();
            }
        }

        //check for two pairs (if there are 2x of any 2 numbers)
        if (currentHand.GroupBy(x => x.numericValue).Count(y => y.Count() >= 2) == 2) {
            badCards = currentHand.GroupBy(x => x.numericValue)
                                  .Where(y => y.Count() < 2)
                                  .SelectMany(x => x)
                                  .ToList();

            gameOverCondition = WinCondition.TwoPair;
        }

        //check for any three (if there are 3x of the same number)
        if (currentHand.GroupBy(x => x.numericValue).Any(y => y.Count() == 3)) {
            threeOfAKindExists = true;

            badCards = currentHand.GroupBy(x => x.numericValue)
                                  .Where(y => y.Count() != 3)
                                  .SelectMany(x => x)
                                  .ToList();

            gameOverCondition = WinCondition.ThreeOfAKind;
        }

        //check for a straight (if Ace is high and 10 isn't low, set Ace to low (1), then sort the hand and check distinct cards for increasing value)
        IEnumerable<Card> sortedHand = currentHand.OrderByDescending(x => x.numericValue);
        bool setAceLow = false;
        if (sortedHand.Max(x => x.numericValue) == 14 && sortedHand.Min(x => x.numericValue) != 10) {
            setAceLow = true;
            sortedHand.First().numericValue = 1;
            sortedHand = currentHand.OrderByDescending(x => x.numericValue);
        }

        List<Card> distinctHand = sortedHand.GroupBy(x => x.numericValue)
                                            .Select(y => y.FirstOrDefault())
                                            .OrderBy(x => x.numericValue)
                                            .ToList();

        //if there are 5 distinct cards, continue
        if (distinctHand.Count() >= 5) {
            int handIterator = 0;

            //count each consecutively-increasing card
            for (int i = 1; i < distinctHand.Count(); i++) {
                if (distinctHand[i].numericValue == (distinctHand[i-1].numericValue + 1)) {
                    handIterator++;
                }
            }

            if (handIterator >= 4) {
                badCards = null;
                straightExists = true;
                gameOverCondition = WinCondition.Straight;
            }
        }

        if (setAceLow) {
            sortedHand.First().numericValue = 14;
        }

        //check for flush (only one type of suit in the hand)
        if (currentHand.GroupBy(x => x.suit).Count() == 1) {
            badCards = null;
            flushExists = true;
            gameOverCondition = WinCondition.Flush;
        }

        //check for full house (1 pair of numbers and 1 set of three numbers)
        if (pairExists && threeOfAKindExists) {
            badCards = null;
            gameOverCondition = WinCondition.FullHouse;
        }

        //check for any four (if there are 4x of the same number)
        if (currentHand.GroupBy(x => x.numericValue).Any(y => y.Count() == 4)) {
            badCards = currentHand.GroupBy(x => x.numericValue)
                                  .Where(y => y.Count() < 4)
                                  .SelectMany(x => x)
                                  .ToList();
            
            gameOverCondition = WinCondition.FourOfAKind;
        }

        //check for straight flush
        if (straightExists && flushExists) {
            badCards = null;
            gameOverCondition = WinCondition.StraightFlush;
        }

        //check for royal flush
        if (straightExists && flushExists && currentHand.Max(x => x.numericValue == 14)) {
            badCards = null;
            gameOverCondition = WinCondition.RoyalFlush;
        }

        if (handTurn == 1 && badCards != null) {
            foreach (UICard card in gameCards) {
                if (!badCards.Contains(card.referenceCard)) {
                    card.Hold(true);
                }
            }
        }

        if (handTurn == 2 || gameOverCondition != WinCondition.YouLost) {
            int points = (int)gameOverCondition;
            debugText.text = $"{gameOverCondition.ToString()} - {points} points!";
        }

        if (handTurn == 2) {
            if (badCards != null && gameOverCondition != WinCondition.YouLost) {
                foreach (UICard card in gameCards) {
                    if (badCards.Contains(card.referenceCard)) {
                        card.DimCard();
                    }
                }
            }
        }
    }
}

public enum WinCondition {
    RoyalFlush = 250,
    StraightFlush = 50,
    FourOfAKind = 25,
    FullHouse = 9,
    Flush = 6,
    Straight = 4,
    ThreeOfAKind = 3,
    TwoPair = 2,
    JacksOrBetter = 1,
    YouLost = 0
}