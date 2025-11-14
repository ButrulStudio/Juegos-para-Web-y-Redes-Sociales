using UnityEngine;

public enum PowerUpType
{
    Armadura,   // Restaura armadura (INSTANTÁNEO)
    Velocidad,  // Aumenta velocidad (TEMPORAL/PERMANENTE)
    Recarga,    // Aumenta velocidad de recarga (TEMPORAL/PERMANENTE)
    Daño        // Aumenta daño (TEMPORAL/PERMANENTE)
}

[CreateAssetMenu(fileName = "NewPowerUp", menuName = "PowerUps/PowerUp Data")]
public class PowerUpData : ScriptableObject
{
    [Header("Datos básicos")]
    public PowerUpType powerUpType;
    public string powerUpName;
    [TextArea] public string description;
    public int cost = 0;
    
    
    [Header("UI")]
    public Sprite icon;

    [Header("Valores de efectos")]
    public float armorRestore = 0f;
    public float speedMultiplier = 1f;
    public float reloadMultiplier = 1f;
    public float damageMultiplier = 1f;
}