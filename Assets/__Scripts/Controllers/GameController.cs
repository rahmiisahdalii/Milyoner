using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using OpenTDB;
using Utilities;

public class GameController : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip correctAnswerClip;
    [SerializeField] private AudioClip wrongAnswerClip;

    [Header("Delays")]
    [SerializeField] private float nextQuestionDelay = 3f;
    [SerializeField] private float gameOverDelay = 5f;
    [SerializeField] private float revealAnswerDelay = 4f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI statusText;
    private string defaultStatus = "Lütfen bir cevap seçin.";

    [Header("Status Messages")]
    private string correctAnswerStatus = "Doğru cevap.";
    private string incorrectAnswerStatus = "Yanlış cevap.";

    private SceneController sceneController;
    private SoundController soundController;
    private QuestionDisplay questionDisplay;
    private Question currentQuestion;
    private Answer correctAnswer;
    private List<string> allAnswers;
    private int numQuestions;
    private int currentQuestionNumber = 0;
    private bool didWalkAway = false;
    private List<Question> usedQuestions = new List<Question>();

    private List<int> prizes = new List<int>
    {
        0, 500, 1000, 2000, 5000, 10000, 20000,
        50000, 75000, 150000, 250000, 500000, 1000000
    };

    private List<int> safetyNets = new List<int> { 1000, 50000 };

    public int CurrentWinnings => prizes[currentQuestionNumber];
    public Question CurrentQuestion => currentQuestion;
    public Answer CorrectAnswer => correctAnswer;
    public List<int> Prizes => prizes;
    public List<int> SafetyNets => safetyNets;
    public float RevealAnswerDelay => revealAnswerDelay;

    public string A => allAnswers[(int)Answer.A];
    public string B => allAnswers[(int)Answer.B];
    public string C => allAnswers[(int)Answer.C];
    public string D => allAnswers[(int)Answer.D];

    public string StatusText
    {
        get => statusText.text;
        set => statusText.text = value;
    }

    [Obsolete]
    void Awake()
    {
        SetupSingleton();
    }

    [Obsolete]
    void Start()
    {
        questionDisplay = FindObjectOfType<QuestionDisplay>();
        sceneController = FindObjectOfType<SceneController>();
        soundController = FindObjectOfType<SoundController>();

        if (SceneManager.GetActiveScene().name == SceneNames.GameScene)
        {
            numQuestions = prizes.Count - 1;
            NextQuestion();
        }
    }

    public IEnumerator LoadNextQuestion()
    {
        statusText.text = correctAnswerStatus;
        soundController.PlayOneShot(correctAnswerClip);

        yield return new WaitForSeconds(nextQuestionDelay);

       if (++currentQuestionNumber > numQuestions)
        {
            sceneController.GameOver();
        }
        else
        {
            NextQuestion();
        }
    }

    public IEnumerator EndGame()
    {
        statusText.text = incorrectAnswerStatus;
        soundController.PlayOneShot(wrongAnswerClip);

        yield return new WaitForSeconds(gameOverDelay);

        sceneController.GameOver();
    }

    public int GetFinalWinnings()
    {
        if (didWalkAway || CurrentWinnings == prizes.Max())
        {
            return CurrentWinnings;
        }

        foreach (var sn in safetyNets)
        {
            if (CurrentWinnings >= sn)
            {
                return sn;
            }
        }

        return 0;
    }

    [Obsolete]
    public void HideAnswer(string ans)
    {
        int index = allAnswers.IndexOf(ans);
        allAnswers[index] = "";
        AnswerButton.DisableAnswer((Answer)index);

        questionDisplay.DisplayQuestion();
    }

    public void TakeTheMoney()
    {
        didWalkAway = true;
        sceneController.GameOver();
    }

    public void ResetGame()
    {
        Destroy(gameObject);
    }

    [Obsolete]
    private async void NextQuestion()
{
    var request = new QuestionRequest
    {
        difficulty = GetDifficulty()
    };

    try
    {
        List<Question> res = await RequestHandler.GetQuestions();

        if (res == null || res.Count == 0)
        {
            Debug.LogWarning("Soru listesi boş.");
            statusText.text = "Hiç soru bulunamadı.";
            return;
        }

        var unusedQuestions = res
            .Where(q => !usedQuestions.Any(uq => uq.question == q.question))
            .ToList();

        if (unusedQuestions.Count == 0)
        {
            Debug.Log("Tüm sorular gösterildi.");
            statusText.text = "Tüm sorular gösterildi.";
            return;
        }

        int randomIndex = UnityEngine.Random.Range(0, unusedQuestions.Count);
        currentQuestion = unusedQuestions[randomIndex];
        usedQuestions.Add(currentQuestion);

        ShuffleAnswers();
        questionDisplay.DisplayQuestion();
        AnswerButton.ResetAll();

        statusText.text = defaultStatus;
    }
    catch (System.Exception err)
    {
        Debug.LogError(err);
        statusText.text = err.Message;
    }
}


    private string GetDifficulty()
    {
        if (CurrentWinnings >= safetyNets[1])
        {
            return Difficulty.Hard;
        }
        else if (CurrentWinnings >= safetyNets[0])
        {
            return Difficulty.Medium;
        }
        else
        {
            return Difficulty.Easy;
        }
    }

    private void ShuffleAnswers()
    {
        allAnswers = new List<string>(currentQuestion.incorrect_answers);
        allAnswers.Add(currentQuestion.correct_answer);
        allAnswers = allAnswers.OrderBy(a => Guid.NewGuid()).ToList();
        correctAnswer = (Answer)allAnswers.IndexOf(currentQuestion.correct_answer);
    }

    [Obsolete]
    private void SetupSingleton()
    {
        if (FindObjectsOfType(GetType()).Length > 1)
        {
            Destroy(gameObject);
        }
        else
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
