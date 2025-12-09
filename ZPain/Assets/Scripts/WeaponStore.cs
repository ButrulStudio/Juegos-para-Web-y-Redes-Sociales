using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WeaponStore : MonoBehaviour
{
    [Header("Configuración del arma en la tienda")]
    public WeaponData weaponData;

    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI del mensaje (opcional)")]
    public TextMeshProUGUI interactionText;

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    // Se mantiene estático para la sesión de juego actual
    private static Dictionary<WeaponType, WeaponData> ownedWeaponInstances = new Dictionary<WeaponType, WeaponData>();

    void Start()
    {
        playerCamera = Camera.main;
        playerShooting = FindAnyObjectByType<PlayerShooting>();

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
        if (interactionText == null || playerShooting == null) return;

        bool alreadyOwned = ownedWeaponInstances.ContainsKey(weaponData.weaponType);
        bool isAmmoFull = playerShooting.IsAmmoFull(weaponData);
        bool isEquipped = playerShooting.GetEquippedWeaponType() == weaponData.weaponType;

        interactionText.gameObject.SetActive(true);

        if (!alreadyOwned)
        {
            if (weaponData.price <= 0)
                interactionText.text = "Pulsa [" + interactionKey + "] para coger " + weaponData.weaponName + " (gratis)";
            else
                interactionText.text = "Pulsa [" + interactionKey + "] para comprar " + weaponData.weaponName + " por " + weaponData.price + " puntos";
            return;
        }

        if (isEquipped && isAmmoFull)
        {
            interactionText.text = $"{weaponData.weaponName} — MUNICIÓN LLENA";
            return;
        }

        interactionText.text = "Pulsa [" + interactionKey + "] para comprar munición de " + weaponData.weaponName + " por " + weaponData.ammoPrice + " puntos";
    }

    void TryPurchaseOrEquip()
    {
        if (playerShooting == null) return;

        bool alreadyOwned = ownedWeaponInstances.ContainsKey(weaponData.weaponType);
        bool isAmmoFull = playerShooting.IsAmmoFull(weaponData);
        bool isEquipped = playerShooting.GetEquippedWeaponType() == weaponData.weaponType;

        if (!alreadyOwned)
        {
            WeaponData newWeaponInstance = Instantiate(weaponData);
            int cost = (int)weaponData.price;

            if (cost <= 0 || ScoreManager.Instance.TrySpendPoints(cost))
            {
                ownedWeaponInstances.Add(newWeaponInstance.weaponType, newWeaponInstance);
                playerShooting.EquipWeapon(newWeaponInstance);
                playerShooting.ForceCurrentWeaponAmmoToFull();
                Debug.Log($"Has comprado {newWeaponInstance.weaponName} por {cost} puntos.");
            }
            else
            {
                Destroy(newWeaponInstance);
                Debug.Log("No tienes suficientes puntos para comprar esta arma.");
            }
            return;
        }

        if (isEquipped && isAmmoFull)
        {
            Debug.Log("Munición ya al máximo. No se puede comprar.");
            ShowInteractionMessage();
            return;
        }

        int ammoCost = weaponData.ammoPrice;
        if (!ScoreManager.Instance.TrySpendPoints(ammoCost))
        {
            Debug.Log("No tienes suficientes puntos para comprar munición.");
            return;
        }

        WeaponData instanceToEquip = ownedWeaponInstances[weaponData.weaponType];
        playerShooting.EquipWeapon(instanceToEquip);
        playerShooting.ForceCurrentWeaponAmmoToFull();

        Debug.Log("Has pagado la munición y equipado " + instanceToEquip.weaponName + ".");
        ShowInteractionMessage();
    }

    public static void ResetOwnedWeapons()
    {
        if (ownedWeaponInstances != null)
        {
            foreach (var weaponInstance in ownedWeaponInstances.Values)
            {
                if (weaponInstance != null)
                {
                    Destroy(weaponInstance);
                }
            }
            ownedWeaponInstances.Clear();
        }
        Debug.Log("Datos estáticos de Armas reseteados.");
    }

    public static ICollection<WeaponData> GetOwnedWeaponData()
    {
        return ownedWeaponInstances.Values;
    }

    public static void RegisterStartingWeapon(WeaponData weaponInstance)
    {
        if (weaponInstance == null) return;

        if (ownedWeaponInstances.Count == 0)
        {
            ownedWeaponInstances.Clear();
        }

        if (!ownedWeaponInstances.ContainsKey(weaponInstance.weaponType))
        {
            ownedWeaponInstances.Add(weaponInstance.weaponType, weaponInstance);
            Debug.Log($"Arma inicial {weaponInstance.weaponName} registrada en la tienda.");
        }
    }
}