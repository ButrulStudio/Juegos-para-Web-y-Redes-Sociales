using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;

public class PlayerShooting : MonoBehaviour
{
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

    [Header("Ajustes Móviles (Auto-Fire)")]
    public bool useAutoFire = true;
    public LayerMask obstacleLayers;

    // --- VARIABLES MÓVILES ---
    private bool isMobileFiring = false;
    private bool isMobileAiming = false;

    [Header("--- SISTEMA DE ULTIMATE (LANZALLAMAS) ---")]
    [SerializeField] private WeaponData ultimateWeaponData;
    [SerializeField] private Image ultimateIconFill;
    [SerializeField] private GameObject ultimateReadyVisual;
    [SerializeField] private KeyCode ultimateKey = KeyCode.X;
    [SerializeField] private float ultimateDuration = 20f;

    [Header("Ultimate UI & Unlock")]
    [SerializeField] private GameObject ultimateUIParent;
    [SerializeField] private TextMeshProUGUI ultimateKillCountText;
    [SerializeField] private int totalCollectiblesRequired = 5;

    [Header("Coleccionables UI")]
    [SerializeField] private TextMeshProUGUI collectibleCountText;
    [SerializeField] private float collectibleDisplayTime = 3f;
    private Coroutine displayCollectibleCoroutine;

    // Estado interno del ultimate
    private bool isUltimateUnlocked = false;
    private int collectiblesFoundCount = 0;
    private int currentKillsCount = 0;
    private bool ultimateIsReady = false;
    private bool isUltimateActive = false;
    private int preUltSlotIndex = 0;

    [Header("--- ATAQUE CUCHILLO ---")]
    [SerializeField] private KeyCode meleeKey = KeyCode.C;
    [SerializeField] private GameObject knifeGameObject;
    [SerializeField] private float meleeDuration = 0.6f;
    [SerializeField] private float damageDelay = 0.2f;
    [SerializeField] private float knifeDamage = 50f;
    [SerializeField] private float knifeRange = 2.5f;
    [SerializeField] private AudioClip knifeSwingSound;
    [SerializeField] private string meleeAnimationName = "Attack";

    private Animator knifeAnimator;
    private bool isMeleeAttacking = false;

    [Header("Inventario")]
    private WeaponData[] weaponSlots = new WeaponData[2];
    private int currentSlotIndex = 0;

    [Header("Estado del Arma Actual")]
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;
    private bool isBursting = false;

    private float lmgCurrentHeatTime = 0f;

    private Vector3 weaponInitialLocalPos;
    private Vector3 weaponCurrentOffset;
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

    [Header("Animaciones")]
    [SerializeField] private Vector3 reloadRotation = new Vector3(35f, 0f, 0f);
    [SerializeField] private float reloadAnimSpeed = 8f;
    private Vector3 weaponInitialLocalRot;
    [SerializeField] private float switchDuration = 0.3f;
    [SerializeField] private Vector3 switchUpOffset = new Vector3(0, 0.5f, 0);
    [SerializeField] private Vector3 switchBackDistance = new Vector3(0, 0, 0.1f);
    [SerializeField] private Vector3 switchRotation = new Vector3(-35f, 0f, 0f);

    [SerializeField] private AudioSource audioSource;

    [Header("Weapon Drop")]
    public bool isWeaponDropped = false;

    [Header("UI Notificación")]
    [SerializeField] private TextMeshProUGUI unlockMessageText;
    [SerializeField] private float messageDisplayDuration = 5f;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (crosshairImage != null) crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();
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
        if (ultimateUIParent != null) ultimateUIParent.SetActive(false);
        if (collectibleCountText != null) collectibleCountText.gameObject.SetActive(false);

        if (!Application.isMobilePlatform && !Application.isEditor)
        {
            useAutoFire = false;
        }

        CheckForDirectStart();

        if (knifeGameObject != null)
        {
            knifeAnimator = knifeGameObject.GetComponent<Animator>();
            if (knifeAnimator == null) knifeAnimator = knifeGameObject.GetComponentInChildren<Animator>();
            knifeGameObject.SetActive(false);
        }

        if (unlockMessageText != null) unlockMessageText.gameObject.SetActive(false);
    }

    private void CheckForDirectStart()
    {
        if (currentWeapon == null && GameManager.Instance != null && GameManager.Instance.startingWeaponAsset != null)
        {
            InitializeNewGame(GameManager.Instance.startingWeaponAsset);
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

        if (isWeaponDropped) return;

        if (!isUltimateActive && !isMeleeAttacking)
        {
            HandleMeleeInput();
            HandleWeaponSwitching();
        }

        HandleLMGHeat();

        if (isReloading || isMeleeAttacking) return;

        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        if (weaponHolder != null && currentWeapon != null)
        {
            Vector3 targetPosition = weaponInitialLocalPos;
            if (isAiming && currentWeapon.sniperScopeSprite == null) targetPosition = currentWeapon.aimPosition;

            Vector3 smoothPosition = Vector3.Lerp(weaponHolder.localPosition - weaponCurrentOffset, targetPosition, Time.deltaTime * adsSpeed);
            Quaternion targetRotation = Quaternion.Euler(weaponInitialLocalRot);
            if (isAiming && currentWeapon.sniperScopeSprite == null) targetRotation = Quaternion.Euler(currentWeapon.aimRotation);

            weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, targetRotation, Time.deltaTime * adsSpeed);
            weaponCurrentOffset = Vector3.Lerp(weaponCurrentOffset, Vector3.zero, Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed);
            weaponHolder.localPosition = smoothPosition + weaponCurrentOffset;
        }
    }

    void HandleLMGHeat()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.weaponType == WeaponType.LMG && currentWeapon.isUpgraded)
        {
            bool isFiring = Input.GetMouseButton(0) || isMobileFiring || (useAutoFire && IsAimingAtEnemy());

            if (isFiring && currentAmmoInMag > 0 && !isReloading && !isUltimateActive)
            {
                lmgCurrentHeatTime += Time.deltaTime;
                if (lmgCurrentHeatTime > currentWeapon.heatRampUpTime)
                    lmgCurrentHeatTime = currentWeapon.heatRampUpTime;
            }
            else
            {
                lmgCurrentHeatTime -= Time.deltaTime * (currentWeapon.heatRampUpTime / currentWeapon.heatCooldownTime);
                if (lmgCurrentHeatTime < 0) lmgCurrentHeatTime = 0;
            }

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

    float GetCurrentWeaponDamage()
    {
        if (currentWeapon == null) return 0f;

        float baseDmg = currentWeapon.damage;

        if (currentWeapon.weaponType == WeaponType.LMG && currentWeapon.isUpgraded)
        {
            float heatProgress = lmgCurrentHeatTime / currentWeapon.heatRampUpTime;
            float heatMultiplier = Mathf.Lerp(1.0f, currentWeapon.maxHeatDamageMultiplier, heatProgress);
            baseDmg *= heatMultiplier;
        }

        return baseDmg * damageMultiplier;
    }

    private bool IsAimingAtEnemy()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, currentWeapon.range))
        {
            if (hit.collider.CompareTag("Zombie")) return true;
            ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
            if (hitbox != null) return true;
        }
        return false;
    }

    void HandleShooting()
    {
        if (currentWeapon == null) return;

        // 1. Detección de UI
        bool isPointerOverUI = false;
        if (Cursor.lockState == CursorLockMode.None)
        {
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject()) isPointerOverUI = true;
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    isPointerOverUI = true;
            }
        }

        // 2. Auto-Fire
        bool enemyInSight = false;
        if (useAutoFire && !isPointerOverUI && !isReloading)
        {
            enemyInSight = IsAimingAtEnemy();
        }

        // 3. Inputs
        bool mouseInput = Input.GetMouseButton(0) && !isPointerOverUI;
        bool mouseInputDown = Input.GetMouseButtonDown(0) && !isPointerOverUI;

        bool fireInputHeld = mouseInput || isMobileFiring || enemyInSight;
        bool fireInputDownCombined = mouseInputDown;

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
            case WeaponType.Shotgun:
            case WeaponType.Sniper:
                // --- CORRECCIÓN AQUÍ ---
                // Separamos la lógica para asegurar que el Sniper mejorado NO use BurstFire
                if ((fireInputDownCombined || (enemyInSight && useAutoFire)) && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;

                    if (currentWeapon.weaponType == WeaponType.Shotgun)
                    {
                        StartCoroutine(ShootShotgunCoroutine());
                    }
                    else if (currentWeapon.weaponType == WeaponType.Sniper)
                    {
                        StartCoroutine(ShootSniperCoroutine());
                    }
                    else if (currentWeapon.isUpgraded && currentWeapon.weaponType == WeaponType.Pistol)
                    {
                        // Solo la pistola usa BurstFire al mejorarse
                        StartCoroutine(BurstFire());
                    }
                    else
                    {
                        // Disparo normal (Pistola sin mejorar)
                        Shoot();
                    }
                }
                break;

            case WeaponType.Rifle:
            case WeaponType.SMG:
            case WeaponType.LMG:
            case WeaponType.Flamethrower:
                if (fireInputHeld && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    if (currentWeapon.weaponType == WeaponType.Flamethrower) ShootFlamethrower();
                    else ShootRifle();
                }
                break;
        }
    }

    // --- MÉTODOS PÚBLICOS UI MÓVIL ---
    public void SetMobileFiring(bool isFiring) { isMobileFiring = isFiring; }
    public void MobileToggleAim() { isMobileAiming = !isMobileAiming; }

    public void MobileFireOnce()
    {
        if (currentWeapon == null) return;
        if (Time.time >= nextFireTime && !isBursting && !isReloading)
        {
            if (currentWeapon.weaponType == WeaponType.Pistol || currentWeapon.weaponType == WeaponType.Shotgun || currentWeapon.weaponType == WeaponType.Sniper)
            {
                nextFireTime = Time.time + currentWeapon.fireRate;

                // --- CORRECCIÓN TAMBIÉN AQUÍ ---
                if (currentWeapon.weaponType == WeaponType.Shotgun) StartCoroutine(ShootShotgunCoroutine());
                else if (currentWeapon.weaponType == WeaponType.Sniper) StartCoroutine(ShootSniperCoroutine());
                else if (currentWeapon.isUpgraded && currentWeapon.weaponType == WeaponType.Pistol) StartCoroutine(BurstFire());
                else Shoot();
            }
        }
    }

    public void MobileReload()
    {
        if (!isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0)
            StartCoroutine(ReloadCoroutine());
    }

    // --- LÓGICA DE DISPARO INTERNA ---
    void Shoot()
    {
        if (currentAmmoInMag <= 0) { HandleEmptyClip(); return; }
        FireBaseLogic();
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range)) HandleHit(hit, GetCurrentWeaponDamage());
        ApplyRecoil();
    }

    IEnumerator BurstFire()
    {
        if (isBursting) yield break;
        isBursting = true;
        int burstCount = 3;
        for (int i = 0; i < burstCount; i++)
        {
            if (currentAmmoInMag <= 0) { HandleEmptyClip(); break; }
            FireBaseLogic();
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range)) HandleHit(hit, GetCurrentWeaponDamage());
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
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range)) HandleHit(hit, GetCurrentWeaponDamage());
        ApplyRecoil();
    }

    IEnumerator ShootShotgunCoroutine()
    {
        if (currentAmmoInMag <= 0) { HandleEmptyClip(); yield break; }
        FireBaseLogic();
        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            Vector3 direction = playerCamera.transform.forward;
            direction = Quaternion.Euler(Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle), Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle), 0) * direction;
            Ray ray = new Ray(playerCamera.transform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range)) HandleHit(hit, GetCurrentWeaponDamage());
        }
        ApplyRecoil();
        if (currentWeapon.pumpActionSound != null) { yield return new WaitForSeconds(currentWeapon.actionSoundDelay); PlaySound(currentWeapon.pumpActionSound); }
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
                ZombieController zombieHealth = (hitbox != null) ? hitbox.zombieController : hit.collider.GetComponent<ZombieController>();
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
        if (currentWeapon.boltActionSound != null) { yield return new WaitForSeconds(currentWeapon.actionSoundDelay); PlaySound(currentWeapon.boltActionSound); }
    }

    void ShootFlamethrower()
    {
        PlaySound(currentWeapon.shootSound);
        StartCoroutine(MuzzleFlashRoutine());
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(ray.origin, currentWeapon.flameRadius, ray.direction, currentWeapon.range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        HashSet<ZombieController> burnedZombies = new HashSet<ZombieController>();
        foreach (RaycastHit hit in hits)
        {
            ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
            ZombieController zombie = (hitbox != null) ? hitbox.zombieController : hit.collider.GetComponent<ZombieController>();
            if (zombie != null) { if (burnedZombies.Add(zombie)) HandleHit(hit, currentWeapon.damage * damageMultiplier); }
        }
    }

    void HandleHit(RaycastHit hit, float damage)
    {
        Palomas collectible = hit.collider.GetComponent<Palomas>();
        if (collectible != null) { collectible.TakeDamage(damage * damageMultiplier); SpawnImpactEffects(hit); return; }

        ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
        ZombieController zombieHealth = (hitbox != null) ? hitbox.zombieController : hit.collider.GetComponent<ZombieController>();

        if (zombieHealth != null)
        {
            if (zombieHealth.GetHP() <= 0) { SpawnImpactEffects(hit); return; }
            if (currentWeapon.weaponType != WeaponType.Flamethrower && ScoreManager.Instance != null) ScoreManager.Instance.AddScore(pointsPerHit);

            if (hitbox != null) zombieHealth.TakeDamage(damage, hitbox.hitboxType); else zombieHealth.TakeDamage(damage);

            if (zombieHealth.GetHP() <= 0)
            {
                ShowFloatingScore(hit.point, pointsPerKillDisplay);
                if (currentWeapon.weaponType == WeaponType.SMG && currentWeapon.isUpgraded)
                {
                    if (currentAmmoInMag < currentWeapon.magCapacity)
                    {
                        currentAmmoInMag += currentWeapon.vampireAmmoRestore;
                        if (currentAmmoInMag > currentWeapon.magCapacity) currentAmmoInMag = currentWeapon.magCapacity;
                        UpdateAmmoUI();
                    }
                }
            }
            else { if (currentWeapon.weaponType != WeaponType.Flamethrower) ShowFloatingScore(hit.point, pointsPerHit); }
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
            if (textScript != null) textScript.Setup(points);
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
        if (totalAmmo > 0) { if (!isReloading) StartCoroutine(ReloadCoroutine()); }
        else { if (ammoText != null) { ammoText.text = "SIN MUNICIÓN"; ammoText.color = Color.red; } }
    }

    private void SpawnImpactEffects(RaycastHit hit)
    {
        GameObject particlePrefab = null;
        Sprite decalSprite = null;
        if (hit.collider.CompareTag("Zombie")) particlePrefab = bloodParticlePrefab;
        else if (hit.collider.CompareTag("Mapa")) { particlePrefab = dustParticlePrefab; decalSprite = mapBulletHoleSprite; }

        if (particlePrefab != null) { GameObject effect = Instantiate(particlePrefab, hit.point + (hit.normal * 0.02f), Quaternion.LookRotation(hit.normal)); Destroy(effect, 2f); }
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
            if (isUltimateActive) { ammoText.text = "∞ / ∞"; ammoText.color = Color.yellow; }
            else
            {
                ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";
                if (currentWeapon.weaponType != WeaponType.LMG || !currentWeapon.isUpgraded) ammoText.color = (currentAmmoInMag == 0 && totalAmmo == 0) ? Color.red : defaultAmmoColor;
            }
        }
        else { ammoText.text = ""; ammoText.color = defaultAmmoColor; }
    }

    private void ClearCrosshair() { if (crosshairImage != null) crosshairImage.enabled = false; }
    public void SetCrosshair(Vector2 size, Vector2 aimedSize) { if (crosshairRectTransform == null) return; crosshairRectTransform.sizeDelta = size; crosshairImage.enabled = true; }
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

    private IEnumerator MuzzleFlashRoutine() { if (muzzleLight == null) yield break; muzzleLight.enabled = true; yield return new WaitForSeconds(flashDuration); muzzleLight.enabled = false; }
    private void PlaySound(AudioClip clip) { if (audioSource != null && clip != null) audioSource.PlayOneShot(clip); }

    void HandleReloadInput()
    {
        if (isUltimateActive) return;
        if (currentWeapon == null) return;
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0) StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        PlaySound(currentWeapon.reloadSound);
        lmgCurrentHeatTime = 0;
        float reloadTime = currentWeapon.reloadTime * reloadTimeMultiplier;
        float animTime = 1f / reloadAnimSpeed;
        float t = 0;
        while (t < 1f) { t += Time.deltaTime * reloadAnimSpeed; weaponHolder.localRotation = Quaternion.Lerp(Quaternion.Euler(weaponInitialLocalRot), Quaternion.Euler(reloadRotation), t); yield return null; }

        int neededAmmo = currentWeapon.magCapacity - currentAmmoInMag;
        int ammoToLoad = Mathf.Min(neededAmmo, totalAmmo);
        float waitTime = Mathf.Max(0, reloadTime - animTime * 2f);

        if (currentWeapon.weaponType == WeaponType.Shotgun && ammoToLoad > 0)
        {
            float timePerBullet = (waitTime > 0 && ammoToLoad > 0) ? waitTime / ammoToLoad : 0;
            for (int i = 0; i < ammoToLoad; i++)
            {
                if (timePerBullet > 0) yield return new WaitForSeconds(timePerBullet);
                currentAmmoInMag++; totalAmmo--; UpdateAmmoUI();
            }
        }
        else
        {
            if (waitTime > 0) yield return new WaitForSeconds(waitTime);
            currentAmmoInMag += ammoToLoad; totalAmmo -= ammoToLoad;
        }

        t = 0;
        while (t < 1f) { t += Time.deltaTime * reloadAnimSpeed; weaponHolder.localRotation = Quaternion.Lerp(Quaternion.Euler(reloadRotation), Quaternion.Euler(weaponInitialLocalRot), t); yield return null; }
        isReloading = false; if (currentWeapon.weaponType != WeaponType.Shotgun) UpdateAmmoUI();
    }

    void HandleAiming()
    {
        if (currentWeapon == null || !currentWeapon.canAim) { if (isAiming) StopAiming(); return; }

        bool aimInput = Input.GetMouseButton(1) || isMobileAiming;
        if (aimInput) isAiming = true; else isAiming = false;

        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);

        if (cameraController != null)
        {
            if (isAiming) cameraController.SetSensitivityMultiplier(aimSensitivityMultiplier);
            else cameraController.SetSensitivityMultiplier(1f);
        }

        if (isAiming && currentWeapon.sniperScopeSprite != null)
        {
            if (crosshairRectTransform != null) { crosshairRectTransform.anchorMin = new Vector2(0.5f, 0.5f); crosshairRectTransform.anchorMax = new Vector2(0.5f, 0.5f); crosshairRectTransform.pivot = new Vector2(0.5f, 0.5f); crosshairRectTransform.anchoredPosition = Vector2.zero; crosshairRectTransform.sizeDelta = currentWeapon.aimedCrosshairSize; }
            crosshairImage.sprite = currentWeapon.sniperScopeSprite; crosshairImage.enabled = true;
            if (!weaponHiddenForScope) { if (currentWeaponModel != null) currentWeaponModel.SetActive(false); weaponHiddenForScope = true; }
        }
        else
        {
            if (weaponHiddenForScope) { if (currentWeaponModel != null) currentWeaponModel.SetActive(true); weaponHiddenForScope = false; }
            if (isAiming) crosshairImage.enabled = false; else UpdateCrosshair();
        }
    }

    public void StopAiming()
    {
        isAiming = false;
        isMobileAiming = false;
        if (cameraController != null) cameraController.SetSensitivityMultiplier(1f);
        if (playerCamera != null) playerCamera.fieldOfView = defaultFOV;
        if (weaponHiddenForScope) { if (currentWeaponModel != null) currentWeaponModel.SetActive(true); weaponHiddenForScope = false; }
        UpdateCrosshair();
    }

    // --- MÉTODOS DE APOYO (Inventario y Cuchillo) ---

    void HandleMeleeInput()
    {
        if (Input.GetKeyDown(meleeKey) && !isReloading && !isUltimateActive && currentWeapon != null) StartCoroutine(QuickMeleeRoutine());
    }

    private IEnumerator QuickMeleeRoutine()
    {
        if (knifeGameObject == null) yield break;
        isMeleeAttacking = true; isReloading = false; isBursting = false; StopAiming();
        Vector3 savedWeaponPos = Vector3.zero; Quaternion savedWeaponRot = Quaternion.identity;
        if (currentWeaponModel != null) { savedWeaponPos = currentWeaponModel.transform.localPosition; savedWeaponRot = currentWeaponModel.transform.localRotation; currentWeaponModel.SetActive(false); }
        if (crosshairImage != null) crosshairImage.enabled = false; if (cameraController != null) cameraController.enabled = false;
        knifeGameObject.SetActive(true); if (knifeAnimator != null) knifeAnimator.SetTrigger(meleeAnimationName);
        yield return new WaitForSeconds(damageDelay);
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, knifeRange)) HandleHit(hit, knifeDamage);
        yield return new WaitForSeconds(meleeDuration - damageDelay);
        knifeGameObject.SetActive(false);
        if (currentWeaponModel != null) { currentWeaponModel.SetActive(true); currentWeaponModel.transform.localPosition = savedWeaponPos; currentWeaponModel.transform.localRotation = savedWeaponRot; }
        if (cameraController != null) cameraController.enabled = true; if (crosshairImage != null) crosshairImage.enabled = true;
        isMeleeAttacking = false;
    }

    void HandleUltimateLogic()
    {
        if (!isUltimateUnlocked) { if (ultimateUIParent != null && ultimateUIParent.activeSelf) ultimateUIParent.SetActive(false); return; }
        if (ultimateUIParent != null && !ultimateUIParent.activeSelf) ultimateUIParent.SetActive(true);
        if (isUltimateActive) return;
        if (ultimateWeaponData == null) return;
        float percentage = 0f;
        if (ultimateWeaponData.requiredKillsForUlt > 0) percentage = (float)currentKillsCount / ultimateWeaponData.requiredKillsForUlt;
        if (ultimateIconFill != null) ultimateIconFill.fillAmount = percentage;

        if (currentKillsCount >= ultimateWeaponData.requiredKillsForUlt)
        {
            ultimateIsReady = true;
            if (ultimateReadyVisual != null && !ultimateReadyVisual.activeSelf) ultimateReadyVisual.SetActive(true);
            if (ultimateIconFill != null) ultimateIconFill.fillAmount = 1f;
            if (Input.GetKeyDown(ultimateKey)) StartCoroutine(ActivateUltimateRoutine());
        }
        else
        {
            ultimateIsReady = false;
            if (ultimateReadyVisual != null && ultimateReadyVisual.activeSelf) ultimateReadyVisual.SetActive(false);
        }
    }

    void HandleWeaponSwitching()
    {
        if (isReloading || isAiming || isBursting) return;
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0f) { if (currentSlotIndex != 0 && weaponSlots[0] != null) SwitchToSlot(0); }
        else if (scroll < 0f) { if (currentSlotIndex != 1 && weaponSlots[1] != null) SwitchToSlot(1); }
        if (Input.GetKeyDown(KeyCode.Alpha1) && currentSlotIndex != 0 && weaponSlots[0] != null) SwitchToSlot(0);
        if (Input.GetKeyDown(KeyCode.Alpha2) && currentSlotIndex != 1 && weaponSlots[1] != null) SwitchToSlot(1);
    }

    private void SwitchToSlot(int newIndex)
    {
        StopAllCoroutines(); isReloading = false; isBursting = false; lmgCurrentHeatTime = 0; StopAiming(); SaveCurrentAmmoState();
        if (newIndex == currentSlotIndex) return; StartCoroutine(SwitchWeaponCoroutine(newIndex));
    }

    private IEnumerator SwitchWeaponCoroutine(int newIndex)
    {
        isReloading = true; Vector3 startPosition = weaponHolder.localPosition; Quaternion startRotation = weaponHolder.localRotation;
        Vector3 targetUpPosition = startPosition + switchUpOffset + (Vector3.back * switchBackDistance.z); Quaternion targetRotation = Quaternion.Euler(switchRotation);
        float timer = 0f; while (timer < switchDuration) { timer += Time.deltaTime; float progress = timer / switchDuration; weaponHolder.localPosition = Vector3.Lerp(startPosition, targetUpPosition, progress); weaponHolder.localRotation = Quaternion.Slerp(startRotation, targetRotation, progress); yield return null; }
        currentSlotIndex = newIndex; RefreshWeaponVisuals(weaponSlots[currentSlotIndex]); weaponHolder.localRotation = targetRotation;
        Vector3 currentWeaponPos = weaponHolder.localPosition; Quaternion finalRotation = Quaternion.Euler(weaponInitialLocalRot);
        timer = 0f; while (timer < switchDuration) { timer += Time.deltaTime; float progress = timer / switchDuration; weaponHolder.localPosition = Vector3.Lerp(currentWeaponPos, weaponInitialLocalPos, progress); weaponHolder.localRotation = Quaternion.Slerp(targetRotation, finalRotation, progress); yield return null; }
        weaponHolder.localPosition = weaponInitialLocalPos; weaponHolder.localRotation = finalRotation; isReloading = false;
    }

    public void RegisterZombieKill()
    {
        if (isUltimateActive) return;
        if (!isUltimateUnlocked) return;
        if (ultimateWeaponData != null) { if (currentKillsCount < ultimateWeaponData.requiredKillsForUlt) currentKillsCount++; }
        UpdateUltimateUI();
    }

    IEnumerator ActivateUltimateRoutine()
    {
        isUltimateActive = true; ultimateIsReady = false; if (ultimateReadyVisual != null) ultimateReadyVisual.SetActive(false);
        preUltSlotIndex = currentSlotIndex; RefreshWeaponVisuals(ultimateWeaponData);
        currentAmmoInMag = 9999; totalAmmo = 9999; UpdateAmmoUI(); currentKillsCount = 0; UpdateUltimateUI();
        float timer = ultimateDuration;
        while (timer > 0) { timer -= Time.deltaTime; if (ultimateIconFill != null) ultimateIconFill.fillAmount = timer / ultimateDuration; yield return null; }
        isUltimateActive = false; if (ultimateIconFill != null) ultimateIconFill.fillAmount = 0f;
        SelectSlot(preUltSlotIndex); UpdateUltimateUI();
    }

    public void RegisterCollectibleFound()
    {
        if (isUltimateUnlocked) return;
        collectiblesFoundCount++;
        UpdateCollectibleDisplay();
        if (collectiblesFoundCount >= totalCollectiblesRequired && !isUltimateUnlocked) UnlockUltimatePermanently();
    }

    private void UnlockUltimatePermanently()
    {
        isUltimateUnlocked = true;
        if (ultimateUIParent != null) ultimateUIParent.SetActive(true);
        if (ultimateWeaponData != null) currentKillsCount = ultimateWeaponData.requiredKillsForUlt;
        UpdateUltimateUI();
        if (unlockMessageText != null) { StopCoroutine("DisplayUnlockMessageRoutine"); StartCoroutine("DisplayUnlockMessageRoutine"); }
    }

    private void UpdateUltimateUI()
    {
        if (ultimateWeaponData == null) return;
        if (!isUltimateUnlocked) { if (ultimateUIParent != null) ultimateUIParent.SetActive(false); return; }
        if (ultimateUIParent != null && !ultimateUIParent.activeSelf) ultimateUIParent.SetActive(true);
        int requiredKills = ultimateWeaponData.requiredKillsForUlt;
        float fillAmount = requiredKills > 0 ? (float)currentKillsCount / requiredKills : 1f;
        if (ultimateIconFill != null) ultimateIconFill.fillAmount = fillAmount;
        ultimateIsReady = currentKillsCount >= requiredKills;
        if (ultimateReadyVisual != null) ultimateReadyVisual.SetActive(ultimateIsReady);
        if (ultimateKillCountText != null) ultimateKillCountText.text = ultimateIsReady ? $"LISTO" : $"{currentKillsCount}/{requiredKills}";
    }

    public void UpdateCollectibleDisplay() { if (collectibleCountText == null) return; collectibleCountText.text = $"{collectiblesFoundCount} / {totalCollectiblesRequired}"; if (displayCollectibleCoroutine != null) StopCoroutine(displayCollectibleCoroutine); displayCollectibleCoroutine = StartCoroutine(DisplayCollectibleRoutine()); }
    private IEnumerator DisplayCollectibleRoutine() { if (collectibleCountText != null) collectibleCountText.gameObject.SetActive(true); yield return new WaitForSeconds(collectibleDisplayTime); if (collectibleCountText != null) collectibleCountText.gameObject.SetActive(false); displayCollectibleCoroutine = null; }
    private IEnumerator DisplayUnlockMessageRoutine() { string key = ultimateKey.ToString(); unlockMessageText.text = $"¡Has desbloqueado el arma especial! Pulsa [{key}] para utilizarla"; unlockMessageText.gameObject.SetActive(true); yield return new WaitForSeconds(messageDisplayDuration); unlockMessageText.gameObject.SetActive(false); }

    public bool DropCurrentWeapon(out WeaponData dataToDrop, out GameObject modelToDrop)
    {
        dataToDrop = null; modelToDrop = null;
        if (currentWeapon == null) return false;
        dataToDrop = currentWeapon; modelToDrop = currentWeaponModel;
        currentWeapon = null; currentWeaponModel = null;
        if (modelToDrop != null) { modelToDrop.transform.SetParent(null); modelToDrop.SetActive(false); }
        UpdateAmmoUI(); ClearCrosshair(); isWeaponDropped = true; return true;
    }

    public void PickupUpgradedWeapon(WeaponData newWeaponData, GameObject model)
    {
        if (newWeaponData == null) return;
        EquipWeapon(newWeaponData); ForceCurrentWeaponAmmoToFull();
        currentWeaponModel = model; currentWeaponModel.transform.SetParent(weaponHolder);
        currentWeaponModel.transform.localPosition = Vector3.zero; currentWeaponModel.transform.localRotation = Quaternion.identity; currentWeaponModel.SetActive(true);
        isWeaponDropped = false;
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        if (newWeapon == null) return;
        SaveCurrentAmmoState();
        if (weaponSlots[0] == null) { weaponSlots[0] = newWeapon; currentSlotIndex = 0; }
        else if (weaponSlots[1] == null) { weaponSlots[1] = newWeapon; currentSlotIndex = 1; }
        else { weaponSlots[currentSlotIndex] = newWeapon; }
        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    private void RefreshWeaponVisuals(WeaponData weaponData)
    {
        if (currentWeaponModel != null) Destroy(currentWeaponModel);
        currentWeapon = weaponData; shotTicker = 0; lmgCurrentHeatTime = 0; StopAiming();
        if (currentWeapon == null) { if (crosshairImage != null) crosshairImage.enabled = false; if (ammoText != null) ammoText.text = ""; return; }
        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero; currentWeaponModel.transform.localRotation = Quaternion.identity; currentWeaponModel.transform.localScale = Vector3.one;
            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>(); muzzleLight = newMuzzleLight;
        }
        LoadAmmoStateForWeapon(currentWeapon); isReloading = false; UpdateAmmoUI(); UpdateCrosshair();
    }

    public int GetWeaponTypeInSlot(int slotIndex) { if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return -1; if (weaponSlots[slotIndex] != null) return (int)weaponSlots[slotIndex].weaponType; return -1; }
    public int GetCurrentSlotIndex() { return currentSlotIndex; }
    public void ForceWeaponToSlot(int slotIndex, WeaponData weapon) { if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return; weaponSlots[slotIndex] = weapon; if (slotIndex == currentSlotIndex) RefreshWeaponVisuals(weaponSlots[slotIndex]); }
    public void SelectSlot(int slotIndex) { if (slotIndex < 0 || slotIndex >= weaponSlots.Length) return; SaveCurrentAmmoState(); currentSlotIndex = slotIndex; RefreshWeaponVisuals(weaponSlots[currentSlotIndex]); }
    public void ClearInventory() { weaponSlots[0] = null; weaponSlots[1] = null; if (currentWeaponModel != null) Destroy(currentWeaponModel); currentWeapon = null; }
    private void SaveCurrentAmmoState() { if (currentWeapon != null && !isUltimateActive) { ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag; totalAmmoCache[currentWeapon.weaponType] = totalAmmo; } }
    private void LoadAmmoStateForWeapon(WeaponData weapon) { if (ammoInMagCache.ContainsKey(weapon.weaponType)) { currentAmmoInMag = ammoInMagCache[weapon.weaponType]; totalAmmo = totalAmmoCache[weapon.weaponType]; } else { currentAmmoInMag = weapon.magCapacity; totalAmmo = weapon.maxAmmo - currentWeapon.magCapacity; ammoInMagCache[weapon.weaponType] = currentAmmoInMag; totalAmmoCache[weapon.weaponType] = totalAmmo; } }
    public void ForceCurrentWeaponAmmoToFull() { if (currentWeapon == null) return; currentAmmoInMag = currentWeapon.magCapacity; totalAmmo = currentWeapon.maxAmmo - currentWeapon.magCapacity; SaveCurrentAmmoState(); UpdateAmmoUI(); }
    public bool IsAmmoFull(WeaponData weaponData) { if (weaponData == null) return true; int currentMag = 0; int currentTotal = 0; if (currentWeapon != null && weaponData.weaponType == currentWeapon.weaponType) { currentMag = currentAmmoInMag; currentTotal = totalAmmo; } else if (ammoInMagCache.ContainsKey(weaponData.weaponType)) { currentMag = ammoInMagCache[weaponData.weaponType]; currentTotal = totalAmmoCache[weaponData.weaponType]; } else return false; int maxMag = weaponData.magCapacity; int maxTotal = weaponData.maxAmmo - weaponData.magCapacity; return currentMag >= maxMag && currentTotal >= maxTotal; }
    public WeaponType GetEquippedWeaponType() { if (currentWeapon != null) return currentWeapon.weaponType; return (WeaponType)(-1); }
}