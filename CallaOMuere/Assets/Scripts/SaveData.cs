using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeaponAmmoData
{
    public WeaponType weaponType;
    public int currentMagAmmo;
    public int currentTotalAmmo;
}


[System.Serializable]
public class SaveData
{
    // Datos del jugador
    public float currentHealth;
    public float currentArmor;

    // Datos de la partida
    public int currentScore;
    public int currentWave;
    public int zombiesRemainingInWave;

    // Datos de inventario
    public List<PowerUpType> ownedPowerUps;
    public List<WeaponType> ownedWeapons;
    public List<WeaponType> upgradedWeapons;
    public WeaponType equippedWeaponType; 

    public List<WeaponAmmoData> ammoData;
}