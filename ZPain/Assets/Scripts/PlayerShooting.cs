using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class PlayerShooting : MonoBehaviour
{
    // --- SINGLETON ---
    public static PlayerShooting Instance { get; private set; }

    [Header("Referencias Generales")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CameraController cameraController;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image crosshairImage;

    private RectTransform crosshairRectTransform;
    private Color defaultAmmoColor;

    [Header("Sistema de Puntuación")]
    public int pointsPerHit = 10;
    public int pointsPerKillDisplay = 150;
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("--- SISTEMA DE ULTIMATE (LANZALLAMAS) ---")]
    [SerializeField] private WeaponData ultimateWeaponData;
    [SerializeField] private Image ultimateIconFill;
    [SerializeField] private GameObject ultimateReadyVisual;
    [SerializeField] private KeyCode ultimateKey = KeyCode.X;
    [SerializeField] private float ultimateDuration = 20f;

    private int currentKillsCount = 0;
    private bool isUltimateActive = false;
    private int preUltSlotIndex = 0;

    [Header("Sistema de Inventario (2 Slots)")]
    private WeaponData[] weaponSlots = new WeaponData[2];
    private int currentSlotIndex = 0;

    [Header("Estado del Arma Actual")]
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;
    private bool isBursting = false;

    // --- NUEVO: Variable para el calor de la LMG ---
    private float lmgCurrentHeatTime = 0f;
    // -----------------------------------------------

    private Vector3 weaponInitialLocalPos;
    private Vector3 weaponCurrentOffset;

    // === MUNICIÓN ===
    private int currentAmmoInMag;
    private int totalAmmo;
    private bool isReloading = false;

    private Dictionary<WeaponType, int> ammoInMagCache = new Dictionary<WeaponType, int>();
    private Dictionary<WeaponType, int> totalAmmoCache = new Dictionary<WeaponType, int>();

    [HideInInspector] public float reloadTimeMultiplier = 1f;
    [HideInInspector] public float damageMultiplier = 1f;

    [Header("Efectos Visuales")]
    private Light muzzleLight;
    [SerializeField] private float flashDuration = 0.05f;

    [Header("Efectos de Impacto")]
    [SerializeField] private GameObject bloodParticlePrefab;
    [SerializeField] private GameObject dustParticlePrefab;
    [SerializeField] private GameObject bulletHoleBasePrefab;
    [SerializeField] private Sprite mapBulletHoleSprite;

    private int shotTicker = 0;

    [Header("Apuntado (ADS)")]
    [SerializeField] private float adsSpeed = 10f;
    [SerializeField] private float defaultFOV = 60f;
    private bool isAiming = false;
    private bool weaponHiddenForScope = false;

    [SerializeField][Range(0.1f, 1f)] private float aimSensitivityMultiplier = 0.7f;

    [Header("Animación de Recarga")]
    [SerializeField] private Vector3 reloadRotation = new Vector3(35f, 0f, 0f);
    [SerializeField] private float reloadAnimSpeed = 8f;
    private Vector3 weaponInitialLocalRot;

    [Header("Animación de Cambio de Arma")]
    [SerializeField] private float switchDuration = 0.3f;
    [SerializeField] private Vector3 switchUpOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private float switchBackDistance = 0.1f;
    [SerializeField] private Vector3 switchRotation = new Vector3(-35f, 0f, 0f);

    [SerializeField] private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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
        if (ultimateReadyVisual != null) ultimateReadyVisual.SetActive(false);
        CheckForDirectStart();
    }

    private void CheckForDirectStart()
    {
        if (SaveLoadManager.Instance == null) return;
        if (SaveLoadManager.ShouldLoadGame) return;

        if (currentWeapon == null)
        {
            WeaponData starterWeapon = SaveLoadManager.Instance.GetStartingWeapon();
            if (starterWeapon != null)
            {
                InitializeNewGame(starterWeapon);
            }
        }
    }

    public void InitializeNewGame(WeaponData weaponToEquip)
    {
        weaponSlots[0] = null;
        weaponSlots[1] = null;
        currentSlotIndex = 0;

        if (weaponToEquip != null)
        {
            WeaponData newInstance = Instantiate(weaponToEquip);
            EquipWeapon(newInstance);
            ForceCurrentWeaponAmmoToFull();
            WeaponStore.RegisterStartingWeapon(newInstance);
        }
    }

    void Update()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver) return;

        HandleUltimateLogic();

        if (!isUltimateActive)
        {
            HandleWeaponSwitching();
        }

        // --- NUEVO: Lógica de Calentamiento de LMG ---
        HandleLMGHeat();
        // ---------------------------------------------

        if (isReloading) return;

        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        // Lerp de posición del arma (ADS y Recoil return)
        if (weaponHolder != null && currentWeapon != null)
        {
            Vector3 targetPosition = weaponInitialLocalPos;

            if (isAiming && currentWeapon.sniperScopeSprite == null)
                targetPosition = currentWeapon.aimPosition;

            Vector3 smoothPosition = Vector3.Lerp(
                weaponHolder.localPosition - weaponCurrentOffset,
                targetPosition,
                Time.deltaTime * adsSpeed
            );

            Quaternion targetRotation = Quaternion.Euler(weaponInitialLocalRot);
            if (isAiming && currentWeapon.sniperScopeSprite == null)
                targetRotation = Quaternion.Euler(currentWeapon.aimRotation);

            weaponHolder.localRotation = Quaternion.Slerp(
                weaponHolder.localRotation,
                targetRotation,
                Time.deltaTime * adsSpeed
            );

            weaponCurrentOffset = Vector3.Lerp(
                weaponCurrentOffset,
                Vector3.zero,
                Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed
            );

            weaponHolder.localPosition = smoothPosition + weaponCurrentOffset;
        }
    }

    // --- NUEVO: Función para gestionar el calor de la LMG ---
    void HandleLMGHeat()
    {
        if (currentWeapon == null) return;

        // Solo procesamos si es una LMG y está mejorada
        if (currentWeapon.weaponType == WeaponType.LMG && currentWeapon.isUpgraded)
        {
            // Si estamos disparando y tenemos balas
            if (Input.GetMouseButton(0) && currentAmmoInMag > 0 && !isReloading && !isUltimateActive)
            {
                lmgCurrentHeatTime += Time.deltaTime;
                // Topeamos el tiempo al máximo definido en el scriptable
                if (lmgCurrentHeatTime > currentWeapon.heatRampUpTime)
                    lmgCurrentHeatTime = currentWeapon.heatRampUpTime;
            }
            else
            {
                // Si no disparamos, se enfría
                lmgCurrentHeatTime -= Time.deltaTime * (currentWeapon.heatRampUpTime / currentWeapon.heatCooldownTime);
                if (lmgCurrentHeatTime < 0) lmgCurrentHeatTime = 0;
            }

            // Opcional: Feedback visual en la UI de munición (Texto rojo si está muy caliente)
            if (ammoText != null)
            {
                float heatFactor = lmgCurrentHeatTime / currentWeapon.heatRampUpTime;
                if (heatFactor > 0.5f) ammoText.color = Color.Lerp(defaultAmmoColor, Color.red, heatFactor);
                else if (!isUltimateActive) ammoText.color = defaultAmmoColor;
            }
        }
        else
        {
            lmgCurrentHeatTime = 0;
        }
    }

    // --- NUEVO: Función para calcular daño dinámico (incluye LMG) ---
    float GetCurrentWeaponDamage()
    {
        if (currentWeapon == null) return 0f;

        float baseDmg = currentWeapon.damage;

        // Lógica LMG Mejorada
        if (currentWeapon.weaponType == WeaponType.LMG && currentWeapon.isUpgraded)
        {
            float heatProgress = lmgCurrentHeatTime / currentWeapon.heatRampUpTime; // De 0 a 1
            // Interpolamos entre 1.0 (daño normal) y el multiplicador máximo
            float heatMultiplier = Mathf.Lerp(1.0f, currentWeapon.maxHeatDamageMultiplier, heatProgress);
            baseDmg *= heatMultiplier;
        }

        return baseDmg * damageMultiplier; // Multiplicador global (powerups)
    }

    void HandleUltimateLogic()
    {
        if (isUltimateActive) return;
        if (ultimateWeaponData == null) return;

        float percentage = 0f;
        if (ultimateWeaponData.requiredKillsForUlt > 0)
        {
            percentage = (float)currentKillsCount / ultimateWeaponData.requiredKillsForUlt;
        }

        if (ultimateIconFill != null) ultimateIconFill.fillAmount = percentage;

        if (currentKillsCount >= ultimateWeaponData.requiredKillsForUlt)
        {
            if (ultimateReadyVisual != null && !ultimateReadyVisual.activeSelf)
                ultimateReadyVisual.SetActive(true);

            if (ultimateIconFill != null) ultimateIconFill.fillAmount = 1f;

            if (Input.GetKeyDown(ultimateKey))
                StartCoroutine(ActivateUltimateRoutine());
        }
        else
        {
            if (ultimateReadyVisual != null && ultimateReadyVisual.activeSelf)
                ultimateReadyVisual.SetActive(false);
        }
    }

    public void RegisterZombieKill()
    {
        if (isUltimateActive) return;

        if (ultimateWeaponData != null)
        {
            if (currentKillsCount < ultimateWeaponData.requiredKillsForUlt)
                currentKillsCount++;
        }
    }

    IEnumerator ActivateUltimateRoutine()
    {
        isUltimateActive = true;
        if (ultimateReadyVisual != null) ultimateReadyVisual.SetActive(false);

        preUltSlotIndex = currentSlotIndex;
        RefreshWeaponVisuals(ultimateWeaponData);

        currentAmmoInMag = 9999;
        totalAmmo = 9999;
        UpdateAmmoUI();

        float timer = ultimateDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (ultimateIconFill != null) ultimateIconFill.fillAmount = timer / ultimateDuration;
            yield return null;
        }

        isUltimateActive = false;
        currentKillsCount = 0;
        if (ultimateIconFill != null) ultimateIconFill.fillAmount = 0f;

        SelectSlot(preUltSlotIndex);
    }

    void HandleWeaponSwitching()
    {
        if (isReloading || isAiming || isBursting) return;

        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll > 0f)
        {
            if (currentSlotIndex != 0 && weaponSlots[0] != null) SwitchToSlot(0);
        }
        else if (scroll < 0f)
        {
            if (currentSlotIndex != 1 && weaponSlots[1] != null) SwitchToSlot(1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha1) && currentSlotIndex != 0 && weaponSlots[0] != null) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentSlotIndex != 1 && weaponSlots[1] != null) SwitchToSlot(1);
    }

    private void SwitchToSlot(int newIndex)
    {
        StopAllCoroutines();
        isReloading = false;
        isBursting = false;
        lmgCurrentHeatTime = 0; // Resetear calor al cambiar
        StopAiming();
        SaveCurrentAmmoState();

        if (newIndex == currentSlotIndex) return;
        StartCoroutine(SwitchWeaponCoroutine(newIndex));
    }

    private IEnumerator SwitchWeaponCoroutine(int newIndex)
    {
        isReloading = true;
        Vector3 startPosition = weaponHolder.localPosition;
        Quaternion startRotation = weaponHolder.localRotation;
        Vector3 targetUpPosition = startPosition + switchUpOffset + (Vector3.back * switchBackDistance);
        Quaternion targetRotation = Quaternion.Euler(switchRotation);

        float timer = 0f;
        while (timer < switchDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / switchDuration;
            weaponHolder.localPosition = Vector3.Lerp(startPosition, targetUpPosition, progress);
            weaponHolder.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress);
            yield return null;
        }

        currentSlotIndex = newIndex;
        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
        weaponHolder.localRotation = targetRotation;

        Vector3 currentWeaponPos = weaponHolder.localPosition;
        Quaternion finalRotation = Quaternion.Euler(weaponInitialLocalRot);

        timer = 0f;
        while (timer < switchDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / switchDuration;
            weaponHolder.localPosition = Vector3.Lerp(currentWeaponPos, weaponInitialLocalPos, progress);
            weaponHolder.localRotation = Quaternion.Slerp(targetRotation, finalRotation, progress);
            yield return null;
        }

        weaponHolder.localPosition = weaponInitialLocalPos;
        weaponHolder.localRotation = finalRotation;
        isReloading = false;
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return;
        SaveCurrentAmmoState();

        if (weaponSlots[0] == null)
        {
            weaponSlots[0] = newWeapon;
            currentSlotIndex = 0;
        }
        else if (weaponSlots[1] == null)
        {
            weaponSlots[1] = newWeapon;
            currentSlotIndex = 1;
        }
        else
        {
            weaponSlots[currentSlotIndex] = newWeapon;
        }

        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    private void RefreshWeaponVisuals(WeaponData weaponData)
    {
        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        currentWeapon = weaponData;
        shotTicker = 0;
        lmgCurrentHeatTime = 0; // Resetear calor
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
            currentWeaponModel.transform.localScale = Vector3.one;

            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>();
            muzzleLight = newMuzzleLight;
        }

        LoadAmmoStateForWeapon(currentWeapon);
        isReloading = false;
        UpdateAmmoUI();
        UpdateCrosshair();
    }

    void HandleShooting()
    {
        if (currentWeapon == null) return;

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
                if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    if (currentWeapon.isUpgraded) StartCoroutine(BurstFire()); else Shoot();
                }
                break;

            case WeaponType.Rifle:
            case WeaponType.SMG: // SMG se comporta como Rifle (automática)
            case WeaponType.LMG: // LMG se comporta como Rifle (automática)
                if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    ShootRifle(); // Usamos la misma lógica base
                }
                break;

            case WeaponType.Shotgun:
                if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    StartCoroutine(ShootShotgunCoroutine());
                }
                break;
            case WeaponType.Sniper:
                if (Input.GetMouseButtonDown(0) && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    StartCoroutine(ShootSniperCoroutine());
                }
                break;
            case WeaponType.Flamethrower:
                if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    ShootFlamethrower();
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
            HandleHit(hit, GetCurrentWeaponDamage()); // USAR NUEVO CÁLCULO DE DAÑO
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
                HandleHit(hit, GetCurrentWeaponDamage());
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
            HandleHit(hit, GetCurrentWeaponDamage()); // USAR NUEVO CÁLCULO DE DAÑO (Incluye calor LMG)
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
                HandleHit(hit, GetCurrentWeaponDamage());
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
                        HandleHit(hit, GetCurrentWeaponDamage());
                        alreadyDamaged.Add(zombieHealth);
                        targetsHit++;
                        if (targetsHit >= currentWeapon.penetrationCount) break;
                    }
                }
                else HandleHit(hit, GetCurrentWeaponDamage());
            }
        }

        if (currentWeapon.boltActionSound != null)
        {
            yield return new WaitForSeconds(currentWeapon.actionSoundDelay);
            PlaySound(currentWeapon.boltActionSound);
        }
    }

    void ShootFlamethrower()
    {
        PlaySound(currentWeapon.shootSound);
        StartCoroutine(MuzzleFlashRoutine());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        RaycastHit[] hits = Physics.SphereCastAll(
            ray.origin,
            currentWeapon.flameRadius,
            ray.direction,
            currentWeapon.range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        HashSet<ZombieController> burnedZombies = new HashSet<ZombieController>();

        foreach (RaycastHit hit in hits)
        {
            ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
            ZombieController zombie = null;

            if (hitbox != null)
                zombie = hitbox.zombieController;
            else
                zombie = hit.collider.GetComponent<ZombieController>();

            if (zombie != null)
            {
                if (burnedZombies.Add(zombie))
                {
                    // El lanzallamas tiene daño fijo, no se ve afectado por la lógica de LMG
                    HandleHit(hit, currentWeapon.damage * damageMultiplier);
                }
            }
        }
    }

    void HandleHit(RaycastHit hit, float damage)
    {
        ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
        ZombieController zombieHealth = null;

        if (hitbox != null) zombieHealth = hitbox.zombieController;
        else zombieHealth = hit.collider.GetComponent<ZombieController>();

        if (zombieHealth != null)
        {
            if (zombieHealth.GetHP() <= 0)
            {
                SpawnImpactEffects(hit);
                return;
            }

            if (currentWeapon.weaponType != WeaponType.Flamethrower)
            {
                if (ScoreManager.Instance != null)
                    ScoreManager.Instance.AddScore(pointsPerHit);
            }

            if (hitbox != null)
                zombieHealth.TakeDamage(damage, hitbox.hitboxType);
            else
                zombieHealth.TakeDamage(damage);

            // --- CHEQUEO DE MUERTE PARA VAMPIRISMO (SMG) ---
            if (zombieHealth.GetHP() <= 0)
            {
                ShowFloatingScore(hit.point, pointsPerKillDisplay);

                // LÓGICA VAMPIRISMO SMG MEJORADA
                if (currentWeapon.weaponType == WeaponType.SMG && currentWeapon.isUpgraded)
                {
                    // Recuperamos balas pero sin pasarnos del máximo del cargador
                    if (currentAmmoInMag < currentWeapon.magCapacity)
                    {
                        currentAmmoInMag += currentWeapon.vampireAmmoRestore;
                        if (currentAmmoInMag > currentWeapon.magCapacity)
                            currentAmmoInMag = currentWeapon.magCapacity;

                        UpdateAmmoUI();
                    }
                }
            }
            else
            {
                if (currentWeapon.weaponType != WeaponType.Flamethrower)
                    ShowFloatingScore(hit.point, pointsPerHit);
            }
        }

        SpawnImpactEffects(hit);
    }

    private void ShowFloatingScore(Vector3 position, int points)
    {
        if (floatingTextPrefab != null)
        {
            Vector3 spawnPos = position + (Vector3.up * 0.3f);
            GameObject ft = Instantiate(floatingTextPrefab, spawnPos, Quaternion.identity);
            FloatingText textScript = ft.GetComponent<FloatingText>();
            if (textScript != null)
            {
                textScript.Setup(points);
            }
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
        if (isUltimateActive) return;

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

    private void SpawnImpactEffects(RaycastHit hit)
    {
        GameObject particlePrefab = null;
        Sprite decalSprite = null;

        if (hit.collider.CompareTag("Zombie"))
        {
            particlePrefab = bloodParticlePrefab;
        }
        else if (hit.collider.CompareTag("Mapa"))
        {
            particlePrefab = dustParticlePrefab;
            decalSprite = mapBulletHoleSprite;
        }

        if (particlePrefab != null)
        {
            GameObject effect = Instantiate(particlePrefab, hit.point + (hit.normal * 0.02f), Quaternion.LookRotation(hit.normal));
            Destroy(effect, 2f);
        }

        if (decalSprite != null && bulletHoleBasePrefab != null)
        {
            Quaternion hitRotation = Quaternion.LookRotation(hit.normal);
            GameObject hole = Instantiate(bulletHoleBasePrefab, hit.point + (hit.normal * 0.01f), hitRotation);
            SpriteRenderer sr = hole.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = decalSprite;
            hole.transform.SetParent(hit.collider.transform);
            Destroy(hole, 5f);
        }
    }

    private void ApplyRecoil()
    {
        if (isUltimateActive) return;

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
            if (isUltimateActive)
            {
                ammoText.text = "∞ / ∞";
                ammoText.color = Color.yellow;
            }
            else
            {
                ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";
                // Si la LMG está caliente, el color lo maneja HandleLMGHeat, 
                // pero si no, o si está vacía, usamos lógica normal:
                if (currentWeapon.weaponType != WeaponType.LMG || !currentWeapon.isUpgraded)
                {
                    ammoText.color = (currentAmmoInMag == 0 && totalAmmo == 0) ? Color.red : defaultAmmoColor;
                }
            }
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

    // === MÉTODOS DE RECARGA ===
    void HandleReloadInput()
    {
        if (isUltimateActive) return;

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
        // Si recargamos, la LMG se enfría de golpe
        lmgCurrentHeatTime = 0;

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


    void HandleAiming()
    {
        if (currentWeapon == null || !currentWeapon.canAim)
        {
            if (isAiming) StopAiming();
            return;
        }

        if (Input.GetMouseButtonDown(1)) isAiming = true;
        if (Input.GetMouseButtonUp(1)) isAiming = false;

        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);

        if (cameraController != null)
        {
            if (isAiming)
                cameraController.SetSensitivityMultiplier(aimSensitivityMultiplier);
            else
                cameraController.SetSensitivityMultiplier(1f);
        }

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

            if (isAiming) crosshairImage.enabled = false;
            else UpdateCrosshair();
        }
    }

    public void StopAiming()
    {
        isAiming = false;
        if (cameraController != null) cameraController.SetSensitivityMultiplier(1f);

        if (playerCamera != null) playerCamera.fieldOfView = defaultFOV;
        if (weaponHiddenForScope)
        {
            if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
            weaponHiddenForScope = false;
        }
        UpdateCrosshair();
    }

    // === MÉTODOS DE GESTIÓN DE SLOTS Y GUARDADO ===
    public int GetWeaponTypeInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return -1;
        if (weaponSlots[slotIndex] != null) return (int)weaponSlots[slotIndex].weaponType;
        return -1;
    }

    public int GetCurrentSlotIndex()
    {
        return currentSlotIndex;
    }

    public void ForceWeaponToSlot(int slotIndex, WeaponData weapon)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;
        weaponSlots[slotIndex] = weapon;
        if (slotIndex == currentSlotIndex) RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return;
        SaveCurrentAmmoState();
        currentSlotIndex = slotIndex;
        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    public void ClearInventory()
    {
        weaponSlots[0] = null;
        weaponSlots[1] = null;
        if (currentWeaponModel != null) Destroy(currentWeaponModel);
        currentWeapon = null;
    }

    private void SaveCurrentAmmoState()
    {
        if (currentWeapon != null && !isUltimateActive)
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
            currentAmmoInMag = weapon.magCapacity;
            totalAmmo = weapon.maxAmmo - currentWeapon.magCapacity;
            ammoInMagCache[weapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[weapon.weaponType] = totalAmmo;
        }
    }

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

    void OnDrawGizmos()
    {
        if (playerCamera == null || currentWeapon == null) return;

        Gizmos.color = Color.red;

        Vector3 startPosition = playerCamera.transform.position;
        Vector3 direction = playerCamera.transform.forward;
        Vector3 endPosition = startPosition + (direction * currentWeapon.range);

        Gizmos.DrawLine(startPosition, endPosition);

        if (currentWeapon.weaponType == WeaponType.Flamethrower)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.5f);
            Gizmos.DrawWireSphere(startPosition, currentWeapon.flameRadius);
            Gizmos.DrawWireSphere(endPosition, currentWeapon.flameRadius);
        }
    }
}