using UnityEngine;
using TMPro;

public class WeaponUpgradeShop : MonoBehaviour
{
    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public TextMeshProUGUI interactionText;

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    void Start()
    {
        playerCamera = Camera.main;
        playerShooting = FindAnyObjectByType<PlayerShooting>();
        if (interactionText != null) interactionText.gameObject.SetActive(false);
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
                if (Input.GetKeyDown(interactionKey)) TryUpgrade();
                return;
            }
        }

        if (playerLooking)
        {
            playerLooking = false;
            if (interactionText != null) interactionText.gameObject.SetActive(false);
        }
    }

    void ShowInteractionMessage()
    {
        if (interactionText == null || playerShooting == null || playerShooting.currentWeapon == null) return;

        WeaponData weapon = playerShooting.currentWeapon;

        // Si el arma no se puede mejorar (Lanzallamas), ocultamos el texto
        if (!weapon.canBeUpgraded)
        {
            interactionText.gameObject.SetActive(false);
            return;
        }

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

        if (!weapon.canBeUpgraded) return;
        if (weapon.isUpgraded) return;

        if (!ScoreManager.Instance.TrySpendPoints(weapon.upgradeCost))
        {
            interactionText.text = "No tienes suficientes puntos";
            return;
        }

        // --- APLICAR MEJORA ESPECÍFICA POR ARMA ---
        weapon.isUpgraded = true;

        switch (weapon.weaponType)
        {
            // --- PISTOLAS ---
            case WeaponType.Glock:
                // Se convierte en Glock-18 (Automática y cargador ampliado)
                weapon.magCapacity = 33;
                weapon.maxAmmo = 200;
                weapon.fireRate = 0.09f;
                break;

            // --- ESCOPETAS ---
            case WeaponType.Remington:
                // Mejora táctica: Recarga rápida y más perdigones
                weapon.magCapacity = 12;
                weapon.maxAmmo = 64;
                weapon.reloadTime = 0.5f;
                weapon.pelletCount = 10;
                break;

            case WeaponType.HuntingShotgun:
                // "Super Shotgun": 4 cartuchos y daño masivo + empuje
                weapon.magCapacity = 4;
                weapon.maxAmmo = 40;
                weapon.damage = 80;
                weapon.causesKnockback = true;
                weapon.knockbackForce = 5f;
                break;

            case WeaponType.AA12:
                // Tambor grande y fuego automático
                weapon.magCapacity = 20;
                weapon.maxAmmo = 120;
                weapon.fireRate = 0.15f;
                break;

            // --- RIFLES ---
            case WeaponType.AK47:
                weapon.magCapacity = 60;
                weapon.maxAmmo = 360;
                weapon.damage = 70;
                weapon.fireRate = 0.1f;
                break;

            case WeaponType.M4A1:
                weapon.magCapacity = 60;
                weapon.maxAmmo = 360;
                weapon.fireRate = 0.07f; // Cadencia extrema
                weapon.recoilVerticalMax = 0.5f; // Sin retroceso
                break;

            case WeaponType.MTAR:
                weapon.magCapacity = 120;
                weapon.maxAmmo = 480;
                weapon.reloadTime = 2.0f;
                weapon.damage = 55;
                break;

            case WeaponType.Fal:
                // Se vuelve automática
                weapon.magCapacity = 30;
                weapon.maxAmmo = 240;
                weapon.fireRate = 0.1f;
                break;

            case WeaponType.M14:
                weapon.magCapacity = 25;
                weapon.maxAmmo = 150;
                weapon.damage = 150;
                break;

            // --- SMGS ---
            case WeaponType.UZI:
                weapon.magCapacity = 64;
                weapon.maxAmmo = 320;
                weapon.vampireAmmoRestore = 3;
                break;

            case WeaponType.Mp7:
                weapon.magCapacity = 80;
                weapon.maxAmmo = 400;
                weapon.reloadTime = 0.8f;
                weapon.vampireAmmoRestore = 5;
                break;

            // --- LMG ---
            case WeaponType.RPD:
                weapon.magCapacity = 150;
                weapon.maxAmmo = 600;
                weapon.maxHeatDamageMultiplier = 3.0f; 
                break;

            // --- SNIPERS ---
            case WeaponType.L11:
                weapon.magCapacity = 10;
                weapon.maxAmmo = 50;
                weapon.damage = 2000;
                weapon.penetrationCount = 10;
                break;

            case WeaponType.SVU:
                weapon.magCapacity = 20;
                weapon.maxAmmo = 100;
                weapon.fireRate = 0.2f;
                weapon.penetrationCount = 3;
                break;
        }

        playerShooting.ForceCurrentWeaponAmmoToFull();
        playerShooting.RefreshCurrentWeapon();

        interactionText.text = $"¡{weapon.weaponName} mejorada!";
    }
}