using UnityEngine;
using TMPro; // Necesario para TextMeshPro

public class ScoreManager : MonoBehaviour
{
    // �VERIFICA ESTO EN EL INSPECTOR! Debe tener arrastrado tu TextMeshPro.
    [SerializeField] private TextMeshProUGUI scoreText;

    // Establecemos 30 como el valor por defecto en el c�digo
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
        // La actualizaci�n ocurre aqu�, inmediatamente despu�s de sumar los puntos.
        UpdateScoreDisplay();
    }

    public void ZombieKilled()
    {
        // Esta funci�n ahora usa el valor configurado (30 por defecto)
        AddScore(pointsPerKill);
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            // Formato claro para la puntuaci�n
            scoreText.text = currentScore.ToString();
        }
        else
        {
            // Si ves esto en el log, es el problema.
            Debug.LogError("El TextMeshProUGUI (scoreText) no est� asignado en el Inspector del ScoreManager.");
        }
    }
}