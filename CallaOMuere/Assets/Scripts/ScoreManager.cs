using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    // Propiedad estática para el patrón Singleton
    public static ScoreManager Instance { get; private set; }

    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int pointsPerKill = 30;

    private int currentScore = 0;

    // Inicializa 'Instance' al cargar la escena
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        UpdateScoreDisplay();
    }

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreDisplay();
    }

    public void ZombieKilled()
    {
        AddScore(pointsPerKill);
    }

    public int GetCurrentScore()
    {
        return currentScore;
    }

    // Método para gastar puntos (usado por PowerUps)
    public bool TrySpendPoints(int amount)
    {
        if (currentScore >= amount)
        {
            currentScore -= amount;
            UpdateScoreDisplay();
            return true;
        }
        return false;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = currentScore.ToString();
        }
        else
        {
            Debug.LogError("El TextMeshProUGUI (scoreText) no está asignado en el Inspector del ScoreManager.");
        }
    }
}