using UnityEngine;
using System.IO; // Necesario para leer/escribir archivos
using System.Collections.Generic; // Para las Listas
using System.Linq; // Para conversiones

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    // --- ¡ARRASTRA TUS ASSETS AQUÍ! ---
    [Header("Bases de Datos de Assets")]
    [Tooltip("Arrastra TODOS tus ScriptableObjects de PowerUpData aquí.")]
    [SerializeField] private List<PowerUpData> allGamePowerUps;
    [Tooltip("Arrastra TODOS tus ScriptableObjects de WeaponData aquí.")]
    [SerializeField] private List<WeaponData> allGameWeapons;
    // ------------------------------------

    [Header("Referencias (se buscan solas)")]
    private PlayerHealth playerHealth;
    private ScoreManager scoreManager;
    private WaveManager waveManager;
    private PowerUpManager powerUpManager;
    private PlayerShooting playerShooting;
    private MovementController movementController;

    private string saveFilePath;

    // ¡Flag para que GameManager sepa si debe cargar!
    public static bool ShouldLoadGame { get; private set; } = false;

    void Awake()
    {
        // Singleton persistente
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            saveFilePath = Path.Combine(Application.persistentDataPath, "gamedata.save");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool DoesSaveExist()
    {
        return File.Exists(saveFilePath);
    }

    public void SetLoadGameFlag()
    {
        ShouldLoadGame = true;
    }

    public void SaveGame()
    {
        if (!FindGameManagers())
        {
            Debug.LogError("No se pudieron encontrar los Managers. ¿Estás en la escena del juego?");
            return;
        }

        SaveData data = new SaveData();

        // 1. Guardar estado del jugador
        data.currentHealth = playerHealth.currentHealth;
        data.currentArmor = playerHealth.currentArmor;

        // 2. Guardar estado de la partida
        data.currentScore = scoreManager.GetCurrentScore();
        data.currentWave = waveManager.currentWave; // Asumiendo que WaveManager tiene esto
        // data.zombiesRemainingInWave = waveManager.zombiesRemainingInWave; // Necesitarías hacer pública esta variable

        // 3. Guardar inventario
        //data.ownedPowerUps = PowerUpStore.GetOwnedPowerUps().ToList();

        // ¡Complicado! Extraer datos de las instancias de armas
        //var weaponData = WeaponStore.GetOwnedWeaponData();
        //data.ownedWeapons = weaponData.Select(w => w.weaponType).ToList();
        //data.upgradedWeapons = weaponData.Where(w => w.isUpgraded).Select(w => w.weaponType).ToList();

        // (Aquí iría la lógica de guardar munición si la implementas)

        // 4. Escribir en el archivo
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("¡Partida guardada en " + saveFilePath);
    }

    public void LoadGame()
    {
        if (!DoesSaveExist()) return;
        if (!FindGameManagers())
        {
            Debug.LogError("No se pudieron encontrar los Managers al cargar.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // 1. Cargar estado del jugador
        //playerHealth.SetHealthAndArmor(data.currentHealth, data.currentArmor);

        // 2. Cargar estado de la partida
        //scoreManager.SetScore(data.currentScore);
        //waveManager.SetWave(data.currentWave); // Necesitará lógica para re-calcular zombies

        // 3. Cargar PowerUps (en 2 pasos)
        // 3a. Cargar la lista estática
        //PowerUpStore.LoadOwnedPowerUps(data.ownedPowerUps);
        // 3b. Re-aplicar los efectos en el jugador
        foreach (PowerUpType type in data.ownedPowerUps)
        {
            PowerUpData powerUpAsset = allGamePowerUps.Find(p => p.powerUpType == type);
            if (powerUpAsset != null)
            {
                powerUpManager.ApplyPowerUp(powerUpAsset);
            }
        }

        // 4. Cargar Armas (en 2 pasos)
        // 4a. Re-crear las instancias y cargarlas en la tienda estática
        List<WeaponData> loadedInstances = new List<WeaponData>();
        foreach (WeaponType type in data.ownedWeapons)
        {
            WeaponData baseAsset = allGameWeapons.Find(w => w.weaponType == type);
            if (baseAsset != null)
            {
                WeaponData instance = Instantiate(baseAsset); // ¡Nueva instancia!
                if (data.upgradedWeapons.Contains(type))
                {
                    // ¡Aplica la mejora! (Necesitamos replicar la lógica de WeaponUpgradeShop)
                    ApplyUpgrade(instance);
                }
                loadedInstances.Add(instance);
            }
        }
        // 4b. Cargar la lista estática
        //WeaponStore.LoadOwnedWeapons(loadedInstances);

        // 5. Equipar la primera arma cargada (o la pistola por defecto)
        if (loadedInstances.Count > 0)
        {
            playerShooting.EquipWeapon(loadedInstances[0]);
        }

        Debug.Log("¡Partida cargada!");
        ShouldLoadGame = false; // Resetea el flag
    }

    public void DeleteSave()
    {
        if (DoesSaveExist())
        {
            File.Delete(saveFilePath);
            Debug.Log("Archivo de guardado borrado.");
        }
    }

    // --- Métodos de Ayuda ---

    // Busca los managers en la escena del juego
    private bool FindGameManagers()
    {
        playerHealth = FindObjectOfType<PlayerHealth>();
        scoreManager = ScoreManager.Instance;
        waveManager = FindObjectOfType<WaveManager>();
        powerUpManager = FindObjectOfType<PowerUpManager>();
        playerShooting = FindObjectOfType<PlayerShooting>();
        movementController = FindObjectOfType<MovementController>();

        return playerHealth != null && scoreManager != null && waveManager != null && powerUpManager != null && playerShooting != null && movementController != null;
    }

    // Lógica duplicada de WeaponUpgradeShop para aplicar mejoras al cargar
    private void ApplyUpgrade(WeaponData weapon)
    {
        // Valores hardcodeados de tu script WeaponUpgradeShop
        int shotgunUpgradePellets = 8;
        float rifleUpgradeFireRate = 0.05f;
        float pistolBurstFireRate = 0.1f;

        switch (weapon.weaponType)
        {
            case WeaponType.Pistol:
                weapon.isUpgraded = true;
                weapon.fireRate = pistolBurstFireRate;
                break;
            case WeaponType.Rifle:
                weapon.isUpgraded = true;
                weapon.fireRate = rifleUpgradeFireRate;
                break;
            case WeaponType.Shotgun:
                weapon.isUpgraded = true;
                weapon.pelletCount = shotgunUpgradePellets;
                break;
        }
    }
}