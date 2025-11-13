using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CameraController cameraController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;     // Texto del HUD de munición
    [SerializeField] private Image crosshairImage;         // Imagen de la mira (crosshair)

    [Header("Arma actual")]
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;

    private bool isBursting = false;

    private Vector3 weaponInitialLocalPos;
    private Vector3 weaponCurrentOffset;

    // === MUNICIÓN ===
    private int currentAmmoInMag;
    private int totalAmmo;
    private bool isReloading = false;
    private Dictionary<WeaponType, int> ammoInMagCache = new Dictionary<WeaponType, int>();
    private Dictionary<WeaponType, int> totalAmmoCache = new Dictionary<WeaponType, int>();

    // === MULTIPLICADORES DE POWER-UP ===
    [HideInInspector] public float reloadTimeMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;

    [Header("Muzzle Flash")]
    [Tooltip("Arrastra aquí el componente Light (Point Light) del cañón del arma equipada.")]
    private Light muzzleLight;

    [Tooltip("Duración en segundos del fogonazo. 0.05 es un buen valor para empezar.")]
    [SerializeField] private float flashDuration = 0.05f;


    void Start()
    {
        if (weaponHolder != null) weaponInitialLocalPos = weaponHolder.localPosition;

        // Si se va a cargar una partida, no hacer nada aquí.
        // SaveLoadManager lo gestionará todo.
        if (SaveLoadManager.ShouldLoadGame)
        {
            return;
        }

        // --- LÓGICA DE PARTIDA NUEVA ---
        if (currentWeapon != null)
        {
            // 1. Instanciamos el arma inicial (para que se pueda mejorar)
            currentWeapon = Instantiate(currentWeapon);

            // 2. La equipamos
            EquipWeapon(currentWeapon);

            // 3. Le decimos a la Tienda que esta arma ya nos pertenece.
            WeaponStore.RegisterStartingWeapon(currentWeapon);
        }

        UpdateAmmoUI();
        UpdateCrosshair();
    }

    void Update()
    {
        if (isReloading) return;

        HandleShooting();
        HandleReloadInput();

        // Movimiento de retorno del arma
        if (weaponHolder != null && currentWeapon != null) // Añadida comprobación de currentWeapon
        {
            weaponCurrentOffset = Vector3.Lerp(
                weaponCurrentOffset,
                Vector3.zero,
                Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed
            );
            weaponHolder.localPosition = weaponInitialLocalPos + weaponCurrentOffset;
        }
    }

    // === RECARGA ===
    void HandleReloadInput()
    {
        if (currentWeapon == null) return; // No se puede recargar si no hay arma
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        if (ammoText != null) ammoText.text = "Recargando...";

        yield return new WaitForSeconds(currentWeapon.reloadTime * reloadTimeMultiplier);

        int neededAmmo = currentWeapon.magCapacity - currentAmmoInMag;
        int ammoToLoad = Mathf.Min(neededAmmo, totalAmmo);

        currentAmmoInMag += ammoToLoad;
        totalAmmo -= ammoToLoad;

        isReloading = false;
        UpdateAmmoUI();
    }

    // === DISPARO PRINCIPAL ===
    void HandleShooting()
    {
        if (currentWeapon == null) return;

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;

                    if (currentWeapon.isUpgraded)
                        StartCoroutine(BurstFire());
                    else
                        Shoot();
                }
                break;

            case WeaponType.Rifle:
                if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    ShootRifle();
                }
                break;

            case WeaponType.Shotgun:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    ShootShotgun();
                }
                break;
        }
    }

    // === DISPARO PISTOLA ===
    void Shoot()
    {
        if (currentAmmoInMag <= 0)
        {
            if (ammoText != null) ammoText.text = "R para recargar";
            return;
        }

        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage * damageMultiplier);

        ApplyRecoil();
    }

    // === RAFAGA (PISTOLA MEJORADA) ===
    private IEnumerator BurstFire()
    {
        if (isBursting) yield break;
        isBursting = true;

        int burstCount = 3;
        for (int i = 0; i < burstCount; i++)
        {
            if (currentAmmoInMag <= 0) break;
            currentAmmoInMag--;
            UpdateAmmoUI();
            StartCoroutine(MuzzleFlashRoutine());

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
                HandleHit(hit, currentWeapon.damage * damageMultiplier);

            ApplyRecoil();
            yield return new WaitForSeconds(currentWeapon.fireRate);
        }

        yield return new WaitForSeconds(0.1f);
        isBursting = false;
    }

    // === DISPARO RIFLE ===
    void ShootRifle()
    {
        if (currentAmmoInMag <= 0)
        {
            if (ammoText != null) ammoText.text = "R para recargar";
            return;
        }

        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage * damageMultiplier);

        ApplyRecoil();
    }

    // === DISPARO ESCOPETA ===
    void ShootShotgun()
    {
        if (currentAmmoInMag <= 0)
        {
            if (ammoText != null) ammoText.text = "R para recargar";
            return;
        }

        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());

        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            Vector3 direction = playerCamera.transform.forward;
            direction = Quaternion.Euler(
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                0
            ) * direction;

            Ray ray = new Ray(playerCamera.transform.position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
                HandleHit(hit, currentWeapon.damage * damageMultiplier);
        }

        ApplyRecoil();
    }

    // === GESTIÓN DE IMPACTOS ===
    void HandleHit(RaycastHit hit, float damage)
    {
        ZombieController zombieHealth = hit.collider.GetComponent<ZombieController>();

        if (zombieHealth != null)
        {
            zombieHealth.TakeDamage(damage);
            float remainingHealth = zombieHealth.GetHP();
            Debug.Log($"El Zombie {hit.collider.name} ha recibido {damage} de daño y le quedan {remainingHealth:F1} de vida.");
        }

        if (hit.collider.CompareTag("Zombie") && currentWeapon.bulletHolePrefab != null)
        {
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0);
            GameObject hole = Instantiate(currentWeapon.bulletHolePrefab,
                                        hit.point + hit.normal * 0.001f,
                                        hitRotation);

            hole.transform.SetParent(hit.transform);
            hole.transform.Rotate(0, 0, Random.Range(0, 360));

            Collider holeCollider = hole.GetComponent<Collider>();
            if (holeCollider != null) holeCollider.enabled = false;

            Destroy(hole, 5f);
        }
    }

    // === CAMBIO DE ARMA ===
    public void EquipWeapon(WeaponData weaponData)
    {
        // Guardar munición del arma actual (si hay una)
        if (currentWeapon != null && currentWeaponModel != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeapon = weaponData;

        if (currentWeapon == null)
        {
            if (crosshairImage != null) crosshairImage.enabled = false;
            if (ammoText != null) ammoText.text = "";
            return;
        }

        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>();
            muzzleLight = newMuzzleLight;
            if (muzzleLight == null) Debug.LogWarning($"El arma {weaponData.weaponName} no tiene un componente Light.");
        }

        // Cargar munición en lugar de resetear
        if (ammoInMagCache.ContainsKey(currentWeapon.weaponType))
        {
            currentAmmoInMag = ammoInMagCache[currentWeapon.weaponType];
            totalAmmo = totalAmmoCache[currentWeapon.weaponType];
        }
        else
        {
            // Primera vez que la cogemos
            currentAmmoInMag = currentWeapon.magCapacity;
            totalAmmo = currentWeapon.maxAmmo - currentAmmoInMag;
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        isReloading = false;
        UpdateAmmoUI();
        UpdateCrosshair();
    }

    // === RECOIL ===
    private void ApplyRecoil()
    {
        if (cameraController != null)
        {
            float vertical = Random.Range(currentWeapon.recoilVerticalMin, currentWeapon.recoilVerticalMax);
            float horizontal = Random.Range(currentWeapon.recoilHorizontalMin, currentWeapon.recoilHorizontalMax);
            cameraController.AddRecoil(vertical, horizontal);
        }

        if (weaponHolder != null)
        {
            weaponCurrentOffset = new Vector3(0, 0, -currentWeapon.weaponKickbackDistance);
        }
    }

    // === ACTUALIZACIÓN DE HUD ===
    private void UpdateAmmoUI()
    {
        if (ammoText != null && currentWeapon != null)
            ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";
    }

    private void UpdateCrosshair()
    {
        if (crosshairImage == null) return;
        if (currentWeapon != null && currentWeapon.crosshairIcon != null)
        {
            crosshairImage.sprite = currentWeapon.crosshairIcon;
            crosshairImage.enabled = true;
        }
        else
        {
            crosshairImage.enabled = false;
        }
    }

    // === EFECTO LUMINOSO DE FOGONAZO ===
    private IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleLight == null)
        {
            yield break;
        }
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        muzzleLight.enabled = false;
    }

    public List<WeaponAmmoData> GetAmmoData()
    {
        if (currentWeapon != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        List<WeaponAmmoData> dataList = new List<WeaponAmmoData>();
        foreach (var key in ammoInMagCache.Keys)
        {
            dataList.Add(new WeaponAmmoData
            {
                weaponType = key,
                currentMagAmmo = ammoInMagCache[key],
                currentTotalAmmo = totalAmmoCache[key]
            });
        }
        return dataList;
    }

    public void LoadAmmoData(List<WeaponAmmoData> dataList)
    {
        ammoInMagCache.Clear();
        totalAmmoCache.Clear();

        if (dataList == null) return;

        foreach (var data in dataList)
        {
            ammoInMagCache[data.weaponType] = data.currentMagAmmo;
            totalAmmoCache[data.weaponType] = data.currentTotalAmmo;
        }
        Debug.Log("Datos de munición cargados en el caché de PlayerShooting.");
    }

    public void ForceCurrentWeaponAmmoToFull()
    {
        if (currentWeapon == null) return;

        currentAmmoInMag = currentWeapon.magCapacity;
        totalAmmo = currentWeapon.maxAmmo - currentWeapon.magCapacity;

        ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
        totalAmmoCache[currentWeapon.weaponType] = totalAmmo;

        UpdateAmmoUI();
        Debug.Log($"Munición de {currentWeapon.weaponName} restaurada al comprar.");
    }

    // Comprueba si el jugador tiene la munición al máximo para un arma específica.

    public bool IsAmmoFull(WeaponData weaponData)
    {
        if (weaponData == null) return true;

        int currentMag = 0;
        int currentTotal = 0;

        // Comprueba si el arma es la que lleva en la mano
        if (currentWeapon != null && weaponData.weaponType == currentWeapon.weaponType)
        {
            currentMag = currentAmmoInMag;
            currentTotal = totalAmmo;
        }
        // Si no, comprueba si la tiene en el caché (en el bolsillo)
        else if (ammoInMagCache.ContainsKey(weaponData.weaponType))
        {
            currentMag = ammoInMagCache[weaponData.weaponType];
            currentTotal = totalAmmoCache[weaponData.weaponType];
        }
        else
        {
            // Si no la tiene ni equipada ni en el caché, es que no la ha comprado.
            return false;
        }

        // Definir los máximos
        int maxMag = weaponData.magCapacity;
        int maxTotal = weaponData.maxAmmo - weaponData.magCapacity;

        bool isFull = currentMag >= maxMag && currentTotal >= maxTotal;
        return isFull;
    }

    //Devuelve el tipo del arma que el jugador tiene equipada.
    // Si no tiene ninguna, devuelve -1

    public WeaponType GetEquippedWeaponType()
    {
        if (currentWeapon != null)
        {
            return currentWeapon.weaponType;
        }
        // Devuelve un valor que no coincida con ningún arma
        return (WeaponType)(-1);
    }
}