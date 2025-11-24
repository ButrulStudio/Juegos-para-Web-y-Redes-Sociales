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

    // Datos de los slots del inventario 
    public int weaponTypeInSlot0 = -1;
    public int weaponTypeInSlot1 = -1;
    public int activeSlotIndex = 0;

    public WeaponType equippedWeaponType;

    public List<WeaponAmmoData> ammoData;
}