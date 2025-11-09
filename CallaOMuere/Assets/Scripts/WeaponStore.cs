using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class WeaponStore : MonoBehaviour
{
    [Header("Configuración del arma en la tienda")]
    public WeaponData weaponData; // ScriptableObject del arma que se vende

    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI del mensaje (opcional)")]
    public TextMeshProUGUI interactionText; // Texto en pantalla que mostrará el mensaje

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    // Armas que el jugador ya compró (guardadas estáticamente)
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
        if (interactionText == null) return;

        bool alreadyOwned = ownedWeaponInstances.ContainsKey(weaponData.weaponType);

        interactionText.gameObject.SetActive(true);

        if (alreadyOwned)
            interactionText.text = "Pulsa [" + interactionKey + "] para equipar " + weaponData.weaponName;
        else if (weaponData.price <= 0)
            interactionText.text = "Pulsa [" + interactionKey + "] para coger " + weaponData.weaponName + " (gratis)";
        else
            interactionText.text = "Pulsa [" + interactionKey + "] para comprar " + weaponData.weaponName + " por " + weaponData.price + " puntos";
    }

    void TryPurchaseOrEquip()
    {
        bool alreadyOwned = ownedWeaponInstances.ContainsKey(weaponData.weaponType);

        if (alreadyOwned)
        {
            WeaponData instanceToEquip = ownedWeaponInstances[weaponData.weaponType];
            playerShooting.EquipWeapon(instanceToEquip);
            Debug.Log("Has equipado " + instanceToEquip.weaponName + ".");
        }
        else
        {
            WeaponData newWeaponInstance = Instantiate(weaponData);

            if (weaponData.price <= 0)
            {
                ownedWeaponInstances.Add(newWeaponInstance.weaponType, newWeaponInstance);
                playerShooting.EquipWeapon(newWeaponInstance);
                Debug.Log("Has obtenido " + weaponData.weaponName + " (arma gratuita).");
            }
            else
            {
                bool paid = ScoreManager.Instance.TrySpendPoints((int)weaponData.price);
                if (paid)
                {
                    ownedWeaponInstances.Add(newWeaponInstance.weaponType, newWeaponInstance);
                    playerShooting.EquipWeapon(newWeaponInstance);
                    Debug.Log("Has comprado y equipado " + newWeaponInstance.weaponName + " por " + weaponData.price + " puntos.");
                }
                else
                {
                    Destroy(newWeaponInstance);
                    Debug.Log("No tienes suficientes puntos para comprar esta arma.");
                }
            }
        }
    }
}
