using TMPro;
using UnityEngine;


public class WaveManager : MonoBehaviour
{
    [Header("Configuración de Oleadas")]
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private TextMeshProUGUI waveText;

    // --- CAMPOS PARA EL LÍMITE ---
    [Header("Límite de Zombis Activos")]
    [Tooltip("Máximo número de zombis que pueden estar vivos en la escena a la vez.")]
    [SerializeField] private int maxActiveZombies = 20;
    private int currentActiveZombies = 0;
    // ------------------------------------


    [Header("Configuración de Zombies")]
    [SerializeField] private int initialZombieCount = 5;
    [SerializeField][Range(1.0f, 2.0f)] private float zombieCountMultiplier = 1.05f; // 5% de aumento
    [SerializeField] private float baseZombieHealth = 60f; // La vida de la ronda 1
    [SerializeField] private float healthIncreasePerWave = 30f; // Puntos de vida a añadir por ronda

    [Header("Referencias")]
    [SerializeField] private ZombieSpawner zombieSpawner;

    private int currentWaveIndex = 0; // Se inicializa en 0 (Ronda 1)
    private int zombiesRemainingInWave; // Zombis totales que el spawner debe generar
    private float nextWaveTime;
    private bool isWaitingForNextWave = true;
    private bool hasFinishedSpawning = false; // 👈 NUEVA VARIABLE CLAVE


    // Variables para llevar la cuenta de la progresión
    private int currentZombieCount;

    public int currentWave => currentWaveIndex + 1;

    void Start()
    {
        if (zombieSpawner == null)
        {
            Debug.LogError("WaveManager necesita una referencia a ZombieSpawner.");
            return;
        }

        currentZombieCount = initialZombieCount;
        nextWaveTime = Time.time + 3f;
    }

    void Update()
    {
        if (isWaitingForNextWave)
        {
            if (Time.time >= nextWaveTime)
            {
                StartNextWave();
                if (waveText != null)
                {
                    waveText.text = $"{currentWave}";
                }
                isWaitingForNextWave = false;
            }
        }
    }

    void StartNextWave()
    {
        // 1. Resetear el flag y calcular la cuenta de la oleada
        hasFinishedSpawning = false; // RESETEAR AL INICIO

        if (currentWaveIndex > 0)
        {
            currentZombieCount = Mathf.CeilToInt(currentZombieCount * zombieCountMultiplier);
        }
        zombiesRemainingInWave = currentZombieCount; // Este es el total a spawnear

        // 2. Calcular la vida
        float currentHealth = baseZombieHealth + (healthIncreasePerWave * currentWaveIndex);
        float healthMultiplier = currentHealth / baseZombieHealth;

        // 3. Intervalo
        float spawnInterval = 1f;

        Debug.Log($"Iniciando Oleada {currentWave}: Spawneando {zombiesRemainingInWave} zombies");

        zombieSpawner.StartWaveSpawn(zombiesRemainingInWave, spawnInterval, healthMultiplier);
    }

    /// <summary>
    /// Llamado por ZombieController al morir.
    /// </summary>
    public void ZombieDied()
    {
        currentActiveZombies--;
        zombiesRemainingInWave--;

        // 2. CONDICIÓN DE FIN DE OLEADA: 
        // Se han terminado de generar todos (hasFinishedSpawning)
        // Y todos los activos han muerto.
        if (hasFinishedSpawning && currentActiveZombies <= 0) // 👈 CORRECCIÓN DE LA CONDICIÓN
        {
            EndWave();
        }
        else
        {
            // Ahora el log tiene sentido: cuánto queda por spawnear vs. cuántos están vivos.
            Debug.Log($"Zombis restantes en Oleada {currentWave}: {zombiesRemainingInWave} | Activos: {currentActiveZombies}");
        }
    }

    /// <summary>
    /// NUEVO MÉTODO: Llamado por ZombieSpawner cuando ha generado su cuota total.
    /// </summary>
    public void SpawnerFinished()
    {
        hasFinishedSpawning = true;
        Debug.Log("Spawner ha terminado su cuota de zombies.");

        // Verificar si la oleada terminó instantáneamente
        if (currentActiveZombies <= 0)
        {
            EndWave();
        }
    }

    void EndWave()
    {
        Debug.Log($"¡Oleada {currentWave} completada!");

        currentWaveIndex++;

        nextWaveTime = Time.time + timeBetweenWaves;
        isWaitingForNextWave = true;
    }

    public int GetCurrentWave()
    {
        return currentWave;
    }

    // --- MÉTODOS PARA GESTIÓN DE POBLACIÓN (Usado por ZombieSpawner) ---
    public void ZombieSpawned()
    {
        currentActiveZombies++;
    }

    public bool CanSpawn()
    {
        return currentActiveZombies < maxActiveZombies;
    }
}