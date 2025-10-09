using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    [Header("Configuración general")]
    [SerializeField] private GameObject zombiePrefab;
    // [SerializeField] private int maxZombies = 25; // ELIMINAR O COMENTAR
    // [SerializeField] private float spawnInterval = 3f; // ELIMINAR O COMENTAR

    [Header("Referencias a Scripts")]
    [SerializeField] private WaveManager waveManager; // NUEVA REFERENCIA

    [Header("Puntos de spawn (empties en la escena)")]
    [SerializeField] private Transform[] spawnPoints;

    private int zombiesRemainingInWave;
    private float currentSpawnInterval;
    private float zombieHpMultiplier;

    private float nextSpawnTime;
    private bool isSpawning = false;

    void Update()
    {
        if (!isSpawning) return; // Se detiene si no hay una oleada activa

        if (Time.time >= nextSpawnTime && zombiesRemainingInWave > 0)
        {
            SpawnZombie();
            nextSpawnTime = Time.time + currentSpawnInterval;
            zombiesRemainingInWave--;

            if (zombiesRemainingInWave <= 0)
            {
                // Cuando todos los zombies han salido, detener el spawner.
                isSpawning = false;
            }
        }
    }

    // Nuevo método llamado por WaveManager para iniciar el spawn de la oleada
    public void StartWaveSpawn(int count, float interval, float hpMultiplier)
    {
        zombiesRemainingInWave = count;
        currentSpawnInterval = interval;
        zombieHpMultiplier = hpMultiplier;
        isSpawning = true;
        nextSpawnTime = Time.time; // Empieza a spawnear inmediatamente
    }

    private void SpawnZombie()
    {
        if (zombiePrefab == null || spawnPoints.Length == 0) return;

        // Elige un punto de spawn aleatorio
        Transform randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Instancia el zombie
        GameObject newZombie = Instantiate(zombiePrefab, randomSpawn.position, randomSpawn.rotation);

        // APLICA EL MULTIPLICADOR DE VIDA
        ZombieController zc = newZombie.GetComponent<ZombieController>();
        if (zc != null)
        {
            // Llama a una nueva función en ZombieController para aplicar el multiplicador
            zc.ApplyHealthMultiplier(zombieHpMultiplier);
        }
    }
}