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

    [Header("Visuales de Mejora")]
    public Material upgradedWeaponMaterial;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private Image crosshairImage;

    private RectTransform crosshairRectTransform;
    private Color defaultAmmoColor;

    [Header("Sistema de Puntuación")]
    public int pointsPerHit = 10;
    public int pointsPerKillDisplay = 150;
    [SerializeField] private GameObject floatingTextPrefab;

    [Header("Ajustes Móviles")]
    public bool useAutoFire = true;
    public LayerMask obstacleLayers;

    // --- VARIABLES DE CONTROL MÓVIL ---
    private bool isMobileFiring = false;
    private bool isMobileAiming = false;
    private bool mobileTriggerPulled = false;

    // Esta es la variable que faltaba y daba error:
    [HideInInspector] public bool mobileInteractPressed = false;

    [Header("--- SISTEMA DE ULTIMATE ---")]
    [SerializeField] private WeaponData ultimateWeaponData;
    [SerializeField] private Image ultimateIconFill;
    [SerializeField] private GameObject ultimateReadyVisual;
    [SerializeField] private KeyCode ultimateKey = KeyCode.X;
    [SerializeField] private float ultimateDuration = 20f;
    [SerializeField] private GameObject ultimateUIParent;
    [SerializeField] private TextMeshProUGUI ultimateKillCountText;
    [SerializeField] private int totalCollectiblesRequired = 5;

    [Header("Coleccionables UI")]
    [SerializeField] private TextMeshProUGUI collectibleCountText;
    [SerializeField] private float collectibleDisplayTime = 3f;
    private Coroutine displayCollectibleCoroutine;

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
    private ParticleSystem flamethrowerParticles;

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

    [Header("Efectos")]
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
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

        if (!Application.isMobilePlatform && !Application.isEditor && GameManager.Instance == null)
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
            newInstance.name = weaponToEquip.name;
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
        HandleFlamethrowerEffects();

        if (isReloading || isMeleeAttacking) return;

        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        if (weaponHolder != null && currentWeapon != null)
        {
            Vector3 targetPosition = weaponInitialLocalPos;
            if (isAiming && currentWeapon.sniperScopeSprite == null)
                targetPosition = currentWeapon.aimPosition;

            Vector3 smoothPosition = Vector3.Lerp(weaponHolder.localPosition - weaponCurrentOffset, targetPosition, Time.deltaTime * adsSpeed);
            Quaternion targetRotation = Quaternion.Euler(weaponInitialLocalRot);
            if (isAiming && currentWeapon.sniperScopeSprite == null)
                targetRotation = Quaternion.Euler(currentWeapon.aimRotation);

            weaponHolder.localRotation = Quaternion.Slerp(weaponHolder.localRotation, targetRotation, Time.deltaTime * adsSpeed);
            weaponCurrentOffset = Vector3.Lerp(weaponCurrentOffset, Vector3.zero, Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed);
            weaponHolder.localPosition = smoothPosition + weaponCurrentOffset;
        }
    }

    public bool HasWeapon(WeaponType typeToCheck)
    {
        foreach (WeaponData weapon in weaponSlots)
        {
            if (weapon != null && weapon.weaponType == typeToCheck)
            {
                return true;
            }
        }
        return false;
    }

    public void RefillAmmoForType(WeaponType typeToRefill)
    {
        if (currentWeapon != null && currentWeapon.weaponType == typeToRefill)
        {
            currentAmmoInMag = currentWeapon.magCapacity;
            totalAmmo = currentWeapon.maxAmmo - currentWeapon.magCapacity;
            SaveCurrentAmmoState();
            UpdateAmmoUI();
            return;
        }

        foreach (WeaponData weapon in weaponSlots)
        {
            if (weapon != null && weapon.weaponType == typeToRefill)
            {
                int maxMag = weapon.magCapacity;
                int maxTotal = weapon.maxAmmo - weapon.magCapacity;

                ammoInMagCache[weapon.weaponType] = maxMag;
                totalAmmoCache[weapon.weaponType] = maxTotal;
                return;
            }
        }
    }

    public bool IsAmmoFullForType(WeaponData weaponData)
    {
        if (weaponData == null) return true;

        if (currentWeapon != null && currentWeapon.weaponType == weaponData.weaponType)
        {
            int maxTotal = currentWeapon.maxAmmo - currentWeapon.magCapacity;
            return currentAmmoInMag >= currentWeapon.magCapacity && totalAmmo >= maxTotal;
        }

        if (ammoInMagCache.ContainsKey(weaponData.weaponType))
        {
            int cachedMag = ammoInMagCache[weaponData.weaponType];
            int cachedTotal = totalAmmoCache[weaponData.weaponType];

            foreach (var w in weaponSlots)
            {
                if (w != null && w.weaponType == weaponData.weaponType)
                {
                    int realMaxTotal = w.maxAmmo - w.magCapacity;
                    return cachedMag >= w.magCapacity && cachedTotal >= realMaxTotal;
                }
            }
        }

        return false;
    }

    public void EquipWeapon(WeaponData newWeaponAsset)
    {
        if (newWeaponAsset == null) return;

        SaveCurrentAmmoState();

        WeaponData newWeaponInstance = Instantiate(newWeaponAsset);
        newWeaponInstance.name = newWeaponAsset.name;

        if (weaponSlots[0] == null)
        {
            weaponSlots[0] = newWeaponInstance;
            currentSlotIndex = 0;
        }
        else if (weaponSlots[1] == null)
        {
            weaponSlots[1] = newWeaponInstance;
            currentSlotIndex = 1;
        }
        else
        {
            weaponSlots[currentSlotIndex] = newWeaponInstance;
        }

        RefreshWeaponVisuals(weaponSlots[currentSlotIndex]);
    }

    public void RefreshCurrentWeapon()
    {
        if (currentWeapon != null)
        {
            RefreshWeaponVisuals(currentWeapon);
        }
    }

    private void RefreshWeaponVisuals(WeaponData weaponData)
    {
        if (currentWeaponModel != null) Destroy(currentWeaponModel);

        currentWeapon = weaponData;
        shotTicker = 0;
        lmgCurrentHeatTime = 0;
        flamethrowerParticles = null;
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

            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>();
            muzzleLight = newMuzzleLight;

            if (currentWeapon.weaponType == WeaponType.Flamethrower)
            {
                flamethrowerParticles = currentWeaponModel.GetComponentInChildren<ParticleSystem>();
                if (flamethrowerParticles != null) flamethrowerParticles.Stop();
            }

            if (currentWeapon.isUpgraded && upgradedWeaponMaterial != null)
            {
                Renderer[] renderers = currentWeaponModel.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer r in renderers)
                {
                    if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    {
                        Material[] newMats = new Material[r.materials.Length];
                        for (int i = 0; i < newMats.Length; i++)
                        {
                            newMats[i] = upgradedWeaponMaterial;
                        }
                        r.materials = newMats;
                    }
                }
            }

            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
            currentWeaponModel.transform.localScale = Vector3.one;
        }

        LoadAmmoStateForWeapon(currentWeapon);
        isReloading = false;
        UpdateAmmoUI();
        UpdateCrosshair();
    }

    void HandleFlamethrowerEffects()
    {
        if (currentWeapon != null && currentWeapon.weaponType == WeaponType.Flamethrower)
        {
            bool isPointerOverUI = false;
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject()) isPointerOverUI = true;
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                    isPointerOverUI = true;
            }

            bool mouseHeld = false;
            if (!Application.isMobilePlatform)
            {
                mouseHeld = Input.GetMouseButton(0) && !isPointerOverUI;
            }

            bool fireInputHeld = mouseHeld || isMobileFiring || (useAutoFire && IsAimingAtEnemy());
            bool canFire = fireInputHeld && !isReloading && currentAmmoInMag > 0;

            if (flamethrowerParticles != null)
            {
                if (canFire)
                {
                    if (!flamethrowerParticles.isPlaying) flamethrowerParticles.Play();
                }
                else
                {
                    if (flamethrowerParticles.isPlaying) flamethrowerParticles.Stop();
                }
            }

            if (audioSource != null && currentWeapon.shootSound != null)
            {
                if (canFire)
                {
                    if (!audioSource.isPlaying || audioSource.clip != currentWeapon.shootSound)
                    {
                        audioSource.clip = currentWeapon.shootSound;
                        audioSource.loop = true;
                        audioSource.Play();
                    }
                }
                else
                {
                    if (audioSource.isPlaying && audioSource.clip == currentWeapon.shootSound)
                    {
                        audioSource.Stop();
                        audioSource.loop = false;
                        audioSource.clip = null;
                    }
                }
            }
        }
    }

    void HandleLMGHeat()
    {
        if (currentWeapon == null) return;

        if (currentWeapon.weaponType == WeaponType.RPD && currentWeapon.isUpgraded)
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
                if (heatFactor > 0.5f)
                    ammoText.color = Color.Lerp(defaultAmmoColor, Color.red, heatFactor);
                else if (!isUltimateActive)
                    ammoText.color = defaultAmmoColor;
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

        if (currentWeapon.weaponType == WeaponType.RPD && currentWeapon.isUpgraded)
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

        bool isPointerOverUI = false;
        if (EventSystem.current != null)
        {
            if (EventSystem.current.IsPointerOverGameObject()) isPointerOverUI = true;
            if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId))
                isPointerOverUI = true;
        }

        bool mouseInput = false;
        bool mouseInputDown = false;

        if (!Application.isMobilePlatform)
        {
            mouseInput = Input.GetMouseButton(0) && !isPointerOverUI;
            mouseInputDown = Input.GetMouseButtonDown(0) && !isPointerOverUI;
        }

        bool enemyInSight = false;
        if (useAutoFire && !isPointerOverUI && !isReloading)
        {
            enemyInSight = IsAimingAtEnemy();
        }

        bool fireInputHeld = mouseInput || isMobileFiring || enemyInSight;
        bool fireInputDownCombined = mouseInputDown || mobileTriggerPulled;

        switch (currentWeapon.weaponType)
        {
            case WeaponType.Glock:
            case WeaponType.Remington:
            case WeaponType.HuntingShotgun:
            case WeaponType.L11:
            case WeaponType.SVU:
            case WeaponType.Fal:
            case WeaponType.M14:

                bool becomesAuto = currentWeapon.isUpgraded && (currentWeapon.weaponType == WeaponType.Glock || currentWeapon.weaponType == WeaponType.Fal);

                if (becomesAuto)
                {
                    if (fireInputHeld && Time.time >= nextFireTime)
                    {
                        nextFireTime = Time.time + currentWeapon.fireRate;
                        Shoot();
                    }
                }
                else
                {
                    if ((fireInputDownCombined || (enemyInSight && useAutoFire)) && Time.time >= nextFireTime && !isBursting)
                    {
                        nextFireTime = Time.time + currentWeapon.fireRate;

                        if (currentWeapon.weaponType == WeaponType.Remington || currentWeapon.weaponType == WeaponType.HuntingShotgun)
                            StartCoroutine(ShootShotgunCoroutine());
                        else if (currentWeapon.weaponType == WeaponType.L11 || currentWeapon.weaponType == WeaponType.SVU)
                            StartCoroutine(ShootSniperCoroutine());
                        else
                            Shoot();
                    }
                }
                break;

            case WeaponType.AK47:
            case WeaponType.M4A1:
            case WeaponType.MTAR:
            case WeaponType.UZI:
            case WeaponType.Mp7:
            case WeaponType.RPD:
            case WeaponType.AA12:
            case WeaponType.Flamethrower:
                if (fireInputHeld && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;

                    if (currentWeapon.weaponType == WeaponType.Flamethrower) ShootFlamethrower();
                    else if (currentWeapon.weaponType == WeaponType.AA12) StartCoroutine(ShootShotgunCoroutine());
                    else ShootRifle();
                }
                break;
        }
        mobileTriggerPulled = false;
    }

    public void SetMobileFiring(bool isFiring)
    {
        if (isFiring && !isMobileFiring)
        {
            mobileTriggerPulled = true;
        }
        isMobileFiring = isFiring;
    }

    // --- ESTA ES LA FUNCIÓN QUE FALTABA Y DABA ERROR ---
    public void MobilePressInteract()
    {
        mobileInteractPressed = true;
        StartCoroutine(ResetMobileInteract());
    }

    private IEnumerator ResetMobileInteract()
    {
        yield return null;
        mobileInteractPressed = false;
    }
    // --------------------------------------------------

    public void MobileToggleAim()
    {
        isMobileAiming = !isMobileAiming;
    }

    public void MobileFireOnce()
    {
        if (currentWeapon == null) return;

        if (Time.time >= nextFireTime && !isBursting && !isReloading)
        {
            bool isSemi = (currentWeapon.weaponType == WeaponType.Glock && !currentWeapon.isUpgraded) ||
                          (currentWeapon.weaponType == WeaponType.Fal && !currentWeapon.isUpgraded) ||
                          currentWeapon.weaponType == WeaponType.Remington ||
                          currentWeapon.weaponType == WeaponType.HuntingShotgun ||
                          currentWeapon.weaponType == WeaponType.L11 ||
                          currentWeapon.weaponType == WeaponType.SVU ||
                          currentWeapon.weaponType == WeaponType.M14;

            if (isSemi)
            {
                nextFireTime = Time.time + currentWeapon.fireRate;

                if (currentWeapon.weaponType == WeaponType.Remington || currentWeapon.weaponType == WeaponType.HuntingShotgun)
                    StartCoroutine(ShootShotgunCoroutine());
                else if (currentWeapon.weaponType == WeaponType.L11 || currentWeapon.weaponType == WeaponType.SVU)
                    StartCoroutine(ShootSniperCoroutine());
                else
                    Shoot();
            }
        }
    }

    public void MobileReload()
    {
        if (!isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0)
            StartCoroutine(ReloadCoroutine());
    }

    void Shoot()
    {
        if (currentAmmoInMag <= 0)
        {
            HandleEmptyClip();
            return;
        }

        FireBaseLogic();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            HandleHit(hit, GetCurrentWeaponDamage());
        }
        ApplyRecoil();
    }

    IEnumerator BurstFire()
    {
        if (isBursting) yield break;

        isBursting = true;
        int burstCount = 3;

        for (int i = 0; i < burstCount; i++)
        {
            if (currentAmmoInMag <= 0)
            {
                HandleEmptyClip();
                break;
            }

            FireBaseLogic();

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            {
                HandleHit(hit, GetCurrentWeaponDamage());
            }
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
            HandleEmptyClip();
            return;
        }

        FireBaseLogic();

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            HandleHit(hit, GetCurrentWeaponDamage());
        }
        ApplyRecoil();
    }

    IEnumerator ShootShotgunCoroutine()
    {
        if (currentAmmoInMag <= 0)
        {
            HandleEmptyClip();
            yield break;
        }

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
            {
                HandleHit(hit, GetCurrentWeaponDamage());
            }
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
            HandleEmptyClip();
            yield break;
        }

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
                else
                {
                    HandleHit(hit, GetCurrentWeaponDamage());
                }
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
        // Lanzamos un SphereCast (un rayo gordo) para quemar en área
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.SphereCastAll(
            ray.origin,
            currentWeapon.flameRadius,
            ray.direction,
            currentWeapon.range,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        // Usamos un HashSet para no dañar al mismo zombi dos veces en el mismo frame
        HashSet<ZombieController> burnedZombies = new HashSet<ZombieController>();

        foreach (RaycastHit hit in hits)
        {
            ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
            ZombieController zombie = (hitbox != null) ? hitbox.zombieController : hit.collider.GetComponent<ZombieController>();

            if (zombie != null)
            {
                if (burnedZombies.Add(zombie))
                {
                    HandleHit(hit, currentWeapon.damage * damageMultiplier);
                }
            }
        }
    }

    void HandleHit(RaycastHit hit, float damage)
    {
        Palomas collectible = hit.collider.GetComponent<Palomas>();
        if (collectible != null)
        {
            collectible.TakeDamage(damage * damageMultiplier);
            SpawnImpactEffects(hit);
            return;
        }

        ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
        ZombieController zombieHealth = (hitbox != null) ? hitbox.zombieController : hit.collider.GetComponent<ZombieController>();

        if (zombieHealth != null)
        {
            if (zombieHealth.GetHP() <= 0)
            {
                SpawnImpactEffects(hit);
                return;
            }

            if (currentWeapon.weaponType != WeaponType.Flamethrower && ScoreManager.Instance != null)
                ScoreManager.Instance.AddScore(pointsPerHit);

            if (hitbox != null)
                zombieHealth.TakeDamage(damage, hitbox.hitboxType);
            else
                zombieHealth.TakeDamage(damage);

            if (currentWeapon.causesSlow)
                zombieHealth.ApplySlow(currentWeapon.slowAmount, currentWeapon.slowDuration);

            if (currentWeapon.causesKnockback)
            {
                Vector3 pushDirection = (zombieHealth.transform.position - transform.position).normalized;
                pushDirection.y = 0;
                zombieHealth.ApplyKnockback(pushDirection, currentWeapon.knockbackForce);
            }

            if (zombieHealth.GetHP() <= 0)
            {
                ShowFloatingScore(hit.point, pointsPerKillDisplay);

                if ((currentWeapon.weaponType == WeaponType.UZI || currentWeapon.weaponType == WeaponType.Mp7) && currentWeapon.isUpgraded)
                {
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
            if (textScript != null) textScript.Setup(points);
        }
    }

    void FireBaseLogic()
    {
        PlaySound(currentWeapon.shootSound);
        currentAmmoInMag--;
        UpdateAmmoUI();
        if (currentWeapon.weaponType != WeaponType.Flamethrower)
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
            if (ammoText != null)
            {
                ammoText.text = "SIN MUNICIÓN";
                ammoText.color = Color.red;
            }
        }
    }

    private void SpawnImpactEffects(RaycastHit hit)
    {
        GameObject particlePrefab = null;
        Sprite decalSprite = null;

        if (hit.collider.CompareTag("Zombie"))
            particlePrefab = bloodParticlePrefab;
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
        if (weaponHolder != null)
            weaponCurrentOffset = new Vector3(0, 0, -currentWeapon.weaponKickbackDistance);
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
                if (currentWeapon.weaponType == WeaponType.RPD && currentWeapon.isUpgraded) { }
                else if (currentAmmoInMag == 0 && totalAmmo == 0) ammoText.color = Color.red;
                else ammoText.color = defaultAmmoColor;
            }
        }
        else
        {
            ammoText.text = "";
            ammoText.color = defaultAmmoColor;
        }
    }

    private void ClearCrosshair()
    {
        if (crosshairImage != null) crosshairImage.enabled = false;
    }

    public void SetCrosshair(Vector2 size, Vector2 aimedSize)
    {
        if (crosshairRectTransform == null) return;
        crosshairRectTransform.sizeDelta = size;
        crosshairImage.enabled = true;
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

    void HandleReloadInput()
    {
        if (isUltimateActive) return;
        if (currentWeapon == null) return;
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0)
            StartCoroutine(ReloadCoroutine());
    }

    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        PlaySound(currentWeapon.reloadSound);
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

        if (currentWeapon.weaponType == WeaponType.Remington || currentWeapon.weaponType == WeaponType.HuntingShotgun)
        {
            float timePerBullet = 0f;
            if (ammoToLoad > 0)
            {
                timePerBullet = currentWeapon.reloadTime / currentWeapon.magCapacity;
            }

            for (int i = 0; i < ammoToLoad; i++)
            {
                if (currentWeapon.shellInsertSound != null)
                {
                    PlaySound(currentWeapon.shellInsertSound);
                }

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
        UpdateAmmoUI();
    }

    void HandleAiming()
    {
        if (currentWeapon == null || !currentWeapon.canAim)
        {
            if (isAiming) StopAiming();
            return;
        }

        bool aimInput = false;

        if (Application.isMobilePlatform)
        {
            aimInput = isMobileAiming;
        }
        else
        {
            aimInput = Input.GetMouseButton(1) || isMobileAiming;
        }

        if (aimInput) isAiming = true; else isAiming = false;

        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        if (playerCamera != null)
        {
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);
        }

        if (cameraController != null)
        {
            if (isAiming) cameraController.SetSensitivityMultiplier(aimSensitivityMultiplier);
            else cameraController.SetSensitivityMultiplier(1f);
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

            if (crosshairImage != null)
            {
                crosshairImage.sprite = currentWeapon.sniperScopeSprite;
                crosshairImage.enabled = true;
            }

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

            if (isAiming)
            {
                if (crosshairImage != null) crosshairImage.enabled = false;
            }
            else
            {
                UpdateCrosshair();
            }
        }
    }

    public void StopAiming()
    {
        isAiming = false;
        isMobileAiming = false;
        if (cameraController != null) cameraController.SetSensitivityMultiplier(1f);
        if (playerCamera != null) playerCamera.fieldOfView = defaultFOV;
        if (weaponHiddenForScope)
        {
            if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
            weaponHiddenForScope = false;
        }
        UpdateCrosshair();
    }

    void HandleMeleeInput()
    {
        if (Input.GetKeyDown(meleeKey) && !isReloading && !isUltimateActive && currentWeapon != null)
            StartCoroutine(QuickMeleeRoutine());
    }

    private IEnumerator QuickMeleeRoutine()
    {
        if (knifeGameObject == null) yield break;

        isMeleeAttacking = true;
        isReloading = false;
        isBursting = false;
        StopAiming();

        Vector3 savedWeaponPos = Vector3.zero;
        Quaternion savedWeaponRot = Quaternion.identity;

        if (currentWeaponModel != null)
        {
            savedWeaponPos = currentWeaponModel.transform.localPosition;
            savedWeaponRot = currentWeaponModel.transform.localRotation;
            currentWeaponModel.SetActive(false);
        }

        if (crosshairImage != null) crosshairImage.enabled = false;
        if (cameraController != null) cameraController.enabled = false;

        knifeGameObject.SetActive(true);
        if (knifeAnimator != null) knifeAnimator.SetTrigger(meleeAnimationName);

        yield return new WaitForSeconds(damageDelay);

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, knifeRange))
            HandleHit(hit, knifeDamage);

        yield return new WaitForSeconds(meleeDuration - damageDelay);

        knifeGameObject.SetActive(false);

        if (currentWeaponModel != null)
        {
            currentWeaponModel.SetActive(true);
            currentWeaponModel.transform.localPosition = savedWeaponPos;
            currentWeaponModel.transform.localRotation = savedWeaponRot;
        }

        if (cameraController != null) cameraController.enabled = true;
        if (crosshairImage != null) crosshairImage.enabled = true;

        isMeleeAttacking = false;
    }

    void HandleUltimateLogic()
    {
        if (!isUltimateUnlocked)
        {
            if (ultimateUIParent != null && ultimateUIParent.activeSelf)
                ultimateUIParent.SetActive(false);
            return;
        }

        if (ultimateUIParent != null && !ultimateUIParent.activeSelf)
            ultimateUIParent.SetActive(true);

        if (isUltimateActive) return;
        if (ultimateWeaponData == null) return;

        float percentage = 0f;
        if (ultimateWeaponData.requiredKillsForUlt > 0)
            percentage = (float)currentKillsCount / ultimateWeaponData.requiredKillsForUlt;

        if (ultimateIconFill != null) ultimateIconFill.fillAmount = percentage;

        if (currentKillsCount >= ultimateWeaponData.requiredKillsForUlt)
        {
            ultimateIsReady = true;
            if (ultimateReadyVisual != null && !ultimateReadyVisual.activeSelf)
                ultimateReadyVisual.SetActive(true);
            if (ultimateIconFill != null) ultimateIconFill.fillAmount = 1f;

            if (Input.GetKeyDown(ultimateKey)) StartCoroutine(ActivateUltimateRoutine());
        }
        else
        {
            ultimateIsReady = false;
            if (ultimateReadyVisual != null && ultimateReadyVisual.activeSelf)
                ultimateReadyVisual.SetActive(false);
        }
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
        lmgCurrentHeatTime = 0;
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

        Vector3 targetUpPosition = startPosition + switchUpOffset + (Vector3.back * switchBackDistance.z);
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

    public void RegisterZombieKill()
    {
        if (isUltimateActive) return;
        if (!isUltimateUnlocked) return;

        if (ultimateWeaponData != null)
        {
            if (currentKillsCount < ultimateWeaponData.requiredKillsForUlt)
                currentKillsCount++;
        }
        UpdateUltimateUI();
    }

    IEnumerator ActivateUltimateRoutine()
    {
        isUltimateActive = true;
        ultimateIsReady = false;

        if (ultimateReadyVisual != null) ultimateReadyVisual.SetActive(false);

        preUltSlotIndex = currentSlotIndex;
        RefreshWeaponVisuals(ultimateWeaponData);

        currentAmmoInMag = 9999;
        totalAmmo = 9999;
        UpdateAmmoUI();

        currentKillsCount = 0;
        UpdateUltimateUI();

        float timer = ultimateDuration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            if (ultimateIconFill != null) ultimateIconFill.fillAmount = timer / ultimateDuration;
            yield return null;
        }

        isUltimateActive = false;
        if (ultimateIconFill != null) ultimateIconFill.fillAmount = 0f;

        SelectSlot(preUltSlotIndex);
        UpdateUltimateUI();
    }

    public void RegisterCollectibleFound()
    {
        if (isUltimateUnlocked) return;

        collectiblesFoundCount++;
        UpdateCollectibleDisplay();

        if (collectiblesFoundCount >= totalCollectiblesRequired && !isUltimateUnlocked)
            UnlockUltimatePermanently();
    }

    private void UnlockUltimatePermanently()
    {
        isUltimateUnlocked = true;

        if (ultimateUIParent != null) ultimateUIParent.SetActive(true);
        if (ultimateWeaponData != null) currentKillsCount = ultimateWeaponData.requiredKillsForUlt;

        UpdateUltimateUI();

        if (unlockMessageText != null)
        {
            StopCoroutine("DisplayUnlockMessageRoutine");
            StartCoroutine("DisplayUnlockMessageRoutine");
        }
    }

    private void UpdateUltimateUI()
    {
        if (ultimateWeaponData == null) return;

        if (!isUltimateUnlocked)
        {
            if (ultimateUIParent != null) ultimateUIParent.SetActive(false);
            return;
        }

        if (ultimateUIParent != null && !ultimateUIParent.activeSelf)
            ultimateUIParent.SetActive(true);

        int requiredKills = ultimateWeaponData.requiredKillsForUlt;
        float fillAmount = requiredKills > 0 ? (float)currentKillsCount / requiredKills : 1f;

        if (ultimateIconFill != null) ultimateIconFill.fillAmount = fillAmount;

        ultimateIsReady = currentKillsCount >= requiredKills;

        if (ultimateReadyVisual != null) ultimateReadyVisual.SetActive(ultimateIsReady);
        if (ultimateKillCountText != null)
            ultimateKillCountText.text = ultimateIsReady ? $"LISTO" : $"{currentKillsCount}/{requiredKills}";
    }

    public void UpdateCollectibleDisplay()
    {
        if (collectibleCountText == null) return;

        collectibleCountText.text = $"{collectiblesFoundCount} / {totalCollectiblesRequired}";

        if (displayCollectibleCoroutine != null) StopCoroutine(displayCollectibleCoroutine);
        displayCollectibleCoroutine = StartCoroutine(DisplayCollectibleRoutine());
    }

    private IEnumerator DisplayCollectibleRoutine()
    {
        if (collectibleCountText != null) collectibleCountText.gameObject.SetActive(true);
        yield return new WaitForSeconds(collectibleDisplayTime);
        if (collectibleCountText != null) collectibleCountText.gameObject.SetActive(false);
        displayCollectibleCoroutine = null;
    }

    private IEnumerator DisplayUnlockMessageRoutine()
    {
        string key = ultimateKey.ToString();
        unlockMessageText.text = $"¡Has desbloqueado el arma especial! Pulsa [{key}] para utilizarla";
        unlockMessageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(messageDisplayDuration);
        unlockMessageText.gameObject.SetActive(false);
    }

    public bool DropCurrentWeapon(out WeaponData dataToDrop, out GameObject modelToDrop)
    {
        dataToDrop = null;
        modelToDrop = null;

        if (currentWeapon == null) return false;

        dataToDrop = currentWeapon;
        modelToDrop = currentWeaponModel;

        currentWeapon = null;
        currentWeaponModel = null;

        if (modelToDrop != null)
        {
            modelToDrop.transform.SetParent(null);
            modelToDrop.SetActive(false);
        }

        UpdateAmmoUI();
        ClearCrosshair();
        isWeaponDropped = true;
        return true;
    }

    public void PickupUpgradedWeapon(WeaponData newWeaponData, GameObject model)
    {
        if (newWeaponData == null) return;

        EquipWeapon(newWeaponData);
        ForceCurrentWeaponAmmoToFull();

        currentWeaponModel = model;
        currentWeaponModel.transform.SetParent(weaponHolder);
        currentWeaponModel.transform.localPosition = Vector3.zero;
        currentWeaponModel.transform.localRotation = Quaternion.identity;
        currentWeaponModel.SetActive(true);

        isWeaponDropped = false;
    }

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
        if (slotIndex == currentSlotIndex) RefreshWeaponVisuals(weaponSlots[slotIndex]);
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
}