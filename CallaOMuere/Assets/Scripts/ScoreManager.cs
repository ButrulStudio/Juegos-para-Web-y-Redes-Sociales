using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class ScoreManager : MonoBehaviour
{
    // ¡VERIFICA ESTO EN EL INSPECTOR! Debe tener arrastrado tu TextMeshPro.
    [SerializeField] private TextMeshProUGUI scoreText;

    // Establecemos 30 como el valor por defecto en el código
    [SerializeField] private int pointsPerKill = 30;

    private int currentScore = 0;

    void Start()
    {
        // El script se asegura de que el texto inicial sea "PUNTOS: 0"
        UpdateScoreDisplay();
    }

    public void AddScore(int points)
    {
        currentScore += points;
        // La actualización ocurre aquí, inmediatamente después de sumar los puntos.
        UpdateScoreDisplay();
    }

    public void ZombieKilled()
    {
        // Esta función ahora usa el valor configurado (30 por defecto)
        AddScore(pointsPerKill);
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            // Formato claro para la puntuación
            scoreText.text = currentScore.ToString();
        }
        else
        {
            // Si ves esto en el log, es el problema.
            Debug.LogError("El TextMeshProUGUI (scoreText) no está asignado en el Inspector del ScoreManager.");
        }
    }
}