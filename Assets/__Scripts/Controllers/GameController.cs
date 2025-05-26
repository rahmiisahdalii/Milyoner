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
    
    // Singleton
    private static GameController instance;
    public static GameController Instance => instance;
    
    // Component References
    private SceneController sceneController;
    private SoundController soundController;
    private QuestionDisplay questionDisplay;
    
    // Game State
    private Question currentQuestion;
    private Answer correctAnswer;
    private List<string> allAnswers;
    private int numQuestions;
    private int currentQuestionNumber = 0;
    private bool didWalkAway = false;
    private List<Question> usedQuestions = new List<Question>();
    
    // Game Configuration
    private List<int> prizes = new List<int>
    {
        0, 500, 1000, 2000, 5000, 10000, 20000,
        50000, 75000, 150000, 250000, 500000, 1000000
    };
    private List<int> safetyNets = new List<int> { 1000, 50000 };
    
    // Public Properties
    public int CurrentWinnings => prizes[currentQuestionNumber];
    public Question CurrentQuestion => currentQuestion;
    public Answer CorrectAnswer => correctAnswer;
    public List<int> Prizes => prizes;
    public List<int> SafetyNets => safetyNets;
    public float RevealAnswerDelay => revealAnswerDelay;
    public string A => allAnswers != null && allAnswers.Count > (int)Answer.A ? allAnswers[(int)Answer.A] : "";
    public string B => allAnswers != null && allAnswers.Count > (int)Answer.B ? allAnswers[(int)Answer.B] : "";
    public string C => allAnswers != null && allAnswers.Count > (int)Answer.C ? allAnswers[(int)Answer.C] : "";
    public string D => allAnswers != null && allAnswers.Count > (int)Answer.D ? allAnswers[(int)Answer.D] : "";
    
    public string StatusText
    {
        get => statusText != null ? statusText.text : "";
        set 
        {
            if (statusText != null)
                statusText.text = value;
            else
                Debug.LogError("StatusText component is null!");
        }
    }
    
    void Awake()
    {
        SetupSingleton();
    }
    
    void Start()
    {
        StartCoroutine(InitializeGame());
    }
    
    private IEnumerator InitializeGame()
    {
        // Wait for all objects to be initialized
        yield return new WaitForEndOfFrame();
        
        // Find required components with null checks
        questionDisplay = FindObjectOfType<QuestionDisplay>();
        if (questionDisplay == null)
            Debug.LogError("QuestionDisplay component not found in scene!");
            
        sceneController = FindObjectOfType<SceneController>();
        if (sceneController == null)
            Debug.LogError("SceneController component not found in scene!");
            
        soundController = FindObjectOfType<SoundController>();
        if (soundController == null)
            Debug.LogError("SoundController component not found in scene!");
        
        // Initialize game if in game scene
        if (SceneManager.GetActiveScene().name == SceneNames.GameScene)
        {
            numQuestions = prizes.Count - 1;
            yield return StartCoroutine(LoadFirstQuestion());
        }
    }
    
    private IEnumerator LoadFirstQuestion()
    {
        yield return new WaitForSeconds(0.5f); // Small delay to ensure everything is ready
        NextQuestion();
    }
    
    public IEnumerator LoadNextQuestion()
    {
        StatusText = correctAnswerStatus;
        
        if (soundController != null && correctAnswerClip != null)
            soundController.PlayOneShot(correctAnswerClip);
            
        yield return new WaitForSeconds(nextQuestionDelay);
        
        if (++currentQuestionNumber > numQuestions)
        {
            if (sceneController != null)
                sceneController.GameOver();
            else
                Debug.LogError("SceneController is null, cannot proceed to game over!");
        }
        else
        {
            NextQuestion();
        }
    }
    
    public IEnumerator EndGame()
    {
        StatusText = incorrectAnswerStatus;
        
        if (soundController != null && wrongAnswerClip != null)
            soundController.PlayOneShot(wrongAnswerClip);
            
        yield return new WaitForSeconds(gameOverDelay);
        
        if (sceneController != null)
            sceneController.GameOver();
        else
            Debug.LogError("SceneController is null, cannot proceed to game over!");
    }
    
    public int GetFinalWinnings()
    {
        if (didWalkAway || CurrentWinnings == prizes.Max())
        {
            return CurrentWinnings;
        }
        
        // Return highest safety net that player has passed
        for (int i = safetyNets.Count - 1; i >= 0; i--)
        {
            if (CurrentWinnings >= safetyNets[i])
            {
                return safetyNets[i];
            }
        }
        
        return 0;
    }
    
    public void HideAnswer(string ans)
    {
        if (allAnswers == null)
        {
            Debug.LogError("AllAnswers list is null!");
            return;
        }
        
        int index = allAnswers.IndexOf(ans);
        if (index >= 0 && index < allAnswers.Count)
        {
            allAnswers[index] = "";
            AnswerButton.DisableAnswer((Answer)index);
            
            if (questionDisplay != null)
                questionDisplay.DisplayQuestion();
            else
                Debug.LogError("QuestionDisplay is null!");
        }
    }
    
    public void TakeTheMoney()
    {
        didWalkAway = true;
        
        if (sceneController != null)
            sceneController.GameOver();
        else
            Debug.LogError("SceneController is null!");
    }
    
    public void ResetGame()
    {
        if (instance == this)
            instance = null;
            
        Destroy(gameObject);
    }
    
    private async void NextQuestion()
    {
        try
        {
            var request = new QuestionRequest
            {
                difficulty = GetDifficulty()
            };
            
            Debug.Log($"Requesting question with difficulty: {request.difficulty}");
            
            List<Question> res = await RequestHandler.GetQuestions(request);
            
            if (res == null || res.Count == 0)
            {
                Debug.LogWarning("Question list is empty from API.");
                StatusText = "Hiç soru bulunamadı.";
                return;
            }
            
            var unusedQuestions = res
                .Where(q => !usedQuestions.Any(uq => uq.question == q.question))
                .ToList();
            
            if (unusedQuestions.Count == 0)
            {
                Debug.Log("All questions have been shown.");
                StatusText = "Tüm sorular gösterildi.";
                
                if (sceneController != null)
                    sceneController.GameOver();
                else
                    Debug.LogError("SceneController is null!");
                return;
            }
            
            int randomIndex = UnityEngine.Random.Range(0, unusedQuestions.Count);
            currentQuestion = unusedQuestions[randomIndex];
            usedQuestions.Add(currentQuestion);
            
            ShuffleAnswers();
            
            if (questionDisplay != null)
                questionDisplay.DisplayQuestion();
            else
                Debug.LogError("QuestionDisplay is null!");
            
            AnswerButton.ResetAll();
            StatusText = defaultStatus;
            
            Debug.Log($"Question loaded successfully: {currentQuestion.question}");
        }
        catch (System.Exception err)
        {
            Debug.LogError($"Error in NextQuestion: {err.Message}\nStackTrace: {err.StackTrace}");
            StatusText = "Soru yüklenirken hata oluştu.";
            
            // Fallback: try to end game gracefully
            if (sceneController != null)
                sceneController.GameOver();
        }
    }
    
    private string GetDifficulty()
    {
        if (safetyNets.Count >= 2 && CurrentWinnings >= safetyNets[1])
        {
            return Difficulty.Hard;
        }
        else if (safetyNets.Count >= 1 && CurrentWinnings >= safetyNets[0])
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
        if (currentQuestion == null)
        {
            Debug.LogError("Current question is null!");
            return;
        }
        
        if (currentQuestion.incorrect_answers == null || string.IsNullOrEmpty(currentQuestion.correct_answer))
        {
            Debug.LogError("Question answers are null or empty!");
            return;
        }
        
        allAnswers = new List<string>(currentQuestion.incorrect_answers);
        allAnswers.Add(currentQuestion.correct_answer);
        allAnswers = allAnswers.OrderBy(a => Guid.NewGuid()).ToList();
        correctAnswer = (Answer)allAnswers.IndexOf(currentQuestion.correct_answer);
        
        Debug.Log($"Answers shuffled. Correct answer is: {correctAnswer}");
    }
    
    private void SetupSingleton()
    {
        if (instance != null && instance != this)
        {
            Debug.Log("GameController instance already exists, destroying duplicate.");
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("GameController singleton initialized.");
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
