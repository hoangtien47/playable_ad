using UnityEngine;

public class BasketballGameController : MonoBehaviour
{
    [Header("Game Settings")]
    public int totalBalls = 5;
    public int shotsNeeded = 3; // Shots needed to "win" the ad
    public float gameTimeLimit = 30f; // Time limit for the ad
    
    [Header("UI Elements - Optional")]
    public GameObject scoreTextObj; // For displaying score
    public GameObject timeTextObj; // For displaying time  
    public GameObject instructionTextObj; // For instructions
    public GameObject winPanel;
    public GameObject losePanel;
    public GameObject downloadButton;
    
    [Header("Ball Management")]
    public GameObject ballPrefab; // Optional: for spawning new balls
    public Transform ballSpawnPoint;
    
    private int currentScore = 0;
    private float timeRemaining;
    private bool gameEnded = false;
    private GameObject[] ballObjects;
    
    void Start()
    {
        timeRemaining = gameTimeLimit;
        
        // Find all balls in scene
        ballObjects = GameObject.FindGameObjectsWithTag("ballTag");
        totalBalls = ballObjects.Length;
        
        // Initialize UI
        UpdateUI();
        
        Debug.Log("Game Started: Tap and drag a ball to throw it into the basket!");
        
        // Hide end panels
        if (winPanel != null) winPanel.SetActive(false);
        if (losePanel != null) losePanel.SetActive(false);
    }
    
    void Update()
    {
        if (gameEnded) return;
        
        // Update timer
        timeRemaining -= Time.deltaTime;
        
        // Check win condition
        if (currentScore >= shotsNeeded)
        {
            EndGame(true);
        }
        
        // Check lose condition (time up)
        if (timeRemaining <= 0)
        {
            EndGame(false);
        }
        
        UpdateUI();
    }
    
    public void OnBallScored()
    {
        currentScore++;
        Debug.Log("Ball scored! Current score: " + currentScore);
        
        // Optional: Add particle effects, sound, etc.
        ShowScoreEffect();
    }
    
    void ShowScoreEffect()
    {
        // Add visual feedback when ball scores
        Debug.Log("Great shot! " + (shotsNeeded - currentScore) + " more to go!");
        
        // Hide instruction after 2 seconds
        Invoke("ResetInstructionText", 2f);
    }
    
    void ResetInstructionText()
    {
        if (!gameEnded)
        {
            Debug.Log("Keep shooting!");
        }
    }
    
    void UpdateUI()
    {
        // Debug output for score and time (since UI elements are optional)
        Debug.Log("Score: " + currentScore + "/" + shotsNeeded + " | Time: " + Mathf.Ceil(timeRemaining).ToString());
        
        // If you have UI text objects, you can get their Text components and update them
        // Example: scoreTextObj.GetComponent<Text>().text = "Score: " + currentScore + "/" + shotsNeeded;
    }
    
    void EndGame(bool won)
    {
        gameEnded = true;
        
        if (won)
        {
            Debug.Log("Player won!");
            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
            
            Debug.Log("Amazing! Download to play the full game!");
        }
        else
        {
            Debug.Log("Time's up!");
            if (losePanel != null)
            {
                losePanel.SetActive(true);
            }
            
            Debug.Log("So close! Download to try again!");
        }
        
        // Show download button after 1 second
        if (downloadButton != null)
        {
            Invoke("ShowDownloadButton", 1f);
        }
    }
    
    void ShowDownloadButton()
    {
        if (downloadButton != null)
        {
            downloadButton.gameObject.SetActive(true);
        }
    }
    
    // Call this from basket trigger when ball goes through
    public void RegisterScore()
    {
        OnBallScored();
    }
    
    
    // Optional: Restart game (for testing)
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
    }
}
