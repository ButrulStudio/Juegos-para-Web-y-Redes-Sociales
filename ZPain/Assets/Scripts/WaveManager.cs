using TMPro;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Configuración de Oleadas")]
    [SerializeField] private float timeBetweenWaves = 10f;
    [SerializeField] private TextMeshProUGUI waveText;

    [Header("Límite de Zombis Activos")]
    [SerializeField] private int maxActiveZombies = 20;
    private int currentActiveZombies = 0;

    [Header("Configuración de Zombies")]
    [SerializeField] private int initialZombieCount = 5;
    [SerializeField][Range(1.0f, 2.0f)] private float zombieCountMultiplier = 1.05f;
    [SerializeField] private float healthIncreasePerWave = 30f;

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

    private int currentZombieCount;

    public int currentWave => currentWaveIndex + 1;
    public int ZombiesRemainingInWave => zombiesRemainingInWave;

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
        // Se eliminó la llamada a SaveLoadManager.Instance.SaveGame()

        hasFinishedSpawning = false;

        if (currentWaveIndex > 0)
        {
            currentZombieCount = Mathf.CeilToInt(currentZombieCount * zombieCountMultiplier);
        }
        zombiesRemainingInWave = currentZombieCount;

        float extraHealthToAdd = healthIncreasePerWave * currentWaveIndex;
        float spawnInterval = 1f;

        Debug.Log($"Iniciando Oleada {currentWave}: Spawneando {zombiesRemainingInWave} zombies con +{extraHealthToAdd} HP extra.");

        zombieSpawner.StartWaveSpawn(zombiesRemainingInWave, spawnInterval, extraHealthToAdd);
    }

    public void ZombieDied()
    {
        currentActiveZombies--;
        zombiesRemainingInWave--;

        if (hasFinishedSpawning && currentActiveZombies <= 0)
        {
            EndWave();
        }
    }

    public void SpawnerFinished()
    {
        hasFinishedSpawning = true;

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

    public void ZombieSpawned()
    {
        currentActiveZombies++;
    }

    public bool CanSpawn()
    {
        return currentActiveZombies < maxActiveZombies;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}