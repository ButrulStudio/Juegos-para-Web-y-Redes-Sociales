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

    private Camera playerCamera;
    private PowerUpManager playerPowerUpManager;
    private PlayerHealth playerHealth;
    private bool isPlayerLooking = false;

    // HashSet estático para almacenar los PowerUps comprados EN LA SESIÓN ACTUAL
    private static HashSet<PowerUpType> ownedPowerUps = new HashSet<PowerUpType>();

    private void Start()
    {
        playerCamera = Camera.main;
        if (playerCamera == null) return;

        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) return;

        playerPowerUpManager = playerGO.GetComponent<PowerUpManager>();
        playerHealth = playerGO.GetComponent<PlayerHealth>();

        rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = baseColor;
        if (hudText != null) hudText.gameObject.SetActive(false);
    }

    private void Update()
    {
        CheckForInteraction();
    }

    private void CheckForInteraction()
    {
        if (playerCamera == null || powerUpData == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isPlayerLooking)
                {
                    isPlayerLooking = true;
                    Highlight(true);
                }

                ShowPowerUpInfo();

                if (Input.GetKeyDown(interactionKey))
                    TryPurchase();

                return;
            }
        }

        if (isPlayerLooking)
        {
            isPlayerLooking = false;
            HidePowerUpInfo();
            Highlight(false);
        }
    }

    private void ShowPowerUpInfo()
    {
        if (hudText == null || powerUpData == null) return;

        bool isArmorPowerUp = powerUpData.powerUpType == PowerUpType.Armadura;

        if (isArmorPowerUp && playerHealth != null && playerHealth.currentArmor >= playerHealth.maxArmor)
        {
            hudText.gameObject.SetActive(true);
            hudText.text = $"{powerUpData.powerUpName} — BLINDAJE COMPLETO";
            return;
        }

        bool alreadyOwned = ownedPowerUps.Contains(powerUpData.powerUpType);
        if (!isArmorPowerUp && alreadyOwned)
        {
            hudText.gameObject.SetActive(true);
            hudText.text = $"{powerUpData.powerUpName} — YA ADQUIRIDO";
            return;
        }

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
        if (ScoreManager.Instance == null || playerPowerUpManager == null) return;

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
    }

    // --- MÉTODOS ESTÁTICOS ---
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
}