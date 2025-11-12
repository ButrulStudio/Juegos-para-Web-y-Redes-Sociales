using UnityEngine;
using TMPro;

[RequireComponent(typeof(Collider))]
public class PowerUpStore : MonoBehaviour
{
    [Header("Configuración del Power-Up")]
    public PowerUpData powerUpData;

    [Header("Interacción")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

    [Header("Visual")]
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan;
    private Renderer rend;

    [Header("UI del HUD")]
    [SerializeField] private TextMeshProUGUI hudText; // Texto del Canvas HUD

    private Transform player;
    private PowerUpManager playerPowerUpManager;
    private PlayerHealth playerHealth; // Referencia al estado de salud del jugador.
    private bool isPlayerNear = false;

    // Evita comprar varias veces el mismo power-up
    // Esta lógica es perfecta para los power-ups permanentes
    private static System.Collections.Generic.HashSet<PowerUpType> ownedPowerUps = new System.Collections.Generic.HashSet<PowerUpType>();

    private void Start()
    {
        // Buscar jugador
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError("No se encontró un GameObject con tag 'Player' en la escena!");
            return;
        }

        player = playerGO.transform;

        // Obtener PowerUpManager del jugador
        playerPowerUpManager = playerGO.GetComponent<PowerUpManager>();
        if (playerPowerUpManager == null)
        {
            Debug.LogError("El jugador no tiene PowerUpManager! Agrégalo al jugador.");
        }

        // Obtener PlayerHealth para la lógica de armadura.
        playerHealth = playerGO.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("El jugador no tiene PlayerHealth! Necesario para la tienda.");
        }

        // Renderer
        rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = baseColor;

        // HUD
        if (hudText != null) hudText.gameObject.SetActive(false);

        // ScoreManager
        if (ScoreManager.Instance == null)
            Debug.LogError("ScoreManager no encontrado en la escena!");
    }

    private void Update()
    {
        CheckProximity();
    }

    private void CheckProximity()
    {
        if (player == null || powerUpData == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        if (distance <= interactionDistance)
        {
            if (!isPlayerNear)
            {
                isPlayerNear = true;
                Highlight(true);
            }

            ShowPowerUpInfo(); // Mover la actualización de info aquí asegura que el mensaje se actualice (ej. Armadura llena/vacía)

            if (Input.GetKeyDown(interactionKey))
                TryPurchase();
        }
        else if (isPlayerNear)
        {
            isPlayerNear = false;
            HidePowerUpInfo();
            Highlight(false);
        }
    }

    private void ShowPowerUpInfo()
    {
        if (hudText == null || powerUpData == null) return;

        bool isArmorPowerUp = powerUpData.powerUpType == PowerUpType.Armadura;

        // 1. COMPROBAR SI LA ARMADURA ESTÁ AL MÁXIMO
        if (isArmorPowerUp && playerHealth != null && playerHealth.currentArmor >= playerHealth.maxArmor)
        {
            hudText.gameObject.SetActive(true);
            hudText.text = $"{powerUpData.powerUpName} — BLINDAJE COMPLETO";
            return;
        }

        // 2. COMPROBAR SI YA ESTÁ COMPRADO (Solo aplica a PowerUps NO de Armadura)
        bool alreadyOwned = ownedPowerUps.Contains(powerUpData.powerUpType);
        if (!isArmorPowerUp && alreadyOwned)
        {
            hudText.gameObject.SetActive(true);
            hudText.text = $"{powerUpData.powerUpName} — YA ADQUIRIDO";
            return;
        }


        // 3. MENSAJE NORMAL DE COMPRA
        hudText.gameObject.SetActive(true);
        hudText.text =
            $"{powerUpData.powerUpName} — <color=yellow>{powerUpData.cost} pts</color>\nPulsa [{interactionKey}] para comprar";
    }

    private void HidePowerUpInfo()
    {
        if (hudText == null) return;
        hudText.gameObject.SetActive(false);
    }

    private void Highlight(bool active)
    {
        if (rend == null) return;
        rend.material.color = active ? highlightColor : baseColor;
    }

    private void TryPurchase()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("Faltan referencias al ScoreManager.");
            return;
        }

        if (playerPowerUpManager == null)
        {
            Debug.LogWarning("Faltan referencias al PowerUpManager.");
            return;
        }

        bool isArmorPowerUp = powerUpData.powerUpType == PowerUpType.Armadura;

        // LÓGICA DE BLOQUEO DE COMPRA (NO REPETIBLE)
        if (!isArmorPowerUp && ownedPowerUps.Contains(powerUpData.powerUpType))
        {
            Debug.Log($"{powerUpData.powerUpName} ya comprado.");
            return;
        }

        // LÓGICA DE BLOQUEO DE ARMADURA LLENA (SOLO ARMADURA)
        if (isArmorPowerUp && playerHealth != null && playerHealth.currentArmor >= playerHealth.maxArmor)
        {
            Debug.Log("Armadura ya al máximo, no se puede comprar.");
            ShowPowerUpInfo(); // Asegurar que el mensaje de "completo" se muestre.
            return;
        }


        int currentPoints = ScoreManager.Instance.GetCurrentScore();

        if (currentPoints < powerUpData.cost)
        {
            Debug.Log($"No tienes suficientes puntos para comprar {powerUpData.powerUpName}.");
            return;
        }

        bool paid = ScoreManager.Instance.TrySpendPoints(powerUpData.cost);

        if (paid)
        {
            // Solo añadimos el PowerUp al set de "poseídos" si NO es el de Armadura (porque es repetible)
            if (!isArmorPowerUp)
            {
                ownedPowerUps.Add(powerUpData.powerUpType);
            }

            playerPowerUpManager.ApplyPowerUp(powerUpData);
            Debug.Log($"Has comprado {powerUpData.powerUpName} por {powerUpData.cost} puntos.");

            // Re-evaluar el estado después de la compra (ej. para el caso de armadura llena)
            ShowPowerUpInfo();

            // Si la compra fue la armadura y ya está llena, desactivar el highlight y el HUD.
            if (isArmorPowerUp && playerHealth.currentArmor >= playerHealth.maxArmor)
            {
                HidePowerUpInfo();
                Highlight(false);
            }
        }
        else
        {
            Debug.Log($"No se pudo gastar los puntos. (Saldo actual: {currentPoints})");
        }
    }
}