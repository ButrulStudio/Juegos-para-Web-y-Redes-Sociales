using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public class SaveLoadManager : MonoBehaviour
{
    public static SaveLoadManager Instance { get; private set; }

    [Header("Bases de Datos de Assets")]
    [SerializeField] private List<PowerUpData> allGamePowerUps;
    [SerializeField] private List<WeaponData> allGameWeapons;

    [Header("Referencias (se buscan solas)")]
    private PlayerHealth playerHealth;
    private ScoreManager scoreManager;
    private WaveManager waveManager;
    private PowerUpManager powerUpManager;
    private PlayerShooting playerShooting;
    private MovementController movementController;

    private string saveFilePath;
    public static bool ShouldLoadGame { get; private set; } = false;

    void Awake()
    {
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

    public void SetLoadGameFlag(bool shouldLoad)
    {
        ShouldLoadGame = shouldLoad;
    }

    public void SaveGame()
    {
        if (!FindGameManagers())
        {
            Debug.LogError("SaveLoadManager: No se pudieron encontrar los Managers.");
            return;
        }

        SaveData data = new SaveData();

        // Guardar vida, puntuación, oleada, etc.
        data.currentHealth = playerHealth.currentHealth;
        data.currentArmor = playerHealth.currentArmor;
        data.currentScore = scoreManager.GetCurrentScore();
        data.currentWave = waveManager.currentWave;
        data.zombiesRemainingInWave = waveManager.ZombiesRemainingInWave;

        // Guardar inventario de armas y powerups
        data.ownedPowerUps = PowerUpStore.GetOwnedPowerUps().ToList();
        var weaponData = WeaponStore.GetOwnedWeaponData();
        data.ownedWeapons = weaponData.Select(w => w.weaponType).ToList();
        data.upgradedWeapons = weaponData.Where(w => w.isUpgraded).Select(w => w.weaponType).ToList();
        if (playerShooting.currentWeapon != null) data.equippedWeaponType = playerShooting.currentWeapon.weaponType;

        data.ammoData = playerShooting.GetAmmoData();

        // Escribir en el archivo
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
        Debug.LogWarning("¡SAVE COMPLETO! Munición guardada. Archivo en: " + saveFilePath);
    }

    public void LoadGame()
    {
        if (!DoesSaveExist()) return;
        if (!FindGameManagers())
        {
            Debug.LogError("LoadGame: No se pudieron encontrar los Managers al cargar.");
            return;
        }

        string json = File.ReadAllText(saveFilePath);
        SaveData data = JsonUtility.FromJson<SaveData>(json);

        // ... (Cargar vida, puntuación, oleada, powerups) ...
        playerHealth.SetHealthAndArmor(data.currentHealth, data.currentArmor);
        scoreManager.SetScore(data.currentScore);
        waveManager.SetWave(data.currentWave);
        PowerUpStore.LoadOwnedPowerUps(data.ownedPowerUps);
        foreach (PowerUpType type in data.ownedPowerUps)
        {
            PowerUpData powerUpAsset = allGamePowerUps.Find(p => p.powerUpType == type);
            if (powerUpAsset != null) powerUpManager.ApplyPowerUp(powerUpAsset);
        }

        // ... (Cargar Armas) ...
        List<WeaponData> loadedInstances = new List<WeaponData>();
        foreach (WeaponType type in data.ownedWeapons)
        {
            WeaponData baseAsset = allGameWeapons.Find(w => w.weaponType == type);
            if (baseAsset != null)
            {
                WeaponData instance = Instantiate(baseAsset);
                if (data.upgradedWeapons.Contains(type)) ApplyUpgrade(instance);
                loadedInstances.Add(instance);
            }
        }
        WeaponStore.LoadOwnedWeapons(loadedInstances);

        playerShooting.LoadAmmoData(data.ammoData);

        WeaponData weaponToEquip = loadedInstances.Find(w => w.weaponType == data.equippedWeaponType);
        if (weaponToEquip != null)
        {
            playerShooting.EquipWeapon(weaponToEquip);
        }
        else if (loadedInstances.Count > 0)
        {
            playerShooting.EquipWeapon(loadedInstances[0]);
        }

        Debug.LogWarning("¡Partida cargada! Munición restaurada.");
        ShouldLoadGame = false;
    }

    public void DeleteSave()
    {
        if (DoesSaveExist())
        {
            File.Delete(saveFilePath);
            Debug.LogWarning("Archivo de guardado borrado (El jugador murió).");
        }
    }

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

    private void ApplyUpgrade(WeaponData weapon)
    {
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