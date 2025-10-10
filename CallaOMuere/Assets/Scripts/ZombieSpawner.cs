using UnityEngine;

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
            SpawnZombie();
            nextSpawnTime = Time.time + currentSpawnInterval;
            zombiesRemainingInWave--;

            if (zombiesRemainingInWave <= 0)
                isSpawning = false;
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

    private void SpawnZombie()
    {
        if (spawnPoints.Length == 0 || zombieTypes.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        ZombieData chosenData = GetRandomZombieData();

        GameObject newZombie = Instantiate(chosenData.prefab, spawnPoint.position, spawnPoint.rotation);
        ZombieController zc = newZombie.GetComponent<ZombieController>();

        if (zc != null)
        {
            zc.ApplyZombieData(chosenData);
            zc.ApplyHealthMultiplier(zombieHpMultiplier);
        }
    }

    private ZombieData GetRandomZombieData()
    {
        int wave = waveManager != null ? waveManager.GetCurrentWave() : 1;

        float basicChance = 0.5f; // 50% en la oleada 1
        float fastChance = 0.3f;  // 30% rápido
        float tankChance = 0.2f;  // 20% tanque

        // Aumenta la dificultad con cada oleada
        // Cada ola reduce los básicos y aumenta el resto un poco
        basicChance = Mathf.Clamp01(0.5f - (wave - 1) * 0.05f); // -5% por ola
        fastChance = Mathf.Clamp01(0.3f + (wave - 1) * 0.03f);  // +3% por ola
        tankChance = Mathf.Clamp01(0.2f + (wave - 1) * 0.02f);  // +2% por ola

        float total = basicChance + fastChance + tankChance;
        basicChance /= total;
        fastChance /= total;
        tankChance /= total;

        float randomValue = Random.value;

        if (randomValue < basicChance)
        {
            return zombieTypes[0]; // básico
        }
        else if (randomValue < basicChance + fastChance && zombieTypes.Length > 1)
        {
            return zombieTypes[1]; // rápido
        }
        else if (zombieTypes.Length > 2)
        {
            return zombieTypes[2]; // tanque
        }
        else
        {
            return zombieTypes[0];
        }
    }
}
