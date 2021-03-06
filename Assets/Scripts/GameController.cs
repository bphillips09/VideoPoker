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
    [SerializeField] private Color textColor;
    [SerializeField] private TextMeshProUGUI betText;
    [SerializeField] private TextMeshProUGUI winText;
    [SerializeField] private TextMeshProUGUI pointsText;
    [SerializeField] private TextMeshProUGUI creditsText;
    [SerializeField] private GameObject gameOverWindow;
    [SerializeField] private GameObject resetWindow;
    [SerializeField] private Button betOneButton;
    [SerializeField] private Button betMaxButton;
    [SerializeField] private TextMeshProUGUI[] topTextElements;
    [SerializeField] private AudioSource audioController;
    [SerializeField] private AudioClip[] audioClips;
    private Array enumArray = null;
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
    private bool handActive = false;

    void Start() {
        enumArray = Enum.GetValues(gameOverCondition.GetType());
        Array.Reverse(enumArray);

        Initialize();
    }

    //initialize the game / reset variables for a new hand
    void Initialize() {
        //bet multiplier is 1-5 unless royal flush when it's 5, then set it to 16 for 4000pts
        if (betMultiplier > 5) {
            betMultiplier = 5;
        }

        resetBet = false;
        gameOverWindow.SetActive(false);
        betText.text = $"BET {betMultiplier}";
        creditsText.text = $"CREDITS {credits}";
        winText.text = "";
        pointsText.text = "";

        if (firstLaunch || betMultiplier > credits) {
            ResetBetMultiplier();
        }

        //remove colored lines using regex to remove <color> tags
        foreach (TextMeshProUGUI pointsTextElement in topTextElements) {
            pointsTextElement.text = Regex.Replace(pointsTextElement.text, "<[^>]*>", "");
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

    //reset the bet back to 1
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
        ButtonClick();

        if (betMultiplier > 5) {
            betMultiplier = 5;
        }

        if (betMultiplier == 4) {
            betOneButton.interactable = false;
        }
        
        if (handTurn >= 2 && !resetBet && betMultiplier == 5) {
            resetBet = true;
            ResetBetMultiplier();
            betMultiplier = 1;
            return;
        }

        if (betMultiplier+1 > credits || betMultiplier == 5) {
            return;
        }

        if (betMultiplier > 0) {
            betMultiplierImages[betMultiplier-1].color = betInactiveColor;
        }

        UpdateBetMultiplier(betMultiplier+1);
        betMultiplierImages[betMultiplier-1].color = betSelectedColor;
    }

    //set the max bet based on credits remaining then play a new hand
    public void MaxBet() {
        ButtonClick();

        int count = 1;

        while (count < credits && count < 5) {
            betMultiplierImages[count-1].color = betInactiveColor;
            count++;
            UpdateBetMultiplier(count);
            betMultiplierImages[count-1].color = betSelectedColor;
        }
        
        DrawHand();
    }

    public void DrawHand() {
        ButtonClick();

        //don't play a new hand while one is currently being animated
        if (handActive) {
            return;
        }

        if (handTurn < 2) {
            PlayHand();
        } else {
            Initialize();
        }
    }

    //play a new hand
    void PlayHand() {
        if (handTurn == 0) {
            if (credits < betMultiplier) {
                return;
            }

            AddToCredits(-betMultiplier);

            betOneButton.interactable = false;
            betMaxButton.interactable = false;
        }

        handActive = true;

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

        //check for a straight
        IEnumerable<Card> sortedHand = currentHand.OrderByDescending(x => x.numericValue);
        bool setAceLow = false;
        Card aceCard = null;

        //if Ace is high and 10 isn't low, set Ace to low (1), then sort the hand and check distinct cards for increasing value
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

        //set ace back to high value
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
            if (betMultiplier == 5) {
                betMultiplier = 16;
            }
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
        } else if (handTurn == 1 && badCards == null && gameOverCondition != WinCondition.YouLost) {
            foreach (UICard card in gameCards) {
                card.Hold(true);
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
        
        //show win condition text
        if (handTurn != 2 && gameOverCondition != WinCondition.YouLost) {
            winText.text = EnumToSpacedString(gameOverCondition);
            PlayAudioClip(2);
        }

        handActive = false;
    }

    void GameOver() {
        gameOverWindow.SetActive(true);

        betOneButton.interactable = true;
        betMaxButton.interactable = true;

        //give player points and show win amount
        int points = (int)gameOverCondition * betMultiplier;
        if (points > 0) {
            AddToCredits(points);
            winText.text = $"{EnumToSpacedString(gameOverCondition)}";
            pointsText.text = $"WIN {points}";
            int textIndex = Array.IndexOf(enumArray, gameOverCondition);
            ShowWinPointText(textIndex);
            PlayAudioClip(3);
        }

        //ask for reset when out of money
        if (credits == 0) {
            PromptForReset();
        }
    }

    void PromptForReset() {
        resetWindow.SetActive(true);
    }

    public void ResetGame() {
        ButtonClick();

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    //turn enum ("JacksOrBetter") into spaced string ("Jacks Or Better")
    string EnumToSpacedString(WinCondition enumCondition) {
        return Regex.Replace(enumCondition.ToString(), "([A-Z])", " $1").Trim();
    }

    void AddToCredits(int points) {
        credits += points;
        creditsText.text = $"CREDITS {credits}";
    }

    //highlight the winning condition in the points text at the top based on the enum index
    void ShowWinPointText(int index) {
        if (index == 0) {
            return;
        }

        List<string[]> stringLines = topTextElements.Select(x => x.text.Split('\n')).ToList();

        //highlight first column and bet column
        stringLines[0][index] = $"<color=white>{stringLines[0][index]}</color>";
        stringLines[betMultiplier][index] = $"<color=white>{stringLines[betMultiplier][index]}</color>";

        for (int i = 0; i < topTextElements.Length; i++) {
            topTextElements[i].text = string.Join("\n", stringLines[i]);
        }
    }

    public void ButtonClick() {
        PlayAudioClip(0);
    }

    public void PlayCardSound() {
        PlayAudioClip(1);
    }

    public void PlayAudioClip(int clipIndex) {
        audioController.PlayOneShot(audioClips[clipIndex]);
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