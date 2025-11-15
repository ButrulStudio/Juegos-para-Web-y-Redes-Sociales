using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private MovementController playerMovement;
    [SerializeField] private PlayerShooting playerShooting;

    [Header("UI Managers")]
    // Referencias a los scripts que controlan la interfaz de usuario
    private PowerUpUIDisplay uiDisplay; 
    private PowerUpUIAnimator uiAnimator; 


    void Start()
    {
        // Obtener referencias a otros componentes en el mismo GameObject
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<MovementController>();

        uiDisplay = FindObjectOfType<PowerUpUIDisplay>();
        uiAnimator = FindObjectOfType<PowerUpUIAnimator>();

        // Mensajes de advertencia si las referencias de UI no están asignadas
        if (uiDisplay == null) Debug.LogWarning("PowerUpManager: PowerUpUIDisplay no asignado en el Inspector.");
        if (uiAnimator == null) Debug.LogWarning("PowerUpManager: PowerUpUIAnimator no asignado en el Inspector.");
        if (playerShooting == null) Debug.LogError("PowerUpManager no encontró el script PlayerShooting.");
    }

    public void ApplyPowerUp(PowerUpData powerUp)
    {
        if (powerUp == null) return;

        if (uiAnimator != null)
        {
            uiAnimator.AnimatePowerUpIcon(powerUp);
        }

        switch (powerUp.powerUpType)
        {
            case PowerUpType.Armadura:
                ApplyArmorRestore(powerUp); // Solo animación central + efecto instantáneo
                break;
            case PowerUpType.Velocidad:
                ApplySpeedBoost(powerUp);
                break;
            case PowerUpType.Recarga:
                ApplyReloadBoost(powerUp);
                break;
            case PowerUpType.Daño:
                ApplyDamageBoost(powerUp);
                break;
        }
    }

    // --- MÉTODOS DE APLICACIÓN ESPECÍFICOS ---

    private void ApplyArmorRestore(PowerUpData data)
    {
        if (playerHealth == null) return;
        playerHealth.RestoreArmor(data.armorRestore);
        Debug.Log($"Bocata de calamares consumido: blindaje restaurado +{data.armorRestore}.");
    }

    private void ApplySpeedBoost(PowerUpData data)
    {
        if (playerMovement == null) return;

        
        if (uiDisplay != null) { uiDisplay.AddPowerUpIcon(data); Debug.Log("polla"); }
        
        playerMovement.speedMultiplier = data.speedMultiplier;
        Debug.Log($"Bebida energética PERMANENTE: velocidad x{data.speedMultiplier}.");
        
    }

    private void ApplyReloadBoost(PowerUpData data)
    {
        if (playerShooting == null) return;

        GameObject icon = null;
        if (uiDisplay != null) icon = uiDisplay.AddPowerUpIcon(data);

        playerShooting.reloadTimeMultiplier = data.reloadMultiplier;
        Debug.Log($"Patatas bravas PERMANENTES: recarga x{data.reloadMultiplier}.");
        
    }

    private void ApplyDamageBoost(PowerUpData data)
    {
        if (playerShooting == null) return;

        GameObject icon = null;
        if (uiDisplay != null) icon = uiDisplay.AddPowerUpIcon(data);
        
        playerShooting.damageMultiplier = data.damageMultiplier;
        Debug.Log($"Schpeppes PERMANENTE: daño x{data.damageMultiplier}.");
    }

}