using UnityEngine;
using TMPro;
using System.Collections.Generic;

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
    [SerializeField] private TextMeshProUGUI hudText;

    // --- MODIFICADO ---
    private Camera playerCamera; // NUEVO: Referencia a la cámara del jugador
    private PowerUpManager playerPowerUpManager;
    private PlayerHealth playerHealth;
    // private Transform player; // ELIMINADO: Ya no usamos la posición del jugador
    private bool isPlayerLooking = false; // RENOMBRADO: de "isPlayerNear" a "isPlayerLooking"

    // ... (El resto de las variables estáticas no cambian) ...
    private static System.Collections.Generic.HashSet<PowerUpType> ownedPowerUps = new System.Collections.Generic.HashSet<PowerUpType>();

    private void Start()
    {
        // --- MODIFICADO ---
        // Buscar Cámara
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("No se encontró una cámara con el tag 'MainCamera' en la escena!");
            return;
        }

        // Buscar jugador
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError("No se encontró un GameObject con tag 'Player' en la escena!");
            return;
        }

        // player = playerGO.transform; // ELIMINADO: Ya no necesitamos esto

        // ... (El resto de Start() no cambia: GetComponent de PowerUpManager, PlayerHealth, Renderer, etc.) ...
        playerPowerUpManager = playerGO.GetComponent<PowerUpManager>();
        if (playerPowerUpManager == null)
        {
            Debug.LogError("El jugador no tiene PowerUpManager! Agrégalo al jugador.");
        }

        playerHealth = playerGO.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("El jugador no tiene PlayerHealth! Necesario para la tienda.");
        }

        rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = baseColor;

        if (hudText != null) hudText.gameObject.SetActive(false);

        if (ScoreManager.Instance == null)
            Debug.LogError("ScoreManager no encontrado en la escena!");
    }

    private void Update()
    {
        // --- MODIFICADO ---
        // CheckProximity(); // Renombrado
        CheckForInteraction();
    }

    // --- MODIFICADO ---
    // El método CheckProximity() ha sido reemplazado por CheckForInteraction()
    private void CheckForInteraction()
    {
        if (playerCamera == null || powerUpData == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // 1. Lanzamos el Raycast
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // 2. Comprobamos si golpea ESTE objeto
            if (hit.collider.gameObject == gameObject)
            {
                // 3. Estamos mirando el objeto
                if (!isPlayerLooking)
                {
                    isPlayerLooking = true;
                    Highlight(true);
                }

                ShowPowerUpInfo();

                if (Input.GetKeyDown(interactionKey))
                    TryPurchase();

                return; // Importante: Salir para no ejecutar la lógica de "dejar de mirar"
            }
        }

        // 4. Si el Raycast falla O golpea otro objeto
        //    (y estábamos mirando antes)
        if (isPlayerLooking)
        {
            isPlayerLooking = false;
            HidePowerUpInfo();
            Highlight(false);
        }
    }

    // --- NINGÚN CAMBIO DE AQUÍ EN ADELANTE ---
    // Los métodos ShowPowerUpInfo(), HidePowerUpInfo(), Highlight(), TryPurchase()
    // y los métodos estáticos (Reset, Get, Load) son idénticos.

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
        // ... (Este método no cambia en absoluto) ...
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

        if (!isArmorPowerUp && ownedPowerUps.Contains(powerUpData.powerUpType))
        {
            Debug.Log($"{powerUpData.powerUpName} ya comprado.");
            return;
        }

        if (isArmorPowerUp && playerHealth != null && playerHealth.currentArmor >= playerHealth.maxArmor)
        {
            Debug.Log("Armadura ya al máximo, no se puede comprar.");
            ShowPowerUpInfo();
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
            if (!isArmorPowerUp)
            {
                ownedPowerUps.Add(powerUpData.powerUpType);
            }

            playerPowerUpManager.ApplyPowerUp(powerUpData);
            Debug.Log($"Has comprado {powerUpData.powerUpName} por {powerUpData.cost} puntos.");

            ShowPowerUpInfo();

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

    public static void ResetOwnedPowerUps()
    {
        if (ownedPowerUps != null)
        {
            ownedPowerUps.Clear();
        }
        Debug.Log("Datos estáticos de PowerUps reseteados.");
    }

    public static HashSet<PowerUpType> GetOwnedPowerUps()
    {
        return ownedPowerUps;
    }

    public static void LoadOwnedPowerUps(List<PowerUpType> loadedPowerUps)
    {
        ownedPowerUps.Clear();
        foreach (var type in loadedPowerUps)
        {
            ownedPowerUps.Add(type);
        }
        Debug.Log("Power-ups cargados en la tienda estática.");
    }
}