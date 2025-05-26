﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using OpenTDB;
using Grammars;
using Utilities;

public class LifelinesUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip fiftyFiftyClip;
    [SerializeField] private AudioClip askTheAudienceClip;
    [SerializeField] private AudioClip phoneAFriendClip;

    [Header("Buttons")]
    [SerializeField] private Button askTheAudienceButton;
    [SerializeField] private Button phoneAFriendButton;
    [SerializeField] private Button fiftyFiftyButton;

    [Header("Probabilities")]
    // The probabilities that the audience/friend knows the correct answer, depending on question difficulty
    [SerializeField] private float easyProbability = 1f;
    [SerializeField] private float mediumProbability = 0.5f;
    [SerializeField] private float hardProbability = 0.25f;

    [Tooltip("Random names to output when the user asks to phone a friend.")]
    [SerializeField] private string[] friends = { "Emin", "Yusuf", "Murat", "Kadir", "Hüseyin" };

    private GameController gc;
    private SoundController sc;

    void Start()
    {
        gc = FindObjectOfType<GameController>();
        sc = FindObjectOfType<SoundController>();
    }

    void OnEnable()
    {
        GameGrammar.OnLifeline += OnLifelineEvent;
    }

    void OnDisable()
    {
        GameGrammar.OnLifeline -= OnLifelineEvent;
    }

    public void FiftyFifty()
    {
        if (!fiftyFiftyButton.interactable) return;

        sc.PlayOneShot(fiftyFiftyClip);

        // Pick a random wrong answer to keep
        var incorrect = gc.CurrentQuestion.incorrect_answers;
        var remain = incorrect[Random.Range(0, incorrect.Count)];

        // Hide the other wrong answers
        foreach (var ans in incorrect)
        {
            if (ans != remain)
            {
                gc.HideAnswer(ans);
            }
        }

        fiftyFiftyButton.interactable = false;
    }

    public void PhoneAFriend()
    {
        if (!phoneAFriendButton.interactable) return;

        sc.PlayOneShot(phoneAFriendClip);

        string friend = friends[Random.Range(0, friends.Length)];

        gc.StatusText = $"{friend} cevabın {GetLifelineAnswer()} olduğunu düşünüyor.";

        phoneAFriendButton.interactable = false;
    }

    public void AskTheAudience()
    {
        if (!askTheAudienceButton.interactable) return;

        sc.PlayOneShot(askTheAudienceClip);

        gc.StatusText = $"Seyircilerimiz cevabın {GetLifelineAnswer()} olduğunu düşünüyor.";

        askTheAudienceButton.interactable = false;
    }

    private void OnLifelineEvent(Lifeline lifeline)
    {
        switch (lifeline)
        {
            case Lifeline.AskTheAudience:
                AskTheAudience();
                break;
            case Lifeline.PhoneAFriend:
                PhoneAFriend();
                break;
            case Lifeline.FiftyFifty:
                FiftyFifty();
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// Randomly decide which answer to give to the player
    /// </summary>
    private string GetLifelineAnswer()
    {
        if (Random.value <= GetProbability())
        {
            return gc.CorrectAnswer.ToString();
        }
        else
        {
            // Get a random (and not disabled) answer
            var buttons = FindObjectsOfType<AnswerButton>();

            do
            {
                Answer value = Enum.RandomValue<Answer>();
                AnswerButton button = buttons.First(b => b.AnswerValue == value);

                if (!button.IsDisabled)
                {
                    return value.ToString();
                }
            } while (true);
        }
    }

    /// <summary>
    /// Determine probability the audience/friend will know the correct answer
    /// </summary>
    private float GetProbability()
    {
        string difficulty = gc.CurrentQuestion.difficulty;

        if (difficulty == Difficulty.Easy)
        {
            return easyProbability;
        }
        else if (difficulty == Difficulty.Medium)
        {
            return mediumProbability;
        }

        return hardProbability;
    }
}
