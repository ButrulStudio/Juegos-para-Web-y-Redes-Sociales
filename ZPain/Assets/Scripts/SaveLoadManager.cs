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

    [Tooltip("El ScriptableObject del arma con la que empezará el jugador en una partida nueva.")]
    [SerializeField] private WeaponData startingWeaponAsset;

    [Header("Referencias")]
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

    // Esta función le permite al GameManager pedir el arma inicial
    public WeaponData GetStartingWeapon()
    {
        return startingWeaponAsset;
    }

    public void SaveGame()
    {
        if (!FindGameManagers())
        {
            Debug.LogError("SaveLoadManager: No se pudieron encontrar los Managers.");
            return;
        }

        SaveData data = new SaveData();

        // 1. Guardar vida, puntuación, oleada, etc.
        data.currentHealth = playerHealth.currentHealth;
        data.currentArmor = playerHealth.currentArmor;
        data.currentScore = scoreManager.GetCurrentScore();
        data.currentWave = waveManager.currentWave;
        data.zombiesRemainingInWave = waveManager.ZombiesRemainingInWave;

        // 2. Guardar inventario de PowerUps
        data.ownedPowerUps = PowerUpStore.GetOwnedPowerUps().ToList();

        // 3. Guardar Inventario de Armas y Slots
        var weaponDataList = WeaponStore.GetOwnedWeaponData();

        // Guardar qué armas poseemos en total (para la tienda y mejoras)
        data.ownedWeapons = weaponDataList.Select(w => w.weaponType).ToList();
        data.upgradedWeapons = weaponDataList.Where(w => w.isUpgraded).Select(w => w.weaponType).ToList();

        // Guardar qué arma exacta está en cada slot
        // IMPORTANTE: Requiere los nuevos métodos en PlayerShooting
        data.weaponTypeInSlot0 = playerShooting.GetWeaponTypeInSlot(0);
        data.weaponTypeInSlot1 = playerShooting.GetWeaponTypeInSlot(1);
        data.activeSlotIndex = playerShooting.GetCurrentSlotIndex();

        // Guardar munición
        data.ammoData = playerShooting.GetAmmoData();

        // Referencia extra (opcional)
        if (playerShooting.currentWeapon != null)
            data.equippedWeaponType = playerShooting.currentWeapon.weaponType;

        // Escribir en el archivo
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
        Debug.LogWarning("¡SAVE COMPLETO! Slots guardados. Archivo en: " + saveFilePath);
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

        // --- Restaurar Stats Básicos ---
        playerHealth.SetHealthAndArmor(data.currentHealth, data.currentArmor);
        scoreManager.SetScore(data.currentScore);
        waveManager.SetWave(data.currentWave);

        // --- Restaurar PowerUps ---
        PowerUpStore.LoadOwnedPowerUps(data.ownedPowerUps);
        foreach (PowerUpType type in data.ownedPowerUps)
        {
            PowerUpData powerUpAsset = allGamePowerUps.Find(p => p.powerUpType == type);
            if (powerUpAsset != null) powerUpManager.ApplyPowerUp(powerUpAsset);
        }

        // --- RESTAURAR ARMAS E INVENTARIO ---

        // 1. Recrear las instancias de armas en memoria
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

        // Registrar en la tienda para que sepa que ya las compramos
        WeaponStore.LoadOwnedWeapons(loadedInstances);

        // 2. Restaurar caché de munición
        playerShooting.LoadAmmoData(data.ammoData);

        // 3. Limpiar slots actuales antes de rellenar
        playerShooting.ClearInventory();

        // 4. Asignar armas a los slots correspondientes
        if (data.weaponTypeInSlot0 != -1)
        {
            WeaponData weaponForSlot0 = loadedInstances.Find(w => (int)w.weaponType == data.weaponTypeInSlot0);
            if (weaponForSlot0 != null)
                playerShooting.ForceWeaponToSlot(0, weaponForSlot0);
        }

        if (data.weaponTypeInSlot1 != -1)
        {
            WeaponData weaponForSlot1 = loadedInstances.Find(w => (int)w.weaponType == data.weaponTypeInSlot1);
            if (weaponForSlot1 != null)
                playerShooting.ForceWeaponToSlot(1, weaponForSlot1);
        }

        // 5. Seleccionar el slot activo correcto
        int slotToActivate = Mathf.Clamp(data.activeSlotIndex, 0, 1);
        playerShooting.SelectSlot(slotToActivate);

        Debug.LogWarning("¡Partida cargada! Inventario y Slots restaurados.");
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

        return playerHealth != null && scoreManager != null && waveManager != null &&
               powerUpManager != null && playerShooting != null && movementController != null;
    }

    private void ApplyUpgrade(WeaponData weapon)
    {
        // Valores hardcodeados de mejoras (idealmente irían en WeaponData, pero sirve por ahora)
        int shotgunUpgradePellets = 8;
        float rifleUpgradeFireRate = 0.05f;
        float pistolBurstFireRate = 0.1f;
        int sniperUpgradePenetration = 3;

        // Mejoras de munición también deberían aplicarse aquí si no se guardan en el ScriptableObject
        int pistolUpgradedMag = 20; int pistolUpgradedMax = 120;
        int rifleUpgradedMag = 45; int rifleUpgradedMax = 270;
        int shotgunUpgradedMag = 12; int shotgunUpgradedMax = 64;
        int sniperUpgradedMag = 10; int sniperUpgradedMax = 50;

        switch (weapon.weaponType)
        {
            case WeaponType.Pistol:
                weapon.isUpgraded = true;
                weapon.fireRate = pistolBurstFireRate;
                weapon.magCapacity = pistolUpgradedMag;
                weapon.maxAmmo = pistolUpgradedMax;
                break;
            case WeaponType.Rifle:
                weapon.isUpgraded = true;
                weapon.fireRate = rifleUpgradeFireRate;
                weapon.magCapacity = rifleUpgradedMag;
                weapon.maxAmmo = rifleUpgradedMax;
                break;
            case WeaponType.Shotgun:
                weapon.isUpgraded = true;
                weapon.pelletCount = shotgunUpgradePellets;
                weapon.magCapacity = shotgunUpgradedMag;
                weapon.maxAmmo = shotgunUpgradedMax;
                break;
            case WeaponType.Sniper:
                weapon.isUpgraded = true;
                weapon.penetrationCount = sniperUpgradePenetration;
                weapon.magCapacity = sniperUpgradedMag;
                weapon.maxAmmo = sniperUpgradedMax;
                break;
        }
    }
}