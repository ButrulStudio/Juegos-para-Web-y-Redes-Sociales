using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WeaponStore : MonoBehaviour
{
    [Header("Configuración del arma en la tienda")]
    [Tooltip("El ScriptableObject base del arma que vende esta pared")]
    public WeaponData weaponData;

    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI del mensaje")]
    public TextMeshProUGUI interactionText;

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    // Diccionario para historial (opcional, no afecta la compra lógica ahora)
    private static Dictionary<WeaponType, WeaponData> ownedWeaponInstances = new Dictionary<WeaponType, WeaponData>();

    void Start()
    {
        playerCamera = Camera.main;
        playerShooting = FindObjectOfType<PlayerShooting>();

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    void Update()
    {
        CheckForInteraction();
    }

    void CheckForInteraction()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // Verificamos si miramos a este objeto de tienda
            if (hit.collider.gameObject == gameObject)
            {
                playerLooking = true;
                ShowInteractionMessage();

                if (Input.GetKeyDown(interactionKey))
                {
                    TryPurchaseOrEquip();
                }
                return;
            }
        }

        if (playerLooking)
        {
            playerLooking = false;
            if (interactionText != null)
                interactionText.gameObject.SetActive(false);
        }
    }

    void ShowInteractionMessage()
    {
        if (interactionText == null || playerShooting == null || weaponData == null) return;

        interactionText.gameObject.SetActive(true);

        // 1. ¿Llevamos el arma encima AHORA MISMO?
        bool currentlyHasWeapon = playerShooting.HasWeapon(weaponData.weaponType);

        if (!currentlyHasWeapon)
        {
            // MODO COMPRA DE ARMA (Porque no la llevas)
            if (weaponData.price <= 0)
                interactionText.text = $"Pulsa [{interactionKey}] para coger {weaponData.weaponName} (gratis)";
            else
                interactionText.text = $"Pulsa [{interactionKey}] para comprar {weaponData.weaponName} por {weaponData.price} puntos";
        }
        else
        {
            // MODO COMPRA DE MUNICIÓN (Porque sí la llevas)
            // Comprobamos si la munición está llena para el arma Específica
            if (playerShooting.IsAmmoFullForType(weaponData))
            {
                interactionText.text = "Munición Completa";
            }
            else
            {
                interactionText.text = $"Pulsa [{interactionKey}] para comprar munición por {weaponData.ammoPrice} puntos";
            }
        }
    }

    void TryPurchaseOrEquip()
    {
        if (playerShooting == null || weaponData == null) return;

        // Comprobamos si la tiene equipada o en la mochila
        bool currentlyHasWeapon = playerShooting.HasWeapon(weaponData.weaponType);

        if (!currentlyHasWeapon)
        {
            // --- COMPRA DE ARMA NUEVA ---
            int cost = (int)weaponData.price;

            if (cost <= 0 || ScoreManager.Instance.TrySpendPoints(cost))
            {
                // EquipWeapon crea una copia limpia (sin mejoras) y la pone en el slot
                playerShooting.EquipWeapon(weaponData);
                playerShooting.ForceCurrentWeaponAmmoToFull();

                // Registro histórico (opcional)
                if (!ownedWeaponInstances.ContainsKey(weaponData.weaponType))
                {
                    ownedWeaponInstances.Add(weaponData.weaponType, weaponData);
                }

                interactionText.text = "¡Arma comprada!";
                Debug.Log($"Has comprado {weaponData.weaponName} por {cost} puntos.");

                // Actualizamos mensaje inmediatamente
                ShowInteractionMessage();
            }
            else
            {
                interactionText.text = "No tienes suficientes puntos";
            }
        }
        else
        {
            // --- COMPRA DE MUNICIÓN ---
            if (playerShooting.IsAmmoFullForType(weaponData)) return; // Ya está llena

            if (ScoreManager.Instance.TrySpendPoints(weaponData.ammoPrice))
            {
                // Rellenamos la munición del arma específica
                playerShooting.RefillAmmoForType(weaponData.weaponType);

                interactionText.text = "¡Munición Recargada!";
                Debug.Log("Munición comprada.");

                ShowInteractionMessage();
            }
            else
            {
                interactionText.text = "No tienes suficientes puntos";
            }
        }
    }

    public static void ResetOwnedWeapons()
    {
        ownedWeaponInstances.Clear();
    }

    public static void RegisterStartingWeapon(WeaponData weaponInstance)
    {
        if (weaponInstance == null) return;
        if (!ownedWeaponInstances.ContainsKey(weaponInstance.weaponType))
        {
            ownedWeaponInstances.Add(weaponInstance.weaponType, weaponInstance);
        }
    }
}