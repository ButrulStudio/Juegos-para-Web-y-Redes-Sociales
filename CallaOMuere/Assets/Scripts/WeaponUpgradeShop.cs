using UnityEngine;
using TMPro;

public class WeaponUpgradeShop : MonoBehaviour
{
    [Header("Detección e interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;

    [Header("UI del mensaje")]
    public TextMeshProUGUI interactionText;

    [Header("Valores de mejora")]
    [SerializeField] private int shotgunUpgradePellets = 8;
    [SerializeField] private float rifleUpgradeFireRate = 0.05f;
    [SerializeField] private float pistolBurstFireRate = 0.1f;

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

        switch (weapon.weaponType)
        {
            case WeaponType.Pistol:
                weapon.isUpgraded = true;
                weapon.fireRate = pistolBurstFireRate;
                Debug.Log("Pistola mejorada: ahora dispara ráfagas de 3 balas.");
                break;

            case WeaponType.Rifle:
                weapon.isUpgraded = true;
                weapon.fireRate = rifleUpgradeFireRate;
                Debug.Log("Rifle mejorado: ahora dispara más rápido.");
                break;

            case WeaponType.Shotgun:
                weapon.isUpgraded = true;
                weapon.pelletCount = shotgunUpgradePellets;
                Debug.Log("Escopeta mejorada: ahora dispara más perdigones.");
                break;
        }

        interactionText.text = $"{weapon.weaponName} mejorada correctamente!";
    }
}
