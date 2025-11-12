using System.Collections;
using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private MovementController playerMovement;
    private PlayerShooting playerShooting;

    private float originalSpeed;
    private float originalSprintMultiplier;

    private bool speedBoostActive = false;
    private bool reloadBoostActive = false;
    private bool damageBoostActive = false;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<MovementController>();
        playerShooting = GetComponent<PlayerShooting>();

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
                StartCoroutine(ApplySpeedBoost(powerUp));
                break;
            case PowerUpType.Recarga:
                StartCoroutine(ApplyReloadBoost(powerUp));
                break;
            case PowerUpType.Daño:
                StartCoroutine(ApplyDamageBoost(powerUp));
                break;
        }
    }

    private void ApplyArmorRestore(PowerUpData data)
    {
        if (playerHealth == null) return;

        playerHealth.RestoreArmor(data.armorRestore);
        Debug.Log($"Bocata de calamares consumido: blindaje restaurado +{data.armorRestore}.");
    }

    private IEnumerator ApplySpeedBoost(PowerUpData data)
    {
        if (playerMovement == null || speedBoostActive) yield break;

        speedBoostActive = true;

        playerMovement.ApplySpeedMultiplier(data.speedMultiplier, data.duration);
        Debug.Log($"Bebida energética activada: velocidad aumentada x{data.speedMultiplier} por {data.duration} segundos.");

        yield return new WaitForSeconds(data.duration);

        speedBoostActive = false;
        Debug.Log("Efecto de velocidad finalizado.");
    }

    private IEnumerator ApplyReloadBoost(PowerUpData data)
    {
        if (playerShooting == null || reloadBoostActive) yield break;

        reloadBoostActive = true;

        // Aplicar multiplicador de recarga a todas las armas
        foreach (var weapon in FindObjectsOfType<WeaponData>())
        {
            weapon.reloadTime *= data.reloadMultiplier;
        }

        Debug.Log($"Patatas bravas activadas: recarga más rápida x{data.reloadMultiplier} por {data.duration} segundos.");

        yield return new WaitForSeconds(data.duration);

        // Restaurar recarga original
        foreach (var weapon in FindObjectsOfType<WeaponData>())
        {
            weapon.reloadTime /= data.reloadMultiplier;
        }

        reloadBoostActive = false;
        Debug.Log("Efecto de recarga finalizado.");
    }

    private IEnumerator ApplyDamageBoost(PowerUpData data)
    {
        if (playerShooting == null || damageBoostActive) yield break;

        damageBoostActive = true;
        float originalDamage = playerShooting.currentWeapon.damage;

        playerShooting.currentWeapon.damage *= data.damageMultiplier;
        Debug.Log($"Schpeppes activado: daño aumentado x{data.damageMultiplier} por {data.duration} segundos.");

        yield return new WaitForSeconds(data.duration);

        playerShooting.currentWeapon.damage = originalDamage;
        damageBoostActive = false;
        Debug.Log("Efecto de daño finalizado.");
    }
}
