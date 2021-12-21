using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Card {
    public int numericValue;
    public Suit suit;
    public CardColor color;

    public string GetStringValue() {
        string nameValue = "";

        switch (numericValue) {
            case (11):
                nameValue = "J";
            break;

            case (12):
                nameValue = "Q";
            break;

            case (13):
                nameValue = "K";
            break;

            case (14):
                nameValue = "A";
            break;

            default:
                nameValue = numericValue.ToString();
            break;
        }

        return nameValue;
    }
}

public enum Suit {
    Spades,
    Hearts,
    Diamonds,
    Clubs
}

public enum CardColor {
    Red,
    Black
}