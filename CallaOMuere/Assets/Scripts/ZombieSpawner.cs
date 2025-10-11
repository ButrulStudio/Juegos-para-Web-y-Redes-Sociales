using UnityEngine;
using Random = UnityEngine.Random;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private ZombieData[] zombieTypes;

    private int zombiesRemainingInWave;
    private float currentSpawnInterval;
    private float zombieHpMultiplier;
    private float nextSpawnTime;
    private bool isSpawning = false;

    void Update()
    {
        if (!isSpawning) return;

        if (Time.time >= nextSpawnTime && zombiesRemainingInWave > 0)
        {
            if (TrySpawnZombie())
            {
                nextSpawnTime = Time.time + currentSpawnInterval;
                zombiesRemainingInWave--;

                if (zombiesRemainingInWave <= 0)
                {
                    isSpawning = false;
                    // 👈 NUEVA LLAMADA: Notificar al WaveManager que la fase de generación ha terminado
                    if (waveManager != null)
                    {
                        waveManager.SpawnerFinished();
                    }
                }
            }
        }
    }

    public void StartWaveSpawn(int count, float interval, float hpMultiplier)
    {
        zombiesRemainingInWave = count;
        currentSpawnInterval = interval;
        zombieHpMultiplier = hpMultiplier;
        isSpawning = true;
        nextSpawnTime = Time.time;
    }

    private bool TrySpawnZombie()
    {
        if (spawnPoints.Length == 0 || zombieTypes.Length == 0) return false;

        if (waveManager != null && !waveManager.CanSpawn())
        {
            return false; // Límite de población alcanzado. No spawneamos.
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        ZombieData chosenData = GetRandomZombieData();

        GameObject newZombie = Instantiate(chosenData.prefab, spawnPoint.position, spawnPoint.rotation);
        ZombieController zc = newZombie.GetComponent<ZombieController>();

        if (zc != null)
        {
            zc.ApplyZombieData(chosenData);
            zc.ApplyHealthMultiplier(zombieHpMultiplier);
        }

        if (waveManager != null)
        {
            waveManager.ZombieSpawned();
        }

        return true;
    }


    private ZombieData GetRandomZombieData()
    {
        int wave = waveManager != null ? waveManager.GetCurrentWave() : 1;
        float basicChance = 0.5f;
        float fastChance = 0.3f;
        float tankChance = 0.2f;

        basicChance = Mathf.Clamp01(0.5f - (wave - 1) * 0.05f);
        fastChance = Mathf.Clamp01(0.3f + (wave - 1) * 0.03f);
        tankChance = Mathf.Clamp01(0.2f + (wave - 1) * 0.02f);

        float total = basicChance + fastChance + tankChance;
        if (total > 0)
        {
            basicChance /= total;
            fastChance /= total;
            tankChance /= total;
        }

        float randomValue = Random.value;

        if (randomValue < basicChance)
        {
            return zombieTypes[0];
        }
        else if (randomValue < basicChance + fastChance && zombieTypes.Length > 1)
        {
            return zombieTypes[1];
        }
        else if (zombieTypes.Length > 2)
        {
            return zombieTypes[2];
        }
        else
        {
            return zombieTypes[0];
        }
    }
}