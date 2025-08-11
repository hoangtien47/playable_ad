using UnityEngine;

public class BasketTrigger : MonoBehaviour
{
    [Header("Score Detection")]
    public string ballTag = "ballTag";
    public BasketballGameController gameController;
    
    [Header("Trigger Objects")]
    public GameObject dunkTriggerTop;
    public GameObject dunkTriggerBottom;
    public GameObject basketRing;
    
    [Header("Effects")]
    public ParticleSystem scoreEffect;
    public ParticleSystem perfectScoreEffect;
    public AudioSource audioSource;
    public AudioClip scoreSound;
    public AudioClip perfectScoreSound;
    public float effectDuration = 2f;
    
    // Track scoring state for each ball
    private System.Collections.Generic.Dictionary<GameObject, BallScoreState> ballStates = 
        new System.Collections.Generic.Dictionary<GameObject, BallScoreState>();
    
    private class BallScoreState
    {
        public bool passedTopTrigger = false;
        public bool touchedRing = false;
        public bool hasScored = false;
    }
    
    void Start()
    {
        // Find game controller if not assigned
        if (gameController == null)
        {
            gameController = FindFirstObjectByType<BasketballGameController>();
        }
        
        // Setup audio
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // Add trigger components to the trigger objects
        SetupTrigger(dunkTriggerTop, "TopTrigger");
        SetupTrigger(dunkTriggerBottom, "BottomTrigger");
        SetupTrigger(basketRing, "RingCollider");
    }
    
    void SetupTrigger(GameObject triggerObject, string triggerType)
    {
        if (triggerObject != null)
        {
            BasketTriggerDetector detector = triggerObject.GetComponent<BasketTriggerDetector>();
            if (detector == null)
            {
                detector = triggerObject.AddComponent<BasketTriggerDetector>();
            }
            detector.parentBasket = this;
            detector.triggerType = triggerType;
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // This method is kept for compatibility but main logic is in HandleTrigger
        if (other.CompareTag(ballTag))
        {
            Debug.Log("Ball entered main basket trigger: " + other.name);
        }
    }
    
    public void HandleTrigger(GameObject ball, string triggerType)
    {
        if (!ball.CompareTag(ballTag)) return;
        
        // Get or create ball state
        if (!ballStates.ContainsKey(ball))
        {
            ballStates[ball] = new BallScoreState();
        }
        
        BallScoreState state = ballStates[ball];
        if (state.hasScored) return; // Already scored with this ball
        
        Debug.Log($"Ball {ball.name} triggered {triggerType}");
        
        switch (triggerType)
        {
            case "TopTrigger":
                // Ball passed through top trigger
                Rigidbody ballRb = ball.GetComponent<Rigidbody>();
                if (ballRb != null && ballRb.linearVelocity.y < -1f) // Moving downward
                {
                    state.passedTopTrigger = true;
                    Debug.Log($"Ball {ball.name} passed top trigger going down");
                }
                break;
                
            case "BottomTrigger":
                // Ball reached bottom trigger - check for score
                if (state.passedTopTrigger)
                {
                    int points = state.touchedRing ? 1 : 3; // 3 for perfect, 1 for regular
                    RegisterScore(ball, points);
                    state.hasScored = true;
                }
                break;
                
            case "RingCollider":
                // Ball touched the ring
                state.touchedRing = true;
                Debug.Log($"Ball {ball.name} touched the ring");
                break;
        }
    }
    
    void RegisterScore(GameObject ball, int points)
    {
        Debug.Log($"SCORE! Ball: {ball.name}, Points: {points}");
        
        // Notify game controller
        if (gameController != null)
        {
            // Call RegisterScore for both types - you can modify this method later to handle points
            gameController.RegisterScore();
            
            // Log the points for debugging
            if (points == 3)
            {
                Debug.Log("PERFECT SHOT! 3 points!");
            }
            else
            {
                Debug.Log("Regular shot! 1 point!");
            }
        }
        
        // Play effects based on score type
        PlayScoreEffects(points == 3);
        
        // Clean up ball state after a delay
        StartCoroutine(CleanupBallState(ball));
    }
    
    System.Collections.IEnumerator CleanupBallState(GameObject ball)
    {
        yield return new UnityEngine.WaitForSeconds(3f);
        if (ballStates.ContainsKey(ball))
        {
            ballStates.Remove(ball);
        }
    }
    
    void PlayScoreEffects(bool isPerfect)
    {
        // Play appropriate particle effect
        if (isPerfect && perfectScoreEffect != null)
        {
            perfectScoreEffect.Play();
        }
        else if (scoreEffect != null)
        {
            scoreEffect.Play();
        }
        
        // Play appropriate sound
        AudioClip soundToPlay = isPerfect && perfectScoreSound != null ? perfectScoreSound : scoreSound;
        if (audioSource != null && soundToPlay != null)
        {
            audioSource.PlayOneShot(soundToPlay);
        }
    }
}

// Helper component for detecting triggers on different objects
public class BasketTriggerDetector : MonoBehaviour
{
    public BasketTrigger parentBasket;
    public string triggerType;
    
    void OnTriggerEnter(Collider other)
    {
        if (parentBasket != null)
        {
            parentBasket.HandleTrigger(other.gameObject, triggerType);
        }
    }
}
