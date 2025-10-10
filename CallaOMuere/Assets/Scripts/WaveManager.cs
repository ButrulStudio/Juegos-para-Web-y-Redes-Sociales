using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Configuraci�n de Oleadas")]
    [SerializeField] private float timeBetweenWaves = 10f;

    [Header("Configuraci�n de Zombies")]
    [SerializeField] private int initialZombieCount = 5;
    [SerializeField][Range(1.0f, 2.0f)] private float zombieCountMultiplier = 1.05f; // 5% de aumento
    [SerializeField] private float baseZombieHealth = 60f; // La vida de la ronda 1
    [SerializeField] private float healthIncreasePerWave = 30f; // Puntos de vida a a�adir por ronda

    [Header("Referencias")]
    [SerializeField] private ZombieSpawner zombieSpawner;

    private int currentWaveIndex = 0;
    private int zombiesRemainingInWave;
    private float nextWaveTime;
    private bool isWaitingForNextWave = true;

    // Variables para llevar la cuenta de la progresi�n
    private int currentZombieCount;

    // Para mostrar el número de wave dentro del juego
    public int currentWave => currentWaveIndex + 1;

    void Start()
    {
        if (zombieSpawner == null)
        {
            Debug.LogError("WaveManager necesita una referencia a ZombieSpawner.");
            return;
        }

        // Preparamos el contador para la primera oleada
        currentZombieCount = initialZombieCount;
        nextWaveTime = Time.time + 3f;
    }

    void Update()
    {
        // El sistema ahora puede continuar indefinidamente
        if (isWaitingForNextWave)
        {
            if (Time.time >= nextWaveTime)
            {
                StartNextWave();
                isWaitingForNextWave = false;
            }
        }
    }

    void StartNextWave()
    {
        // --- C�LCULO PROCEDURAL DE LA OLEADA ---

        // 1. Calcular el n�mero de zombies para esta oleada
        if (currentWaveIndex > 0) // No aplicar el multiplicador en la primera ronda
        {
            // Aumenta un 5% y redondea hacia arriba al entero m�s cercano
            currentZombieCount = Mathf.CeilToInt(currentZombieCount * zombieCountMultiplier);
        }
        zombiesRemainingInWave = currentZombieCount;

        // 2. Calcular la vida de los zombies para esta oleada
        float currentHealth = baseZombieHealth + (healthIncreasePerWave * currentWaveIndex);
        // Convertimos la vida a un multiplicador basado en la vida inicial (60)
        float healthMultiplier = currentHealth / baseZombieHealth;

        // 3. El intervalo de spawn puede ser constante o tambi�n podr�as hacerlo procedural
        float spawnInterval = 1f;

        Debug.Log($"Iniciando Oleada {currentWaveIndex + 1}: Spawneando {zombiesRemainingInWave} zombies con {currentHealth} HP cada uno.");

        // Instruir al spawner para que comience a generar
        zombieSpawner.StartWaveSpawn(zombiesRemainingInWave,spawnInterval,healthMultiplier);
    }

    public void ZombieDied()
    {
        zombiesRemainingInWave--;
        Debug.Log($"Zombies restantes en Oleada {currentWaveIndex + 1}: {zombiesRemainingInWave}");

        if (zombiesRemainingInWave <= 0)
        {
            EndWave();
        }
    }

    void EndWave()
    {
        Debug.Log($"�Oleada {currentWaveIndex + 1} completada!");
        currentWaveIndex++;

        nextWaveTime = Time.time + timeBetweenWaves;
        isWaitingForNextWave = true;
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

}