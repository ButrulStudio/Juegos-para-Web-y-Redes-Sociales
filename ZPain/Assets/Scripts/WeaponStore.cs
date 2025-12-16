using UnityEngine;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
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

    [Header("Sonidos")]
    public AudioClip purchaseSound;
    private AudioSource audioSource;

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    private static Dictionary<WeaponType, WeaponData> ownedWeaponInstances = new Dictionary<WeaponType, WeaponData>();

    void Start()
    {
        playerCamera = Camera.main;
        playerShooting = FindObjectOfType<PlayerShooting>();
        audioSource = GetComponent<AudioSource>();

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
            if (hit.collider.gameObject == gameObject)
            {
                playerLooking = true;
                ShowInteractionMessage();

                if (Input.GetKeyDown(interactionKey) || (playerShooting != null && playerShooting.mobileInteractPressed))
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

        bool currentlyHasWeapon = playerShooting.HasWeapon(weaponData.weaponType);

        if (!currentlyHasWeapon)
        {
            if (weaponData.price <= 0)
                interactionText.text = $"Pulsa [{interactionKey}] para coger {weaponData.weaponName} (gratis)";
            else
                interactionText.text = $"Pulsa [{interactionKey}] para comprar {weaponData.weaponName} por {weaponData.price} puntos";
        }
        else
        {
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

        bool currentlyHasWeapon = playerShooting.HasWeapon(weaponData.weaponType);

        if (!currentlyHasWeapon)
        {
            int cost = (int)weaponData.price;

            if (cost <= 0 || ScoreManager.Instance.TrySpendPoints(cost))
            {
                playerShooting.EquipWeapon(weaponData);
                playerShooting.ForceCurrentWeaponAmmoToFull();

                if (!ownedWeaponInstances.ContainsKey(weaponData.weaponType))
                {
                    ownedWeaponInstances.Add(weaponData.weaponType, weaponData);
                }

                if (audioSource != null && purchaseSound != null)
                {
                    audioSource.PlayOneShot(purchaseSound);
                }

                interactionText.text = "¡Arma comprada!";
                ShowInteractionMessage();
            }
            else
            {
                interactionText.text = "No tienes suficientes puntos";
            }
        }
        else
        {
            if (playerShooting.IsAmmoFullForType(weaponData)) return;

            if (ScoreManager.Instance.TrySpendPoints(weaponData.ammoPrice))
            {
                playerShooting.RefillAmmoForType(weaponData.weaponType);

                if (audioSource != null && purchaseSound != null)
                {
                    audioSource.PlayOneShot(purchaseSound);
                }

                interactionText.text = "¡Munición Recargada!";
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