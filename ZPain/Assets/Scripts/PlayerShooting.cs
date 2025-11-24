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

    [Header("Puntuación")]
    [Tooltip("Puntos que gana el jugador por cada bala que acierta al zombi")]
    public int pointsPerHit = 10;

    private RectTransform crosshairRectTransform;
    private Color defaultAmmoColor;

    [Header("Sistema de Inventario (2 Slots)")]
    // Array para guardar las dos armas.
    private WeaponData[] weaponSlots = new WeaponData[2];
    // Índice para saber qué arma tenemos en la mano (0 o 1).
    private int currentSlotIndex = 0;

    [Header("Estado del Arma Actual")]
    public WeaponData currentWeapon; // Referencia rápida al arma activa
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

    // Caché de munición global
    private Dictionary<WeaponType, int> ammoInMagCache = new Dictionary<WeaponType, int>();
    private Dictionary<WeaponType, int> totalAmmoCache = new Dictionary<WeaponType, int>();

    // === MULTIPLICADORES DE POWER-UP ===
    [HideInInspector] public float reloadTimeMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;

    [Header("Efectos Visuales")]
    private Light muzzleLight;
    [SerializeField] private float flashDuration = 0.05f;

    [Header("Efectos de Impacto")]
    [SerializeField] private GameObject bloodParticlePrefab;
    [SerializeField] private GameObject dustParticlePrefab;

    [Header("Decals")]
    [SerializeField] private GameObject bulletHoleBasePrefab;
    [SerializeField] private Sprite mapBulletHoleSprite;
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

    // Inicializa la partida poniendo el arma inicial en el Slot 0
    public void InitializeNewGame(WeaponData weaponToEquip)
    {
        // Limpiamos los slots por si acaso
        weaponSlots[0] = null;
        weaponSlots[1] = null;
        currentSlotIndex = 0;

        if (weaponToEquip != null)
        {
            // Creamos una copia para no sobrescribir el Asset original
            WeaponData newInstance = Instantiate(weaponToEquip);

            // Equipamos directamente
            EquipWeapon(newInstance);

            // Forzamos munición llena al empezar
            ForceCurrentWeaponAmmoToFull();

            WeaponStore.RegisterStartingWeapon(newInstance);
        }
    }

    void Update()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver) return;

        // Gestión de cambio de arma con la rueda del ratón
        HandleWeaponSwitching();

        if (isReloading) return;

        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        // Recuperación del retroceso visual
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
    //                        SISTEMA DE CAMBIO DE ARMA (NUEVO)
    // =================================================================================

    void HandleWeaponSwitching()
    {
        // No cambiar de arma si estamos recargando, apuntando o disparando ráfaga
        if (isReloading || isAiming || isBursting) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            // Rueda arriba -> Slot 0
            if (currentSlotIndex != 0 && weaponSlots[0] != null)
            {
                SwitchToSlot(0);
            }
        }
        else if (scroll < 0f)
        {
            // Rueda abajo -> Slot 1
            if (currentSlotIndex != 1 && weaponSlots[1] != null)
            {
                SwitchToSlot(1);
            }
        }

        // También puedes añadir teclas numéricas si quieres
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentSlotIndex != 0 && weaponSlots[0] != null) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentSlotIndex != 1 && weaponSlots[1] != null) SwitchToSlot(1);
    }

    private void SwitchToSlot(int newIndex)
    {
        // 1. Guardar el estado del arma actual antes de cambiar
        SaveCurrentAmmoState();

        // 2. Cambiar índice
        currentSlotIndex = newIndex;

        // 3. Cargar visualmente el arma del nuevo slot
        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    // =================================================================================
    //                        SISTEMA DE EQUIPAMIENTO (MODIFICADO)
    // =================================================================================

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return;

        // Antes de nada, guardamos la munición del arma que tenemos en la mano ahora mismo
        SaveCurrentAmmoState();

        // LÓGICA DE HUECOS:
        // Caso A: El Slot 0 está vacío.
        if (weaponSlots[0] == null)
        {
            weaponSlots[0] = newWeapon;
            currentSlotIndex = 0;
        }
        // Caso B: El Slot 0 tiene algo, pero el Slot 1 está vacío.
        else if (weaponSlots[1] == null)
        {
            weaponSlots[1] = newWeapon;
            currentSlotIndex = 1;
        }
        // Caso C: Ambos llenos -> Reemplazamos la que tenemos en la mano.
        else
        {
            // (Opcional: Aquí podrías soltar el arma antigua al suelo)
            weaponSlots[currentSlotIndex] = newWeapon;
        }

        // Finalmente, actualizamos el modelo 3D y la UI
        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    /// <summary>
    /// Se encarga de destruir el modelo viejo, instanciar el nuevo y actualizar UI.
    /// </summary>
    private void RefreshWeaponVisuals(WeaponData weaponData)
    {
        // 1. Limpieza del modelo anterior
        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        // 2. Asignar datos
        currentWeapon = weaponData;
        StopAiming(); // Resetear zoom

        if (currentWeapon == null)
        {
            // Si no hay arma en este slot (raro, pero posible)
            if (crosshairImage != null) crosshairImage.enabled = false;
            if (ammoText != null) ammoText.text = "";
            return;
        }

        // 3. Instanciar nuevo modelo
        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;

            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>();
            muzzleLight = newMuzzleLight;
        }

        // 4. Cargar Munición del caché
        LoadAmmoStateForWeapon(currentWeapon);

        // 5. Resetear estados
        isReloading = false;
        UpdateAmmoUI();
        UpdateCrosshair();
    }

    // --- Helpers de Munición ---

    private void SaveCurrentAmmoState()
    {
        if (currentWeapon != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }
    }

    private void LoadAmmoStateForWeapon(WeaponData weapon)
    {
        if (ammoInMagCache.ContainsKey(weapon.weaponType))
        {
            currentAmmoInMag = ammoInMagCache[weapon.weaponType];
            totalAmmo = totalAmmoCache[weapon.weaponType];
        }
        else
        {
            // Primera vez que cogemos este tipo de arma
            currentAmmoInMag = weapon.magCapacity;
            totalAmmo = weapon.maxAmmo - currentAmmoInMag;

            // Guardar en caché inicial
            ammoInMagCache[weapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[weapon.weaponType] = totalAmmo;
        }
    }

    // =================================================================================
    //               RESTO DE LÓGICA (Disparo, Recarga, Impactos...) - IGUAL
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
            weaponHolder.localRotation = Quaternion.Lerp(Quaternion.Euler(weaponInitialLocalRot), Quaternion.Euler(reloadRotation), t);
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
                else HandleHit(hit, currentWeapon.damage * damageMultiplier);
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
            if (!isReloading) StartCoroutine(ReloadCoroutine());
        }
        else
        {
            if (ammoText != null) { ammoText.text = "SIN MUNICIÓN"; ammoText.color = Color.red; }
        }
    }

    // === IMPACTOS Y EFECTOS ===

    void HandleHit(RaycastHit hit, float damage)
    {
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

        if (zombieHealth != null)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(pointsPerHit);
            }
        }
    }

    private void SpawnParticleEffect(RaycastHit hit, GameObject prefab)
    {
        GameObject effect = Instantiate(prefab, hit.point + (hit.normal * 0.02f), Quaternion.LookRotation(hit.normal));
        Destroy(effect, 2f);
    }

    private void SpawnDecal(RaycastHit hit, Sprite sprite)
    {
        Quaternion hitRotation = Quaternion.LookRotation(hit.normal);
        GameObject hole = Instantiate(bulletHoleBasePrefab, hit.point + (hit.normal * 0.01f), hitRotation);
        SpriteRenderer sr = hole.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
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

    // === UTILIDADES ===

    private void ApplyRecoil()
    {
        if (cameraController != null)
        {
            float vertical = Random.Range(currentWeapon.recoilVerticalMin, currentWeapon.recoilVerticalMax);
            float horizontal = Random.Range(currentWeapon.recoilHorizontalMin, currentWeapon.recoilHorizontalMax);
            cameraController.AddRecoil(vertical, horizontal);
        }
        if (weaponHolder != null) weaponCurrentOffset = new Vector3(0, 0, -currentWeapon.weaponKickbackDistance);
    }

    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        if (currentWeapon != null)
        {
            ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";
            ammoText.color = (currentAmmoInMag == 0 && totalAmmo == 0) ? Color.red : defaultAmmoColor;
        }
        else { ammoText.text = ""; ammoText.color = defaultAmmoColor; }
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

    // --- Helpers para munición ---

    public List<WeaponAmmoData> GetAmmoData()
    {
        SaveCurrentAmmoState();
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
        SaveCurrentAmmoState();
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

    // ... (resto del código existente)

    // === MÉTODOS PARA GUARDADO Y CARGA DE SLOTS ===

    /// <summary>
    /// Devuelve el tipo de arma en el slot indicado, o -1 si está vacío.
    /// </summary>
    public int GetWeaponTypeInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return -1;

        if (weaponSlots[slotIndex] != null)
        {
            return (int)weaponSlots[slotIndex].weaponType;
        }
        return -1;
    }

    /// <summary>
    /// Devuelve el índice del slot actual (0 o 1).
    /// </summary>
    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    /// <summary>
    /// Fuerza la asignación de un arma a un slot específico (usado al cargar partida).
    /// </summary>
    public void ForceWeaponToSlot(int slotIndex, WeaponData weapon)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;

        // Asignar al array
        weaponSlots[slotIndex] = weapon;

        // Si estamos forzando el slot actual, refrescar visuales inmediatamente
        if (slotIndex == currentSlotIndex)
        {
            RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
        }
    }

    /// <summary>
    /// Cambia al slot indicado sin verificaciones de input (para cargar partida).
    /// </summary>
    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;

        // Guardamos estado del anterior si existe
        SaveCurrentAmmoState();

        currentSlotIndex = slotIndex;
        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    // Método auxiliar para limpiar inventario al cargar
    public void ClearInventory()
    {
        weaponSlots[0] = null;
        weaponSlots[1] = null;
        if (currentWeaponModel != null) Destroy(currentWeaponModel);
        currentWeapon = null;
    }
}