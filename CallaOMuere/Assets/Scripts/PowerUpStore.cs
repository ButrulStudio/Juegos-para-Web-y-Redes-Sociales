using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class PowerUpStore : MonoBehaviour
{
    [Header("Configuración del Power-Up")]
    // El ScriptableObject que define qué PowerUp vende esta tienda.
    public PowerUpData powerUpData;

    [Header("Interacción")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.F;

    [Header("Visual")]
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] private Color highlightColor = Color.cyan;
    private Renderer rend; // Renderer del objeto para cambiar su color al mirarlo.

    [Header("UI del HUD")]
    [SerializeField] private TextMeshProUGUI hudText; // Texto en pantalla para mostrar el precio/info.

    // Referencias a componentes del jugador (se buscan en Start).
    private Camera playerCamera; // Referencia a la cámara del jugador.
    private PowerUpManager playerPowerUpManager;
    private PlayerHealth playerHealth;
    private bool isPlayerLooking = false; // Flag para saber si el jugador está mirando el objeto.

    // HashSet estático para almacenar los PowerUps comprados.
    private static System.Collections.Generic.HashSet<PowerUpType> ownedPowerUps = new System.Collections.Generic.HashSet<PowerUpType>();

    private void Start()
    {
        // --- Búsqueda de Referencias ---
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            Debug.LogError("No se encontró una cámara con el tag 'MainCamera' en la escena!");
            return;
        }

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null)
        {
            Debug.LogError("No se encontró un GameObject con tag 'Player' en la escena!");
            return;
        }

        // Obtener los componentes necesarios del jugador.
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

        // --- Configuración Inicial ---
        rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = baseColor; // Color base inicial.

        if (hudText != null) hudText.gameObject.SetActive(false); // Ocultar texto de UI.

        if (ScoreManager.Instance == null)
            Debug.LogError("ScoreManager no encontrado en la escena!");
    }

    private void Update()
    {
        // Comprueba si el jugador está mirando e intentando interactuar.
        CheckForInteraction();
    }

    /// <summary>
    /// Comprueba si el jugador está mirando a esta tienda usando un Raycast
    /// y gestiona la interacción.
    /// </summary>
    private void CheckForInteraction()
    {
        if (playerCamera == null || powerUpData == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // 1. Lanzamos el Raycast desde la cámara.
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // 2. Comprobamos si el rayo golpea ESTE objeto.
            if (hit.collider.gameObject == gameObject)
            {
                // 3. El jugador está mirando el objeto.
                if (!isPlayerLooking)
                {
                    // Si es la primera vez que mira, activamos el Highlight.
                    isPlayerLooking = true;
                    Highlight(true);
                }

                ShowPowerUpInfo(); // Muestra el mensaje de UI (precio, etc.).

                // Comprueba si pulsa la tecla de interacción.
                if (Input.GetKeyDown(interactionKey))
                    TryPurchase();

                return; // Salimos para no ejecutar la lógica de "dejar de mirar".
            }
        }

        if (isPlayerLooking)
        {
            isPlayerLooking = false;
            HidePowerUpInfo();
            Highlight(false);
        }
    }

    /// <summary>
    /// Muestra el mensaje contextual en el HUD (ej. precio, "Ya adquirido", etc.).
    /// </summary>
    private void ShowPowerUpInfo()
    {
        if (hudText == null || powerUpData == null) return;

        bool isArmorPowerUp = powerUpData.powerUpType == PowerUpType.Armadura;

        // Caso 1: Es armadura y el jugador ya tiene el máximo.
        if (isArmorPowerUp && playerHealth != null && playerHealth.currentArmor >= playerHealth.maxArmor)
        {
            hudText.gameObject.SetActive(true);
            hudText.text = $"{powerUpData.powerUpName} — BLINDAJE COMPLETO";
            return;
        }

        // Caso 2: Es un PowerUp permanente y ya lo ha comprado.
        bool alreadyOwned = ownedPowerUps.Contains(powerUpData.powerUpType);
        if (!isArmorPowerUp && alreadyOwned)
        {
            hudText.gameObject.SetActive(true);
            hudText.text = $"{powerUpData.powerUpName} — YA ADQUIRIDO";
            return;
        }

        // Caso 3: Mensaje normal de compra.
        hudText.gameObject.SetActive(true);
        hudText.text =
            $"{powerUpData.powerUpName} — <color=yellow>{powerUpData.cost} pts</color>\nPulsa [{interactionKey}] para comprar";
    }

    /// <summary>
    /// Oculta el texto del HUD.
    /// </summary>
    private void HidePowerUpInfo()
    {
        if (hudText == null) return;
        hudText.gameObject.SetActive(false);
    }

    /// <summary>
    /// Activa/desactiva el color de "highlight" en el material del objeto.
    /// </summary>
    private void Highlight(bool active)
    {
        if (rend == null) return;
        rend.material.color = active ? highlightColor : baseColor;
    }

    /// <summary>
    /// Lógica que se ejecuta al pulsar la tecla de interacción.
    /// Valida si la compra es posible y la efectúa.
    /// </summary>
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

        // Validación 1: ¿Es permanente y ya lo tiene?
        if (!isArmorPowerUp && ownedPowerUps.Contains(powerUpData.powerUpType))
        {
            Debug.Log($"{powerUpData.powerUpName} ya comprado.");
            return;
        }

        // Validación 2: ¿Es armadura y ya está al máximo?
        if (isArmorPowerUp && playerHealth != null && playerHealth.currentArmor >= playerHealth.maxArmor)
        {
            Debug.Log("Armadura ya al máximo, no se puede comprar.");
            ShowPowerUpInfo(); // Actualiza el texto a "BLINDAJE COMPLETO".
            return;
        }

        // Validación 3: ¿Tiene suficientes puntos?
        int currentPoints = ScoreManager.Instance.GetCurrentScore();
        if (currentPoints < powerUpData.cost)
        {
            Debug.Log($"No tienes suficientes puntos para comprar {powerUpData.powerUpName}.");
            return;
        }

        // Intenta gastar los puntos.
        bool paid = ScoreManager.Instance.TrySpendPoints(powerUpData.cost);

        if (paid)
        {
            // Si el pago es exitoso:
            // 1. Si no es armadura, lo añade a la lista de comprados.
            if (!isArmorPowerUp)
            {
                ownedPowerUps.Add(powerUpData.powerUpType);
            }

            // 2. Aplica el efecto al jugador.
            playerPowerUpManager.ApplyPowerUp(powerUpData);
            Debug.Log($"Has comprado {powerUpData.powerUpName} por {powerUpData.cost} puntos.");

            // 3. Actualiza el texto de la UI (ej. a "YA ADQUIRIDO").
            ShowPowerUpInfo();

            // 4. Si compró armadura y llegó al máximo, oculta la interacción.
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

    // --- MÉTODOS ESTÁTICOS ---
    /// <summary>
    /// Limpia el HashSet de PowerUps comprados (ej. al reintentar o salir).
    /// </summary>
    public static void ResetOwnedPowerUps()
    {
        if (ownedPowerUps != null)
        {
            ownedPowerUps.Clear();
        }
        Debug.Log("Datos estáticos de PowerUps reseteados.");
    }

    /// <summary>
    /// Devuelve el HashSet de PowerUps (para el SaveLoadManager).
    /// </summary>
    public static HashSet<PowerUpType> GetOwnedPowerUps()
    {
        return ownedPowerUps;
    }

    /// <summary>
    /// Carga la lista de PowerUps desde un archivo de guardado.
    /// </summary>
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