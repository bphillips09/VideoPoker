using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using UnityEngine.UI;
using Array = System.Array;
using Enum = System.Enum;
using System.Text.RegularExpressions;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour {
    [SerializeField] private UICard[] gameCards;
    [SerializeField] private Sprite[] suitSprite;
    [SerializeField] private Sprite[] cardTypeSprite;
    [SerializeField] private Sprite[] deckSprites;
    [SerializeField] private Image[] betMultiplierImages;
    [SerializeField] private Color betSelectedColor;
    [SerializeField] private Color betInactiveColor;
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private GameObject gameOverWindow;
    [SerializeField] private GameObject resetWindow;
    [SerializeField] private Button betOneButton;
    [SerializeField] private Button betMaxButton;
    private bool resetBet = false;
    private bool firstLaunch = true;
    private List<Card> deck;
    private List<Card> currentHand;
    private int credits = 100;
    private int betMultiplier = 1;
    private Sprite currentDeckSprite;
    private List<Card> badCards = null;
    private int handTurn = 0;
    private WinCondition gameOverCondition = WinCondition.YouLost;

    void Start() {
        Initialize();
    }

    //initialize the game / reset variables for a new hand
    void Initialize() {
        resetBet = false;
        gameOverWindow.SetActive(false);
        betText.text = $"BET {betMultiplier}";
        creditsText.text = $"CREDITS {credits}";
        winText.text = "";

        if (firstLaunch || betMultiplier > credits) {
            ResetBetMultiplier();
        }

        currentDeckSprite = deckSprites[Random.Range(0, deckSprites.Length)];

        badCards = null;
        gameOverCondition = WinCondition.YouLost;
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

    void ResetBetMultiplier() {
        firstLaunch = false;

        foreach (Image img in betMultiplierImages) {
            img.color = betInactiveColor;
        }

        betMultiplierImages[0].color = betSelectedColor;
        UpdateBetMultiplier(1);
    }

    //increment the bet as long as there are enough credits
    public void IncrementBet() {
        if (handTurn >= 2 && !resetBet) {
            resetBet = true;
            ResetBetMultiplier();
        }

        if (betMultiplier+1 > credits || betMultiplier == 5) {
            return;
        }

        betMultiplierImages[betMultiplier-1].color = betInactiveColor;
        UpdateBetMultiplier(betMultiplier+1);
        betMultiplierImages[betMultiplier-1].color = betSelectedColor;
    }

    public void MaxBet() {
        int count = 1;

        while (count < credits && count < 5) {
            betMultiplierImages[count-1].color = betInactiveColor;
            count++;
            UpdateBetMultiplier(count);
            betMultiplierImages[count-1].color = betSelectedColor;
        }
        PlayHand();
    }

    public void DrawHand() {
        PlayHand();
    }

    void PlayHand() {
        if (handTurn == 0) {
            if (credits < betMultiplier) {
                return;
            }

            AddToCredits(-betMultiplier);

            betOneButton.interactable = false;
            betMaxButton.interactable = false;
        }

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

    void UpdateBetMultiplier(int set) {
        betMultiplier = set;
        betText.text = $"BET {betMultiplier}";
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

        //check for a pair of jacks or better (if there are 1x of any 2 numbers, and both > 11 (Jack))
        if (currentHand.GroupBy(x => x.numericValue).Count(y => y.Count() == 2) == 1) {
            pairExists = true;

            //check for jacks or better
            int applicableNumericValue = currentHand.GroupBy(x => x.numericValue)
                                                    .Where(y => y.Count() == 2)
                                                    .SelectMany(x => x)
                                                    .FirstOrDefault().numericValue;

            if (applicableNumericValue > 10) {
                badCards = currentHand.Where(x => x.numericValue != applicableNumericValue).ToList();

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
        Card aceCard = null;

        if (sortedHand.Max(x => x.numericValue) == 14 && sortedHand.Min(x => x.numericValue) != 10) {
            setAceLow = true;
            aceCard = sortedHand.First();
            aceCard.numericValue = 1;
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
            aceCard.numericValue = 14;
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

        //hold any cards that can win (unless all can win)
        if (handTurn == 1 && badCards != null) {
            foreach (UICard card in gameCards) {
                if (!badCards.Contains(card.referenceCard)) {
                    card.Hold(true);
                }
            }
        }

        if (handTurn == 2) {
            GameOver();

            //dim any cards that aren't part of the winning hand
            if (badCards != null && gameOverCondition != WinCondition.YouLost) {
                foreach (UICard card in gameCards) {
                    if (badCards.Contains(card.referenceCard)) {
                        card.DimCard();
                    }
                }
            }
        }
        
        if (handTurn != 2 && gameOverCondition != WinCondition.YouLost) {
            winText.text = EnumToSpacedString(gameOverCondition);
        }
    }

    void GameOver() {
        gameOverWindow.SetActive(true);

        betOneButton.interactable = true;
        betMaxButton.interactable = true;

        int points = (int)gameOverCondition * betMultiplier;
        if (points > 0) {
            AddToCredits(points);
            winText.text = $"{EnumToSpacedString(gameOverCondition)} - WIN {points}";
            int textIndex = Array.IndexOf(Enum.GetValues(gameOverCondition.GetType()), gameOverCondition);
            ShowWinPointText(textIndex);
        }

        if (credits == 0) {
            PromptForReset();
        }
    }

    void PromptForReset() {
        resetWindow.SetActive(true);
    }

    public void ResetGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    string EnumToSpacedString(WinCondition enumCondition) {
        return Regex.Replace(enumCondition.ToString(), "([A-Z])", " $1").Trim();
    }

    void AddToCredits(int points) {
        credits += points;
        creditsText.text = $"CREDITS {credits}";
    }

    void ShowWinPointText(int index) {
        //
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