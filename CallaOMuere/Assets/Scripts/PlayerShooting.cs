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
    [SerializeField] private TextMeshProUGUI ammoText;     // Texto del HUD de munición
    [SerializeField] private Image crosshairImage;         // Imagen de la mira (crosshair)

    private RectTransform crosshairRectTransform;

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

    // --- APUNTADO ---
    [Header("Apuntado (ADS)")]
    [SerializeField] private float adsSpeed = 10f; // Velocidad de transición al apuntar
    [SerializeField] private float defaultFOV = 60f; // FOV normal de la cámara
    private bool isAiming = false;
    private bool weaponHiddenForScope = false; // Bandera para saber si el arma está oculta

    [Header("Animación de Recarga")]
    [SerializeField] private Vector3 reloadRotation = new Vector3(35f, 0f, 0f);
    [SerializeField] private float reloadAnimSpeed = 8f;

    private Vector3 weaponInitialLocalRot;

    void Start()
    {
        if (crosshairImage != null) crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();

        if (weaponHolder != null) weaponInitialLocalPos = weaponHolder.localPosition;
        if (weaponHolder != null) weaponInitialLocalRot = weaponHolder.localEulerAngles;

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

        playerCamera.fieldOfView = defaultFOV;
    }

    void Update()
    {
        if (isReloading) return;

        HandleShooting();
        HandleReloadInput();

        HandleAiming();

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

    // === APUNTADO ===
    void HandleAiming()
    {
        if (currentWeapon == null || !currentWeapon.canAim)
        {
            // Si el arma no puede apuntar o no hay arma, asegura que no estamos apuntando
            if (isAiming) StopAiming();
            return;
        }

        if (Input.GetButtonDown("Fire2")) // Clic derecho
        {
            isAiming = true;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            isAiming = false;
        }

        // Transición del FOV
        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);

        // --- Lógica de la Mirilla y Ocultar Arma ---
        if (isAiming && currentWeapon.sniperScopeSprite != null)
        {
            if (crosshairRectTransform != null)
            {
                
                crosshairRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                crosshairRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                crosshairRectTransform.pivot = new Vector2(0.5f, 0.5f);
                crosshairRectTransform.anchoredPosition = Vector2.zero;

                // 2. Aplica el TAMAÑO DE APUNTADO
                crosshairRectTransform.sizeDelta = currentWeapon.aimedCrosshairSize;
            }

            crosshairImage.sprite = currentWeapon.sniperScopeSprite;
            crosshairImage.enabled = true; // Asegura que la mirilla del scope está visible

            // Oculta el arma 3D si se usa una mirilla de francotirador
            if (!weaponHiddenForScope)
            {
                if (currentWeaponModel != null) currentWeaponModel.SetActive(false);
                weaponHiddenForScope = true;
            }
        }
        else
        {
            // Si no estamos apuntando con mirilla de sniper, mostrar crosshair normal
            if (weaponHiddenForScope) // Si estaba oculta, vuelve a mostrarla
            {
                if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
                weaponHiddenForScope = false;
            }
            UpdateCrosshair(); // Vuelve a la mirilla normal
        }
    }

    // --- dejamos de apuntar ---
    public void StopAiming()
    {
        isAiming = false;
        playerCamera.fieldOfView = defaultFOV; // Resetea instantáneamente el FOV
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

        float reloadTime = currentWeapon.reloadTime * reloadTimeMultiplier;
        float animTime = 1f / reloadAnimSpeed;

        // 🔽 1. Animación: inclinar arma hacia abajo
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

        // 🕒 2. Esperar la recarga (menos lo ya usado por la animación)
        float waitTime = Mathf.Max(0, reloadTime - animTime * 2f);
        yield return new WaitForSeconds(waitTime);

        // 🔼 3. Volver a rotación original
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

        // --- Lógica original de recarga ---
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
            case WeaponType.Sniper:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    ShootSniper();
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

    // === DISPARO SNIPER ===
    void ShootSniper()
    {
        // 1. Comprobar munición
        if (currentAmmoInMag <= 0)
        {
            if (ammoText != null) ammoText.text = "R para recargar";
            return;
        }

        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());
        ApplyRecoil();

        // 2. Lógica de Raycast
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, currentWeapon.range);

        // Si no golpea nada, no sigas
        if (hits.Length == 0) return;

        // 3. Ordenar los impactos por distancia
        var sortedHits = hits.OrderBy(h => h.distance);

        // 4. Procesar los impactos (CON LÓGICA ANTI-DUPLICADOS)

        // ¡NUEVO! Un Set para guardar los zombies que ya hemos golpeado EN ESTE DISPARO
        HashSet<ZombieController> alreadyDamaged = new HashSet<ZombieController>();
        int targetsHit = 0; // El contador de penetración

        foreach (var hit in sortedHits)
        {
            // Obtenemos el componente de salud ANTES de llamar a HandleHit
            ZombieController zombieHealth = hit.collider.GetComponent<ZombieController>();

            // ¿Es un zombie?
            if (zombieHealth != null)
            {
                // ¿Es un zombie que NO hemos golpeado ya?
                if (!alreadyDamaged.Contains(zombieHealth))
                {
                    // ¡Es un objetivo nuevo!
                    // 1. Llama a HandleHit para aplicar daño y efectos de agujero
                    HandleHit(hit, currentWeapon.damage * damageMultiplier);

                    // 2. Añádelo al Set para no volver a golpearlo
                    alreadyDamaged.Add(zombieHealth);

                    // 3. Suma al contador de penetración
                    targetsHit++;

                    // 4. Comprueba si hemos alcanzado el límite de penetración
                    if (targetsHit >= currentWeapon.penetrationCount)
                    {
                        break; // Deja de atravesar, has alcanzado el límite
                    }
                }
                // Si SÍ lo contenía, no hace nada y el rayo "atraviesa" gratis
            }
            else
            {
                // No es un zombie (es una pared, suelo, etc.)
                // Llama a HandleHit para poner el agujero de bala
                HandleHit(hit, currentWeapon.damage * damageMultiplier);

                // IMPORTANTE: Si quieres que la bala se detenga en las paredes
                // (y no atraviese paredes para golpear a un zombie detrás),
                // añade 'break;' en la línea de abajo.

                // break; // <-- Descomenta esto si las balas no deben atravesar muros
            }
        }
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
        // Usamos la referencia guardada 'crosshairRectTransform'
        if (crosshairRectTransform == null) return;

        if (currentWeapon != null && currentWeapon.crosshairIcon != null)
        {
            // 1. Pone el Sprite
            crosshairImage.sprite = currentWeapon.crosshairIcon;

            // 2. Prepara el RectTransform para TAMAÑO DEFINIDO (centrado)
            crosshairRectTransform.anchorMin = new Vector2(0.5f, 0.5f); // Centro
            crosshairRectTransform.anchorMax = new Vector2(0.5f, 0.5f); // Centro
            crosshairRectTransform.pivot = new Vector2(0.5f, 0.5f); // Centro
            crosshairRectTransform.anchoredPosition = Vector2.zero; // Posición 0,0 en el centro

            // 3. Aplica el TAMAÑO del arma
            crosshairRectTransform.sizeDelta = currentWeapon.crosshairSize;

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