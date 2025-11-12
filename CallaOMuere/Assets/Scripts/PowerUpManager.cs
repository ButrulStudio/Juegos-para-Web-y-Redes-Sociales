using System.Collections;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private MovementController playerMovement;
    [SerializeField] private PlayerShooting playerShooting;


    private float originalSpeed;
    private float originalSprintMultiplier;

    private bool speedBoostActive = false;
    private bool reloadBoostActive = false;
    private bool damageBoostActive = false;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<MovementController>();
        //playerShooting = GetComponent<PlayerShooting>();

        if (playerMovement != null)
        {
            originalSpeed = playerMovement.GetVelocity();
            originalSprintMultiplier = playerMovement.GetSprintMultiplier();
        }
    }

    /// <summary>
    /// Aplica un PowerUp al jugador según su tipo
    /// </summary>
    public void ApplyPowerUp(PowerUpData powerUp)
    {
        if (powerUp == null) return;

        switch (powerUp.powerUpType)
        {
            case PowerUpType.Armadura:
                ApplyArmorRestore(powerUp);
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

    private void ApplyArmorRestore(PowerUpData data)
    {
        if (playerHealth == null) return;

        playerHealth.RestoreArmor(data.armorRestore);
        Debug.Log($"Bocata de calamares consumido: blindaje restaurado +{data.armorRestore}.");
    }

    private void ApplySpeedBoost(PowerUpData data)
    {
        if (playerMovement == null || speedBoostActive) return;

        speedBoostActive = true;

        playerMovement.SetPermanentSpeedMultiplier(data.speedMultiplier);
        Debug.Log($"Bebida energética activada: velocidad aumentada x{data.speedMultiplier}.");

    }

    private void ApplyReloadBoost(PowerUpData data)
    {
        if (playerShooting == null || reloadBoostActive) return;

        reloadBoostActive = true;

        playerShooting.reloadTimeMultiplier = data.reloadMultiplier;

        Debug.Log($"Patatas bravas activadas: recarga más rápida x{data.reloadMultiplier}.");
    }

    private void ApplyDamageBoost(PowerUpData data)
    {
        if (playerShooting == null || damageBoostActive) return;

        damageBoostActive = true;

        playerShooting.damageMultiplier = data.damageMultiplier;
        Debug.Log($"Schpeppes activado: daño aumentado x{data.damageMultiplier}.");

    }
}