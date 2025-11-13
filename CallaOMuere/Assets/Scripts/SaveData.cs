using System.Collections.Generic;

// ¡Clave! Esto permite que Unity lo convierta a/desde JSON.
[System.Serializable]
public class SaveData
{
    // Datos del jugador
    public float currentHealth;
    public float currentArmor;

    // Datos de la partida
    public int currentScore;
    public int currentWave;
    public int zombiesRemainingInWave; // Para saber en qué punto de la oleada estaba

    // Datos de inventario
    // Usamos enums simples porque se guardan fácil en JSON
    public List<PowerUpType> ownedPowerUps;
    public List<WeaponType> ownedWeapons;
    public List<WeaponType> upgradedWeapons;

    // Munición actual (¡lo más complejo!)
    // Esto es opcional pero ideal para un buen guardado
    // public Dictionary<WeaponType, int> ammoInMags;
    // public Dictionary<WeaponType, int> totalAmmo;
}