using UnityEngine;

public class PowerUpManager : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private MovementController playerMovement;
    [SerializeField]private PlayerShooting playerShooting;

    // Estas variables ahora solo evitan que el log se llene,
    // ya que PowerUpStore evita la recompra.
    private bool speedBoostActive = false;
    private bool reloadBoostActive = false;
    private bool damageBoostActive = false;

    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<MovementController>();

        // Prueba de depuración para asegurar que PlayerShooting se encuentra
        if (playerShooting == null)
        {
            Debug.LogError("¡¡ERROR: PowerUpManager no pudo encontrar el script PlayerShooting!!");
        }
        else
        {
            Debug.Log("PowerUpManager se conectó a PlayerShooting correctamente.");
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
                // Ya no es una corrutina
                ApplySpeedBoost(powerUp);
                break;
            case PowerUpType.Recarga:
                // Ya no es una corrutina
                ApplyReloadBoost(powerUp);
                break;
            case PowerUpType.Daño:
                // Ya no es una corrutina
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
        // El 'if' ahora solo comprueba si ya se aplicó
        if (playerMovement == null || speedBoostActive) return;

        speedBoostActive = true;

        // Llama al nuevo método permanente en MovementController
        playerMovement.SetPermanentSpeedMultiplier(data.speedMultiplier);

        Debug.Log($"Bebida energética activada: velocidad aumentada permanentemente x{data.speedMultiplier}.");

        // Se eliminó la lógica de duración y reversión
    }

    private void ApplyReloadBoost(PowerUpData data)
    {
        if (playerShooting == null || reloadBoostActive) return;

        reloadBoostActive = true;

        // LÓGICA CORREGIDA: Asigna el multiplicador en PlayerShooting
        playerShooting.reloadTimeMultiplier = data.reloadMultiplier;

        Debug.Log($"Patatas bravas activadas: recarga más rápida permanentemente x{data.reloadMultiplier}.");

        // Se eliminó la lógica de duración y reversión
    }

    private void ApplyDamageBoost(PowerUpData data)
    {
        if (playerShooting == null || damageBoostActive) return;

        damageBoostActive = true;

        // LÓGICA CORREGIDA: Asigna el multiplicador en PlayerShooting
        playerShooting.damageMultiplier = data.damageMultiplier;

        Debug.Log($"Schpeppes activado: daño aumentado permanentemente x{data.damageMultiplier}.");

        // Se eliminó la lógica de duración y reversión
    }
}