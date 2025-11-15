using TMPro;
using UnityEngine;


public class WaveManager : MonoBehaviour
{
    [Header("Configuración de Oleadas")]
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private TextMeshProUGUI waveText;


    [Header("Límite de Zombis Activos")]
    [Tooltip("Máximo número de zombis que pueden estar vivos en la escena a la vez.")]
    [SerializeField] private int maxActiveZombies = 20;
    private int currentActiveZombies = 0;

    [Header("Configuración de Zombies")]
    [SerializeField] private int initialZombieCount = 5;
    [SerializeField][Range(1.0f, 2.0f)] private float zombieCountMultiplier = 1.05f; 
    [SerializeField] private float baseZombieHealth = 60f; // La vida de la ronda 1
    [SerializeField] private float healthIncreasePerWave = 30f; // Puntos de vida a añadir por ronda

    [Header("Referencias")]
    [SerializeField] private ZombieSpawner zombieSpawner;

    [Header("Sonido")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip waveEndSound;

    private int currentWaveIndex = 0;
    private int zombiesRemainingInWave;
    private float nextWaveTime;
    private bool isWaitingForNextWave = true;
    private bool hasFinishedSpawning = false;


    // Variables para llevar la cuenta de la progresión
    private int currentZombieCount;

    //  PROPIEDADES PÚBLICAS 
    public int currentWave => currentWaveIndex + 1;
    public int ZombiesRemainingInWave => zombiesRemainingInWave;


    void Start()
    {
        if (zombieSpawner == null)
        {
            Debug.LogError("WaveManager necesita una referencia a ZombieSpawner.");
            return;
        }

        // Este es el Start() original
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
        // Al empezar la oleada, guarda el progreso.
        // Ignora la oleada 1 (index 0) para no guardar al inicio de la partida.
        if (currentWaveIndex > 0 && SaveLoadManager.Instance != null)
        {
            Debug.Log($"--- AUTOSAVE: Iniciando Oleada {currentWave} ---");
            SaveLoadManager.Instance.SaveGame();
        }

        // Resetear el flag y calcular la cuenta de la oleada
        hasFinishedSpawning = false; // RESETEAR AL INICIO

        if (currentWaveIndex > 0)
        {
            currentZombieCount = Mathf.CeilToInt(currentZombieCount * zombieCountMultiplier);
        }
        zombiesRemainingInWave = currentZombieCount; // Este es el total a spawnear

        //  Calcular la vida
        float currentHealth = baseZombieHealth + (healthIncreasePerWave * currentWaveIndex);
        float healthMultiplier = currentHealth / baseZombieHealth;

        // Intervalo
        float spawnInterval = 1f;

        Debug.Log($"Iniciando Oleada {currentWave}: Spawneando {zombiesRemainingInWave} zombies");

        zombieSpawner.StartWaveSpawn(zombiesRemainingInWave, spawnInterval, healthMultiplier);
    }

 
    // Llamado por ZombieController al morir.

    public void ZombieDied()
    {
        currentActiveZombies--;
        zombiesRemainingInWave--;

        // 2. CONDICIÓN DE FIN DE OLEADA: 
        if (hasFinishedSpawning && currentActiveZombies <= 0)
        {
            EndWave();
        }
        else
        {
            Debug.Log($"Zombis restantes en Oleada {currentWave}: {zombiesRemainingInWave} | Activos: {currentActiveZombies}");
        }
    }

    // Llamado por ZombieSpawner cuando ha generado su cuota total.

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

        PlaySound(waveEndSound);

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

    //Forza al WaveManager a un estado específico al cargar una partida.

    public void SetWave(int waveNumber)
    {
        // Resetea el estado para que la próxima oleada sea la cargada
        currentWaveIndex = waveNumber - 1; // Si waveNumber es 5, index es 4

        // Re-calcular la cuenta de zombies para esta oleada
        currentZombieCount = initialZombieCount;
        for (int i = 0; i < currentWaveIndex; i++)
        {
            currentZombieCount = Mathf.CeilToInt(currentZombieCount * zombieCountMultiplier);
        }

        // Forzar el estado de "entre oleadas"
        nextWaveTime = Time.time + 3f; // Un breve retraso antes de empezar
        isWaitingForNextWave = true;
        hasFinishedSpawning = true;
        currentActiveZombies = 0;
        zombiesRemainingInWave = 0;

        if (waveText != null)
        {
            waveText.text = $"{currentWave}";
        }
    }

    /// <summary>
    /// Reproduce un sonido "one-shot" usando el AudioSource.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

}