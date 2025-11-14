using UnityEngine;
using System.Collections;

public class PowerUpManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    [SerializeField] private MovementController playerMovement;
    [SerializeField] private PlayerShooting playerShooting;

    [Header("UI Managers")]
    // Referencias a los scripts que controlan la interfaz de usuario
    [SerializeField] private PowerUpUIDisplay uiDisplay; 
    [SerializeField] private PowerUpUIAnimator uiAnimator; 

    // Banderas para evitar que los boosts se stackeen o se apliquen varias veces
    private bool speedBoostActive = false;
    private bool reloadBoostActive = false;
    private bool damageBoostActive = false;

    void Start()
    {
        // Obtener referencias a otros componentes en el mismo GameObject
        playerHealth = GetComponent<PlayerHealth>();
        
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
        if (playerMovement == null || speedBoostActive) return;

        GameObject icon = null;
        if (uiDisplay != null) icon = uiDisplay.AddPowerUpIcon(data);
        
        speedBoostActive = true;

        if (data.duration > 0)
        {
            StartCoroutine(SpeedBoostCoroutine(data, icon));
        }
        else
        {
            playerMovement.speedMultiplier = data.speedMultiplier;
            Debug.Log($"Bebida energética PERMANENTE: velocidad x{data.speedMultiplier}.");
        }
    }

    private void ApplyReloadBoost(PowerUpData data)
    {
        if (playerShooting == null || reloadBoostActive) return;

        GameObject icon = null;
        if (uiDisplay != null) icon = uiDisplay.AddPowerUpIcon(data);

        reloadBoostActive = true;

        if (data.duration > 0)
        {
            StartCoroutine(ReloadBoostCoroutine(data, icon));
        }
        else
        {
            playerShooting.reloadTimeMultiplier = data.reloadMultiplier;
            Debug.Log($"Patatas bravas PERMANENTES: recarga x{data.reloadMultiplier}.");
        }
    }

    private void ApplyDamageBoost(PowerUpData data)
    {
        if (playerShooting == null || damageBoostActive) return;

        GameObject icon = null;
        if (uiDisplay != null) icon = uiDisplay.AddPowerUpIcon(data);

        damageBoostActive = true;

        if (data.duration > 0)
        {
            StartCoroutine(DamageBoostCoroutine(data, icon));
        }
        else
        {
            playerShooting.damageMultiplier = data.damageMultiplier;
            Debug.Log($"Schpeppes PERMANENTE: daño x{data.damageMultiplier}.");
        }
    }

    // --- COROUTINES PARA EFECTOS TEMPORALES (Reciben el icono para destruirlo) ---

    private IEnumerator SpeedBoostCoroutine(PowerUpData data, GameObject iconGO)
    {
        float originalMultiplier = playerMovement.speedMultiplier;
        playerMovement.speedMultiplier = data.speedMultiplier;
        
        yield return new WaitForSeconds(data.duration);

        playerMovement.speedMultiplier = originalMultiplier;
        if (iconGO != null) Destroy(iconGO); // Elimina el icono pequeño de la lista
        speedBoostActive = false;
        Debug.Log("Efecto de Bebida energética terminado.");
    }

    private IEnumerator ReloadBoostCoroutine(PowerUpData data, GameObject iconGO)
    {
        float originalMultiplier = playerShooting.reloadTimeMultiplier;
        playerShooting.reloadTimeMultiplier = data.reloadMultiplier;
        
        yield return new WaitForSeconds(data.duration);

        playerShooting.reloadTimeMultiplier = originalMultiplier;
        if (iconGO != null) Destroy(iconGO);
        reloadBoostActive = false;
        Debug.Log("Efecto de Patatas bravas terminado.");
    }

    private IEnumerator DamageBoostCoroutine(PowerUpData data, GameObject iconGO)
    {
        float originalMultiplier = playerShooting.damageMultiplier;
        playerShooting.damageMultiplier = data.damageMultiplier;
        
        yield return new WaitForSeconds(data.duration);

        playerShooting.damageMultiplier = originalMultiplier;
        if (iconGO != null) Destroy(iconGO);
        damageBoostActive = false;
        Debug.Log("Efecto de Schpeppes terminado.");
    }
}