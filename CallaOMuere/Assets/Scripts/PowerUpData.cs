using UnityEngine;

public enum PowerUpType
{
    Armadura,   // Restaura armadura
    Velocidad,  // Aumenta velocidad temporal
    Recarga,    // Aumenta velocidad de recarga temporal
    Daño        // Aumenta daño temporal
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "PowerUps/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    [Header("Datos básicos")]
    public PowerUpType powerUpType;    // Tipo de Power-Up
    public string powerUpName;         // Nombre a mostrar en la UI
    [TextArea] public string description;  // Descripción para la tienda o HUD
    public int cost = 0;               // Precio del Power-Up

    [Header("Duración del efecto (0 = instantáneo o permanente)")]
    public float duration = 0f;

    [Header("Valores de efectos")]
    public float armorRestore = 0f;      // Solo BocataDeCalamares
    public float speedMultiplier = 1f;   // Solo BebidaEnergetica
    public float reloadMultiplier = 1f;  // Solo PatatasBravas
    public float damageMultiplier = 1f;  // Solo Schpeppes
}
