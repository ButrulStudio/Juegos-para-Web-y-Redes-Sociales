using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias Generales")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CameraController cameraController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image crosshairImage;

    private RectTransform crosshairRectTransform;
    private Color defaultAmmoColor;

    [Header("Estado del Arma")]
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;
    private bool isBursting = false;

    // Variables para el retroceso visual (Kickback)
    private Vector3 weaponInitialLocalPos;
    private Vector3 weaponCurrentOffset;

    // === MUNICIÓN ===
    private int currentAmmoInMag;
    private int totalAmmo;
    private bool isReloading = false;

    // Caché de munición para armas no equipadas
    private Dictionary<WeaponType, int> ammoInMagCache = new Dictionary<WeaponType, int>();
    private Dictionary<WeaponType, int> totalAmmoCache = new Dictionary<WeaponType, int>();

    // === MULTIPLICADORES DE POWER-UP ===
    [HideInInspector] public float reloadTimeMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;

    [Header("Efectos Visuales (Muzzle Flash)")]
    private Light muzzleLight;
    [SerializeField] private float flashDuration = 0.05f;

    [Header("Efectos de Impacto (Partículas)")]
    [Tooltip("Sistema de partículas para sangre (Tag Zombie).")]
    [SerializeField] private GameObject bloodParticlePrefab;

    [Tooltip("Sistema de partículas para polvo/tierra (Tag Mapa).")]
    [SerializeField] private GameObject dustParticlePrefab;

    [Header("Decals (Agujeros de Bala PNG)")]
    [Tooltip("Prefab BASE que debe tener un componente SpriteRenderer (sin sprite asignado).")]
    [SerializeField] private GameObject bulletHoleBasePrefab;

    [Tooltip("Sprite único para impactos en el escenario (Tag: Mapa).")]
    [SerializeField] private Sprite mapBulletHoleSprite;

    [Tooltip("Colección de Sprites para impactos en enemigos (Tag: Zombie).")]
    [SerializeField] private Sprite[] zombieBulletHoleSprites;

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

    //====== AUDIO =======
    [SerializeField] private AudioSource audioSource;

    void Awake()
    {
        if (crosshairImage != null)
            crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();

        if (ammoText != null) defaultAmmoColor = ammoText.color;

        if (weaponHolder != null)
        {
            weaponInitialLocalPos = weaponHolder.localPosition;
            weaponInitialLocalRot = weaponHolder.localEulerAngles;
        }
    }

    void Start()
    {
        UpdateAmmoUI();
        UpdateCrosshair();
        if (playerCamera != null) playerCamera.fieldOfView = defaultFOV;
    }

    // Llamado por GameManager al iniciar
    public void InitializeNewGame(WeaponData weaponToEquip)
    {
        if (weaponToEquip != null)
        {
            currentWeapon = Instantiate(weaponToEquip);
            EquipWeapon(currentWeapon);
            WeaponStore.RegisterStartingWeapon(currentWeapon);
        }
    }

    void Update()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver) return;
        if (isReloading) return;

        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        // Recuperación del retroceso visual del arma
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

    // =================================================================================
    //                                  APUNTADO (ADS)
    // =================================================================================

    void HandleAiming()
    {
        if (currentWeapon == null || !currentWeapon.canAim)
        {
            if (isAiming) StopAiming();
            return;
        }

        if (Input.GetButtonDown("Fire2")) isAiming = true;
        if (Input.GetButtonUp("Fire2")) isAiming = false;

        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);

        // Lógica especial para Sniper (Overlay 2D)
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
        if (playerCamera != null) playerCamera.fieldOfView = defaultFOV;
        if (weaponHiddenForScope)
        {
            if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
            weaponHiddenForScope = false;
        }
        UpdateCrosshair();
    }

    // =================================================================================
    //                                  RECARGA
    // =================================================================================

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

        // Animación de bajada
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * reloadAnimSpeed;
            weaponHolder.localRotation = Quaternion.Lerp(Quaternion.Euler(weaponInitialLocalRot), Quaternion.Euler(reloadRotation), t);
            yield return null;
        }

        int neededAmmo = currentWeapon.magCapacity - currentAmmoInMag;
        int ammoToLoad = Mathf.Min(neededAmmo, totalAmmo);
        float waitTime = Mathf.Max(0, reloadTime - animTime * 2f);

        // Lógica Escopeta (bala a bala) vs Normal
        if (currentWeapon.weaponType == WeaponType.Shotgun && ammoToLoad > 0)
        {
            float timePerBullet = (waitTime > 0 && ammoToLoad > 0) ? waitTime / ammoToLoad : 0;
            for (int i = 0; i < ammoToLoad; i++)
            {
                if (timePerBullet > 0) yield return new WaitForSeconds(timePerBullet);
                currentAmmoInMag++;
                totalAmmo--;
                UpdateAmmoUI();
            }
        }
        else
        {
            if (waitTime > 0) yield return new WaitForSeconds(waitTime);
            currentAmmoInMag += ammoToLoad;
            totalAmmo -= ammoToLoad;
        }

        // Animación de subida
        t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime * reloadAnimSpeed;
            weaponHolder.localRotation = Quaternion.Lerp(Quaternion.Euler(reloadRotation), Quaternion.Euler(weaponInitialLocalRot), t);
            yield return null;
        }

        isReloading = false;
        if (currentWeapon.weaponType != WeaponType.Shotgun) UpdateAmmoUI();
    }

    // =================================================================================
    //                                  DISPARO
    // =================================================================================

    void HandleShooting()
    {
        if (currentWeapon == null) return;

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    if (currentWeapon.isUpgraded) StartCoroutine(BurstFire()); else Shoot();
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
        if (currentAmmoInMag <= 0) { HandleEmptyClip(); return; }
        FireBaseLogic();

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
            if (currentAmmoInMag <= 0) { HandleEmptyClip(); break; }
            FireBaseLogic();

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
        if (currentAmmoInMag <= 0) { HandleEmptyClip(); return; }
        FireBaseLogic();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage * damageMultiplier);

        ApplyRecoil();
    }

    IEnumerator ShootShotgunCoroutine()
    {
        if (currentAmmoInMag <= 0) { HandleEmptyClip(); yield break; }
        FireBaseLogic();

        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            Vector3 direction = playerCamera.transform.forward;
            // Dispersión aleatoria
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
        if (currentAmmoInMag <= 0) { HandleEmptyClip(); yield break; }
        FireBaseLogic();
        ApplyRecoil();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        // Penetración: RaycastAll
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

                if (hitbox != null) zombieHealth = hitbox.zombieController;
                else zombieHealth = hit.collider.GetComponent<ZombieController>();

                if (zombieHealth != null)
                {
                    if (!alreadyDamaged.Contains(zombieHealth))
                    {
                        HandleHit(hit, currentWeapon.damage * damageMultiplier);
                        alreadyDamaged.Add(zombieHealth);
                        targetsHit++;
                        if (targetsHit >= currentWeapon.penetrationCount) break;
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

    void FireBaseLogic()
    {
        PlaySound(currentWeapon.shootSound);
        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());
    }

    void HandleEmptyClip()
    {
        PlaySound(currentWeapon.emptyClipSound);

        if (totalAmmo > 0)
        {

            if (!isReloading)
            {
                StartCoroutine(ReloadCoroutine());
            }
        }
        else
        {
            if (ammoText != null)
            {
                ammoText.text = "SIN MUNICIÓN";
                ammoText.color = Color.red;
            }
        }
    }

    // =================================================================================
    //                GESTIÓN DE IMPACTOS, DECALS Y PARTÍCULAS
    // =================================================================================

    void HandleHit(RaycastHit hit, float damage)
    {
        // 1. APLICAR DAÑO
        ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
        ZombieController zombieHealth = null;

        if (hitbox != null)
        {
            zombieHealth = hitbox.zombieController;
            if (zombieHealth != null) zombieHealth.TakeDamage(damage, hitbox.hitboxType);
        }
        else
        {
            zombieHealth = hit.collider.GetComponent<ZombieController>();
            if (zombieHealth != null) zombieHealth.TakeDamage(damage);
        }

        // SELECCIONAR EFECTOS VISUALES SEGÚN TAG
        Sprite decalSprite = null;
        GameObject particlePrefab = null;

        if (hit.collider.CompareTag("Zombie"))
        {
            // Partículas de Sangre
            particlePrefab = bloodParticlePrefab;

            // Sprite de Sangre/Herida aleatorio. Esto en teoria se deberia de poder quitar porque se ha puesto un sistema de particulas
            if (zombieBulletHoleSprites != null && zombieBulletHoleSprites.Length > 0)
            {
                int rnd = Random.Range(0, zombieBulletHoleSprites.Length);
                decalSprite = zombieBulletHoleSprites[rnd];
            }
        }
        else if (hit.collider.CompareTag("Mapa"))
        {
            // Partículas de Polvo/Tierra
            particlePrefab = dustParticlePrefab;

            // Sprite de Agujero en pared
            decalSprite = mapBulletHoleSprite;
        }

        // INSTANCIAR EFECTOS

        // Partículas (Splash)
        if (particlePrefab != null)
        {
            SpawnParticleEffect(hit, particlePrefab);
        }

        // Decal (Sprite pegado)
        if (decalSprite != null && bulletHoleBasePrefab != null)
        {
            SpawnDecal(hit, decalSprite);
        }
        // Fallback (Sistema antiguo de WeaponData si no se configuró el nuevo)
        else if (hit.collider.CompareTag("Zombie") && currentWeapon.bulletHolePrefab != null && bulletHoleBasePrefab == null)
        {
            SpawnLegacyDecal(hit);
        }
    }

    private void SpawnParticleEffect(RaycastHit hit, GameObject prefab)
    {
        // LookRotation(hit.normal) orienta las partículas hacia afuera de la superficie
        GameObject effect = Instantiate(prefab, hit.point + (hit.normal * 0.02f), Quaternion.LookRotation(hit.normal));
        Destroy(effect, 2f);
    }

    private void SpawnDecal(RaycastHit hit, Sprite sprite)
    {
        Quaternion hitRotation = Quaternion.LookRotation(hit.normal);
        // Offset de 0.01f para evitar z-fighting
        GameObject hole = Instantiate(bulletHoleBasePrefab, hit.point + (hit.normal * 0.01f), hitRotation);

        SpriteRenderer sr = hole.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
        else Debug.LogWarning("El 'bulletHoleBasePrefab' no tiene SpriteRenderer.");

        hole.transform.SetParent(hit.collider.transform);
        Destroy(hole, 5f);
    }

    private void SpawnLegacyDecal(RaycastHit hit)
    {
        Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0);
        GameObject hole = Instantiate(currentWeapon.bulletHolePrefab, hit.point + hit.normal * 0.001f, hitRotation);
        hole.transform.SetParent(hit.collider.transform);
        hole.transform.Rotate(0, 0, Random.Range(0, 360));
        Collider holeCollider = hole.GetComponent<Collider>();
        if (holeCollider != null) holeCollider.enabled = false;
        Destroy(hole, 5f);
    }

    // =================================================================================
    //                          INVENTARIO Y UTILIDADES
    // =================================================================================

    public void EquipWeapon(WeaponData weaponData)
    {
        // Guardar estado del arma anterior
        if (currentWeapon != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        // Destruir modelo anterior
        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        currentWeapon = weaponData;
        StopAiming();

        if (currentWeapon == null)
        {
            if (crosshairImage != null) crosshairImage.enabled = false;
            if (ammoText != null) ammoText.text = "";
            return;
        }

        // Instanciar nuevo modelo
        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;

            // Buscar la luz del fogonazo
            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>();
            muzzleLight = newMuzzleLight;
        }

        // Recuperar o inicializar munición
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

    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        if (currentWeapon != null)
        {
            ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";
            ammoText.color = (currentAmmoInMag == 0 && totalAmmo == 0) ? Color.red : defaultAmmoColor;
        }
        else
        {
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
        else crosshairImage.enabled = false;
    }

    private IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleLight == null) yield break;
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        muzzleLight.enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    // --- Métodos de Guardado/Carga ---

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
            dataList.Add(new WeaponAmmoData { weaponType = key, currentMagAmmo = ammoInMagCache[key], currentTotalAmmo = totalAmmoCache[key] });
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
        else return false;

        int maxMag = weaponData.magCapacity;
        int maxTotal = weaponData.maxAmmo - weaponData.magCapacity;
        return currentMag >= maxMag && currentTotal >= maxTotal;
    }

    public WeaponType GetEquippedWeaponType()
    {
        if (currentWeapon != null) return currentWeapon.weaponType;
        return (WeaponType)(-1);
    }
}