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
    private bool isPlayerNear = false;

    // Evita comprar varias veces el mismo power-up
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
            Debug.LogError("El jugador no tiene PowerUpManager! Agrégalo al jugador.");

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
                ShowPowerUpInfo();
                Highlight(true);
            }

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
        if (ScoreManager.Instance == null || playerPowerUpManager == null)
        {
            Debug.LogWarning("Faltan referencias al ScoreManager o PowerUpManager.");
            return;
        }

        if (ownedPowerUps.Contains(powerUpData.powerUpType))
        {
            Debug.Log($"{powerUpData.powerUpName} ya comprado.");
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
            ownedPowerUps.Add(powerUpData.powerUpType);
            playerPowerUpManager.ApplyPowerUp(powerUpData);
            Debug.Log($"Has comprado {powerUpData.powerUpName} por {powerUpData.cost} puntos.");
            HidePowerUpInfo();
            Highlight(false);
        }
        else
        {
            Debug.Log($"No se pudo gastar los puntos. (Saldo actual: {currentPoints})");
        }
    }
}
