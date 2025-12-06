using UnityEngine;
using TMPro;

public class WeaponUpgradeShop : MonoBehaviour
{
    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI del mensaje")]
    public TextMeshProUGUI interactionText;

    [Header("Valores de mejora (Mecánicas Base)")]
    [SerializeField] private int shotgunUpgradePellets = 8;
    [SerializeField] private float rifleUpgradeFireRate = 0.08f;
    [SerializeField] private float pistolBurstFireRate = 0.1f;
    [SerializeField] private int sniperUpgradePenetration = 3;
   
    [Header("Valores de mejora (Nuevas Clases)")]
    [Tooltip("Balas que recupera el SMG por muerte al mejorarse")]
    [SerializeField] private int smgUpgradeVampireAmmo = 3;
    [Tooltip("Multiplicador de daño máximo para la LMG al calentarse")]
    [SerializeField] private float lmgUpgradeMaxHeatMult = 2.5f;

    [Header("Valores de mejora (Munición: Cargador / Total)")]
    [SerializeField] private int pistolUpgradedMag = 20;
    [SerializeField] private int pistolUpgradedMaxAmmo = 120;

    [SerializeField] private int rifleUpgradedMag = 45;
    [SerializeField] private int rifleUpgradedMaxAmmo = 270;

    [SerializeField] private int shotgunUpgradedMag = 12;
    [SerializeField] private int shotgunUpgradedMaxAmmo = 64;

    [SerializeField] private int sniperUpgradedMag = 10;
    [SerializeField] private int sniperUpgradedMaxAmmo = 50;

    // --- NUEVO: Munición para SMG y LMG ---
    [Header("Munición Mejorada (Nuevas Clases)")]
    [SerializeField] private int smgUpgradedMag = 50;
    [SerializeField] private int smgUpgradedMaxAmmo = 300;

    [SerializeField] private int lmgUpgradedMag = 100;
    [SerializeField] private int lmgUpgradedMaxAmmo = 400;
    // --------------------------------------

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
                weapon.magCapacity = pistolUpgradedMag;
                weapon.maxAmmo = pistolUpgradedMaxAmmo;
                break;

            case WeaponType.Rifle:
                weapon.isUpgraded = true;
                weapon.fireRate = rifleUpgradeFireRate;
                weapon.magCapacity = rifleUpgradedMag;
                weapon.maxAmmo = rifleUpgradedMaxAmmo;
                break;

            case WeaponType.Shotgun:
                weapon.isUpgraded = true;
                weapon.pelletCount = shotgunUpgradePellets;
                weapon.magCapacity = shotgunUpgradedMag;
                weapon.maxAmmo = shotgunUpgradedMaxAmmo;
                break;

            case WeaponType.Sniper:
                weapon.isUpgraded = true;
                weapon.penetrationCount = sniperUpgradePenetration;
                weapon.magCapacity = sniperUpgradedMag;
                weapon.maxAmmo = sniperUpgradedMaxAmmo;
                break;

            case WeaponType.SMG:
                weapon.isUpgraded = true;
                weapon.vampireAmmoRestore = smgUpgradeVampireAmmo;
                weapon.magCapacity = smgUpgradedMag;
                weapon.maxAmmo = smgUpgradedMaxAmmo;
                Debug.Log("SMG Mejorada: Vampirismo activado.");
                break;

            case WeaponType.LMG:
                weapon.isUpgraded = true;
                // Activamos el daño por calor
                weapon.maxHeatDamageMultiplier = lmgUpgradeMaxHeatMult;
                weapon.magCapacity = lmgUpgradedMag;
                weapon.maxAmmo = lmgUpgradedMaxAmmo;
                Debug.Log("LMG Mejorada: Daño progresivo activado.");
                break;
                // ----------------------------------------------
        }

        // Rellenar la munición cuando se mejoran las armas
        playerShooting.ForceCurrentWeaponAmmoToFull();

        interactionText.text = $"{weapon.weaponName} mejorada correctamente!";
    }
}