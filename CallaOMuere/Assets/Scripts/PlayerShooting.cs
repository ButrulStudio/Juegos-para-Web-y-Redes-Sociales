using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CameraController cameraController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image crosshairImage;

    private RectTransform crosshairRectTransform;
    private Color defaultAmmoColor;

    [Header("Arma actual")]
    public WeaponData currentWeapon; // Esto se seteará por código, pero puede estar vacío
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

    // --- APUNTADO ---
    [Header("Apuntado (ADS)")]
    [SerializeField] private float adsSpeed = 10f;
    [SerializeField] private float defaultFOV = 60f;
    private bool isAiming = false;
    private bool weaponHiddenForScope = false;

    [Header("Animación de Recarga")]
    [SerializeField] private Vector3 reloadRotation = new Vector3(35f, 0f, 0f);
    [SerializeField] private float reloadAnimSpeed = 8f;

    private Vector3 weaponInitialLocalRot;

    //======AUDIO=======
    [SerializeField] private AudioSource audioSource;

    // Awake() se ejecuta ANTES que cualquier Start().
    // Aquí es donde debemos coger las referencias.
    void Awake()
    {
        if (crosshairImage != null)
            crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();

        if (ammoText != null)
        {
            // ¡CRÍTICO! Obtenemos el color antes de que LoadGame() pueda estropearlo.
            defaultAmmoColor = ammoText.color;
        }

        if (weaponHolder != null)
        {
            weaponInitialLocalPos = weaponHolder.localPosition;
            weaponInitialLocalRot = weaponHolder.localEulerAngles;
        }
    }

    // Start() ahora solo pone la UI en su estado inicial.
    void Start()
    {
        UpdateAmmoUI();
        UpdateCrosshair();
        playerCamera.fieldOfView = defaultFOV;
    }

    // --- ¡FUNCIÓN MODIFICADA! ---
    // Esto será llamado por GameManager si es una partida nueva.
    // Acepta un arma como parámetro.
    public void InitializeNewGame(WeaponData weaponToEquip)
    {
        // Usa el arma que le ha pasado el GameManager
        if (weaponToEquip != null)
        {
            // Instancia ese asset, lo asigna como arma actual y lo equipa
            currentWeapon = Instantiate(weaponToEquip);
            EquipWeapon(currentWeapon);
            WeaponStore.RegisterStartingWeapon(currentWeapon);
        }
        else
        {
            Debug.LogError("InitializeNewGame fue llamado pero el weaponToEquip era nulo.");
        }
    }

    void Update()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver)
            return;

        if (isReloading) return;

        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        if (weaponHolder != null && currentWeapon != null)
        {
            weaponCurrentOffset = Vector3.Lerp(
                weaponCurrentOffset,
                Vector3.zero,
                Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed
            );
            weaponHolder.localPosition = weaponInitialLocalPos + weaponCurrentOffset;
        }
    }

    // === APUNTADO ===
    void HandleAiming()
    {
        if (currentWeapon == null || !currentWeapon.canAim)
        {
            if (isAiming) StopAiming();
            return;
        }

        if (Input.GetButtonDown("Fire2"))
        {
            isAiming = true;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            isAiming = false;
        }

        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);

        if (isAiming && currentWeapon.sniperScopeSprite != null)
        {
            if (crosshairRectTransform != null)
            {

                crosshairRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                crosshairRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                crosshairRectTransform.pivot = new Vector2(0.5f, 0.5f);
                crosshairRectTransform.anchoredPosition = Vector2.zero;
                crosshairRectTransform.sizeDelta = currentWeapon.aimedCrosshairSize;
            }

            crosshairImage.sprite = currentWeapon.sniperScopeSprite;
            crosshairImage.enabled = true;

            if (!weaponHiddenForScope)
            {
                if (currentWeaponModel != null) currentWeaponModel.SetActive(false);
                weaponHiddenForScope = true;
            }
        }
        else
        {
            if (weaponHiddenForScope)
            {
                if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
                weaponHiddenForScope = false;
            }
            UpdateCrosshair();
        }
    }

    public void StopAiming()
    {
        isAiming = false;
        playerCamera.fieldOfView = defaultFOV;
        if (weaponHiddenForScope)
        {
            if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
            weaponHiddenForScope = false;
        }
        UpdateCrosshair();
    }

    // === RECARGA ===
    void HandleReloadInput()
    {
        if (currentWeapon == null) return;
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        PlaySound(currentWeapon.reloadSound);

        float reloadTime = currentWeapon.reloadTime * reloadTimeMultiplier;
        float animTime = 1f / reloadAnimSpeed;

        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * reloadAnimSpeed;
            weaponHolder.localRotation = Quaternion.Lerp(
                Quaternion.Euler(weaponInitialLocalRot),
                Quaternion.Euler(reloadRotation),
                t
            );
            yield return null;
        }

        int neededAmmo = currentWeapon.magCapacity - currentAmmoInMag;
        int ammoToLoad = Mathf.Min(neededAmmo, totalAmmo);

        float waitTime = Mathf.Max(0, reloadTime - animTime * 2f);

        if (currentWeapon.weaponType == WeaponType.Shotgun && ammoToLoad > 0)
        {
            float timePerBullet = (waitTime > 0 && ammoToLoad > 0) ? waitTime / ammoToLoad : 0;

            for (int i = 0; i < ammoToLoad; i++)
            {
                if (timePerBullet > 0)
                    yield return new WaitForSeconds(timePerBullet);

                currentAmmoInMag++;
                totalAmmo--;
                UpdateAmmoUI();
            }
        }
        else
        {
            if (waitTime > 0)
                yield return new WaitForSeconds(waitTime);

            currentAmmoInMag += ammoToLoad;
            totalAmmo -= ammoToLoad;
        }

        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * reloadAnimSpeed;
            weaponHolder.localRotation = Quaternion.Lerp(
                Quaternion.Euler(reloadRotation),
                Quaternion.Euler(weaponInitialLocalRot),
                t
            );
            yield return null;
        }

        isReloading = false;

        if (currentWeapon.weaponType != WeaponType.Shotgun)
        {
            UpdateAmmoUI();
        }
    }

    // === DISPARO ===
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
                    StartCoroutine(ShootShotgunCoroutine());
                }
                break;
            case WeaponType.Sniper:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    StartCoroutine(ShootSniperCoroutine());
                }
                break;
        }
    }

    void Shoot()
    {
        if (currentAmmoInMag <= 0)
        {
            PlaySound(currentWeapon.emptyClipSound);
            if (totalAmmo > 0 && ammoText != null)
            {
                ammoText.text = "R para recargar";
                ammoText.color = defaultAmmoColor;
            }
            return;
        }

        PlaySound(currentWeapon.shootSound);
        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage * damageMultiplier);

        ApplyRecoil();
    }

    private IEnumerator BurstFire()
    {
        if (isBursting) yield break;
        isBursting = true;

        int burstCount = 3;
        for (int i = 0; i < burstCount; i++)
        {
            if (currentAmmoInMag <= 0)
            {
                PlaySound(currentWeapon.emptyClipSound);
                if (totalAmmo > 0 && ammoText != null)
                {
                    ammoText.text = "R para recargar";
                    ammoText.color = defaultAmmoColor;
                }
                break;
            }

            PlaySound(currentWeapon.shootSound);
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

    void ShootRifle()
    {
        if (currentAmmoInMag <= 0)
        {
            PlaySound(currentWeapon.emptyClipSound);
            if (totalAmmo > 0 && ammoText != null)
            {
                ammoText.text = "R para recargar";
                ammoText.color = defaultAmmoColor;
            }
            return;
        }

        PlaySound(currentWeapon.shootSound);
        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage * damageMultiplier);

        ApplyRecoil();
    }

    IEnumerator ShootShotgunCoroutine()
    {
        if (currentAmmoInMag <= 0)
        {
            PlaySound(currentWeapon.emptyClipSound);
            if (totalAmmo > 0 && ammoText != null)
            {
                ammoText.text = "R para recargar";
                ammoText.color = defaultAmmoColor;
            }
            yield break;
        }

        PlaySound(currentWeapon.shootSound);
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

        if (currentWeapon.pumpActionSound != null)
        {
            yield return new WaitForSeconds(currentWeapon.actionSoundDelay);
            PlaySound(currentWeapon.pumpActionSound);
        }
    }

    IEnumerator ShootSniperCoroutine()
    {
        if (currentAmmoInMag <= 0)
        {
            PlaySound(currentWeapon.emptyClipSound);
            if (totalAmmo > 0 && ammoText != null)
            {
                ammoText.text = "R para recargar";
                ammoText.color = defaultAmmoColor;
            }
            yield break;
        }

        PlaySound(currentWeapon.shootSound);
        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());
        ApplyRecoil();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, currentWeapon.range);

        if (hits.Length > 0)
        {
            var sortedHits = hits.OrderBy(h => h.distance);
            HashSet<ZombieController> alreadyDamaged = new HashSet<ZombieController>();
            int targetsHit = 0;

            foreach (var hit in sortedHits)
            {
                ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
                ZombieController zombieHealth = null;

                if (hitbox != null)
                    zombieHealth = hitbox.zombieController;
                else
                    zombieHealth = hit.collider.GetComponent<ZombieController>();

                if (zombieHealth != null)
                {
                    if (!alreadyDamaged.Contains(zombieHealth))
                    {
                        HandleHit(hit, currentWeapon.damage * damageMultiplier);
                        alreadyDamaged.Add(zombieHealth);
                        targetsHit++;

                        if (targetsHit >= currentWeapon.penetrationCount)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    HandleHit(hit, currentWeapon.damage * damageMultiplier);
                }
            }
        }

        if (currentWeapon.boltActionSound != null)
        {
            yield return new WaitForSeconds(currentWeapon.actionSoundDelay);
            PlaySound(currentWeapon.boltActionSound);
        }
    }

    // === GESTIÓN DE IMPACTOS ===
    void HandleHit(RaycastHit hit, float damage)
    {
        ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
        ZombieController zombieHealth = null;

        if (hitbox != null)
        {
            zombieHealth = hitbox.zombieController;
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage, hitbox.hitboxType);
            }
        }
        else
        {
            zombieHealth = hit.collider.GetComponent<ZombieController>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage);
            }
        }

        if (zombieHealth != null)
        {
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
        if (currentWeapon != null && currentWeaponModel != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeapon = weaponData;

        StopAiming();

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

        if (ammoInMagCache.ContainsKey(currentWeapon.weaponType))
        {
            currentAmmoInMag = ammoInMagCache[currentWeapon.weaponType];
            totalAmmo = totalAmmoCache[currentWeapon.weaponType];
        }
        else
        {
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
        if (ammoText == null) return;

        if (currentWeapon != null)
        {
            ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";

            if (currentAmmoInMag == 0 && totalAmmo == 0)
            {
                ammoText.color = Color.red;
            }
            else
            {
                ammoText.color = defaultAmmoColor; // defaultAmmoColor se coge en Awake()
            }
        }
        else
        {
            // Muestra esto si no hay arma (justo al cargar la escena)
            ammoText.text = "";
            ammoText.color = defaultAmmoColor;
        }
    }

    private void UpdateCrosshair()
    {
        if (crosshairRectTransform == null) return;

        if (currentWeapon != null && currentWeapon.crosshairIcon != null)
        {
            crosshairImage.sprite = currentWeapon.crosshairIcon;
            crosshairRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            crosshairRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            crosshairRectTransform.pivot = new Vector2(0.5f, 0.5f);
            crosshairRectTransform.anchoredPosition = Vector2.zero;
            crosshairRectTransform.sizeDelta = currentWeapon.crosshairSize;
            crosshairImage.enabled = true;
        }
        else
        {
            crosshairImage.enabled = false;
        }
    }

    // === OTROS MÉTODOS ===
    private IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleLight == null) yield break;
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
    }

    public bool IsAmmoFull(WeaponData weaponData)
    {
        if (weaponData == null) return true;

        int currentMag = 0;
        int currentTotal = 0;

        if (currentWeapon != null && weaponData.weaponType == currentWeapon.weaponType)
        {
            currentMag = currentAmmoInMag;
            currentTotal = totalAmmo;
        }
        else if (ammoInMagCache.ContainsKey(weaponData.weaponType))
        {
            currentMag = ammoInMagCache[weaponData.weaponType];
            currentTotal = totalAmmoCache[weaponData.weaponType];
        }
        else
        {
            return false;
        }

        int maxMag = weaponData.magCapacity;
        int maxTotal = weaponData.maxAmmo - weaponData.magCapacity;

        bool isFull = currentMag >= maxMag && currentTotal >= maxTotal;
        return isFull;
    }

    public WeaponType GetEquippedWeaponType()
    {
        if (currentWeapon != null)
        {
            return currentWeapon.weaponType;
        }
        return (WeaponType)(-1);
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}