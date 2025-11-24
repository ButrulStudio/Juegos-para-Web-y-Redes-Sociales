using UnityEngine;
using TMPro;

public class WeaponUpgradeShop : MonoBehaviour
{
    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI del mensaje")]
    public TextMeshProUGUI interactionText;

    [Header("Valores de mejora (Mecánicas)")]
    [SerializeField] private int shotgunUpgradePellets = 8;
    [SerializeField] private float rifleUpgradeFireRate = 0.08f; // Cuidado con poner 1f aqui, seria muy lento. Menor es mas rapido.
    [SerializeField] private float pistolBurstFireRate = 0.1f;
    [SerializeField] private int sniperUpgradePenetration = 3;

    [Header("Valores de mejora (Munición: Cargador / Total)")]
    [Tooltip("Nuevo tamaño del cargador para la Pistola mejorada")]
    [SerializeField] private int pistolUpgradedMag = 20;
    [Tooltip("Nueva munición total para la Pistola mejorada")]
    [SerializeField] private int pistolUpgradedMaxAmmo = 120;

    [Tooltip("Nuevo tamaño del cargador para el Rifle mejorado")]
    [SerializeField] private int rifleUpgradedMag = 45;
    [Tooltip("Nueva munición total para el Rifle mejorado")]
    [SerializeField] private int rifleUpgradedMaxAmmo = 270;

    [Tooltip("Nuevo tamaño del cargador para la Escopeta mejorada")]
    [SerializeField] private int shotgunUpgradedMag = 12;
    [Tooltip("Nueva munición total para la Escopeta mejorada")]
    [SerializeField] private int shotgunUpgradedMaxAmmo = 64;

    [Tooltip("Nuevo tamaño del cargador para el Sniper mejorado")]
    [SerializeField] private int sniperUpgradedMag = 10;
    [Tooltip("Nueva munición total para el Sniper mejorado")]
    [SerializeField] private int sniperUpgradedMaxAmmo = 50;


    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

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
                    TryUpgrade();
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
        if (interactionText == null || playerShooting == null || playerShooting.currentWeapon == null)
            return;

        WeaponData weapon = playerShooting.currentWeapon;
        interactionText.gameObject.SetActive(true);

        if (weapon.isUpgraded)
        {
            interactionText.text = $"Ya tienes mejorada la {weapon.weaponName}";
        }
        else
        {
            interactionText.text = $"Pulsa [{interactionKey}] para mejorar {weapon.weaponName} por {weapon.upgradeCost} puntos";
        }
    }

    void TryUpgrade()
    {
        WeaponData weapon = playerShooting.currentWeapon;

        if (weapon.isUpgraded)
        {
            interactionText.text = $"Ya tienes mejorada la {weapon.weaponName}";
            return;
        }

        if (!ScoreManager.Instance.TrySpendPoints(weapon.upgradeCost))
        {
            interactionText.text = "No tienes suficientes puntos";
            return;
        }

        // Aplicar lógica según el tipo de arma
        switch (weapon.weaponType)
        {
            case WeaponType.Pistol:
                weapon.isUpgraded = true;
                weapon.fireRate = pistolBurstFireRate;

                // Nuevas capacidades
                weapon.magCapacity = pistolUpgradedMag;
                weapon.maxAmmo = pistolUpgradedMaxAmmo;

                Debug.Log("Pistola mejorada: ráfagas y más munición.");
                break;

            case WeaponType.Rifle:
                weapon.isUpgraded = true;
                weapon.fireRate = rifleUpgradeFireRate;

                // Nuevas capacidades
                weapon.magCapacity = rifleUpgradedMag;
                weapon.maxAmmo = rifleUpgradedMaxAmmo;

                Debug.Log($"Rifle mejorado: disparo rápido y más munición.");
                break;

            case WeaponType.Shotgun:
                weapon.isUpgraded = true;
                weapon.pelletCount = shotgunUpgradePellets;

                // Nuevas capacidades
                weapon.magCapacity = shotgunUpgradedMag;
                weapon.maxAmmo = shotgunUpgradedMaxAmmo;

                Debug.Log("Escopeta mejorada: más perdigones y más munición.");
                break;

            case WeaponType.Sniper:
                weapon.isUpgraded = true;
                weapon.penetrationCount = sniperUpgradePenetration;

                // Nuevas capacidades
                weapon.magCapacity = sniperUpgradedMag;
                weapon.maxAmmo = sniperUpgradedMaxAmmo;

                Debug.Log("Sniper mejorado: perforación y más munición.");
                break;
        }

        // Rellenar la munición cuando se mejoran las armas
        playerShooting.ForceCurrentWeaponAmmoToFull();

        interactionText.text = $"{weapon.weaponName} mejorada correctamente!";
    }
}