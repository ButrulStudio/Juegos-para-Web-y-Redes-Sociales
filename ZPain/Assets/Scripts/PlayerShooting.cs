using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting; // Nota: Esta directiva 'using' parece no utilizarse.

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
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;

    private bool isBursting = false; // Flag para evitar disparar ráfagas múltiples.

    private Vector3 weaponInitialLocalPos; // Posición original del weaponHolder.
    private Vector3 weaponCurrentOffset; // Desplazamiento actual por el kickback.

    // === MUNICIÓN ===
    private int currentAmmoInMag; // Balas en el cargador actual.
    private int totalAmmo; // Balas en la reserva.
    private bool isReloading = false; // Flag para bloquear acciones mientras se recarga.

    // Caché para guardar la munición de las armas que no están equipadas.
    private Dictionary<WeaponType, int> ammoInMagCache = new Dictionary<WeaponType, int>();
    private Dictionary<WeaponType, int> totalAmmoCache = new Dictionary<WeaponType, int>();

    // === MULTIPLICADORES DE POWER-UP ===
    [HideInInspector] public float reloadTimeMultiplier = 1f; // Modifica la velocidad de recarga.
    [HideInInspector] public float damageMultiplier = 1f; // Modifica el daño.

    [Header("Muzzle Flash")]
    [Tooltip("Arrastra aquí el componente Light (Point Light) del cañón del arma equipada.")]
    private Light muzzleLight; // Luz del fogonazo (se obtiene del prefab).

    [Tooltip("Duración en segundos del fogonazo. 0.05 es un buen valor para empezar.")]
    [SerializeField] private float flashDuration = 0.05f;

    // --- APUNTADO ---
    [Header("Apuntado (ADS)")]
    [SerializeField] private float adsSpeed = 10f; // Velocidad de transición al apuntar.
    [SerializeField] private float defaultFOV = 60f; // FOV normal de la cámara.
    private bool isAiming = false; // Flag de estado de apuntado.
    private bool weaponHiddenForScope = false; // Flag para ocultar el arma al usar miras de sniper.

    [Header("Animación de Recarga")]
    [SerializeField] private Vector3 reloadRotation = new Vector3(35f, 0f, 0f); // Rotación para la animación falsa.
    [SerializeField] private float reloadAnimSpeed = 8f; // Velocidad de la animación falsa.

    private Vector3 weaponInitialLocalRot; // Rotación original del weaponHolder.

    //======AUDIO=======
    [SerializeField] private AudioSource audioSource; // AudioSource para sonidos de disparo/recarga.

    // Awake() se ejecuta ANTES que cualquier Start().
    // Ideal para inicializar referencias.
    void Awake()
    {
        if (crosshairImage != null)
            crosshairRectTransform = crosshairImage.GetComponent<RectTransform>();

        if (ammoText != null)
        {
            // Guardamos el color por defecto antes de que se modifique.
            defaultAmmoColor = ammoText.color;
        }

        if (weaponHolder != null)
        {
            // Guardamos las posiciones/rotaciones iniciales para el recoil y la recarga.
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

    /// <summary>
    /// Llamado por GameManager al iniciar una partida nueva.
    /// Establece el arma inicial.
    /// </summary>
    public void InitializeNewGame(WeaponData weaponToEquip)
    {
        if (weaponToEquip != null)
        {
            // Instanciamos el ScriptableObject para evitar modificar el asset original.
            currentWeapon = Instantiate(weaponToEquip);
            EquipWeapon(currentWeapon); // Equipa el arma.
            WeaponStore.RegisterStartingWeapon(currentWeapon); // Informa a la tienda que ya la tenemos.
        }
        else
        {
            Debug.LogError("InitializeNewGame fue llamado pero el weaponToEquip era nulo.");
        }
    }

    void Update()
    {
        // Guard clauses: No hacer nada si el juego está pausado o si estamos recargando.
        if (GameManager.IsPaused || GameManager.GameIsOver)
            return;

        if (isReloading) return;

        // Manejadores de Input.
        HandleShooting();
        HandleReloadInput();
        HandleAiming();

        // Lógica de recuperación del kickback (visual).
        if (weaponHolder != null && currentWeapon != null)
        {
            // Interpola suavemente el offset del arma de vuelta a cero.
            weaponCurrentOffset = Vector3.Lerp(
                weaponCurrentOffset,
                Vector3.zero,
                Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed
            );
            weaponHolder.localPosition = weaponInitialLocalPos + weaponCurrentOffset;
        }
    }

    // === APUNTADO ===

    /// <summary>
    /// Maneja el input y la lógica de apuntar (ADS).
    /// </summary>
    void HandleAiming()
    {
        // Si el arma no puede apuntar, fuerza a dejar de apuntar.
        if (currentWeapon == null || !currentWeapon.canAim)
        {
            if (isAiming) StopAiming();
            return;
        }

        // Detecta el input de apuntado (Clic derecho).
        if (Input.GetButtonDown("Fire2"))
        {
            isAiming = true;
        }
        if (Input.GetButtonUp("Fire2"))
        {
            isAiming = false;
        }

        // Interpola suavemente el FOV de la cámara.
        float targetFOV = isAiming ? currentWeapon.aimedFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * adsSpeed);

        // Lógica específica para miras de francotirador.
        if (isAiming && currentWeapon.sniperScopeSprite != null)
        {
            // Configura la imagen de la mira para que ocupe toda la pantalla.
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

            // Oculta el modelo del arma para que no tape la mira.
            if (!weaponHiddenForScope)
            {
                if (currentWeaponModel != null) currentWeaponModel.SetActive(false);
                weaponHiddenForScope = true;
            }
        }
        else
        {
            // Si no está apuntando con mira, restaura el arma y la mira normal.
            if (weaponHiddenForScope)
            {
                if (currentWeaponModel != null) currentWeaponModel.SetActive(true);
                weaponHiddenForScope = false;
            }
            UpdateCrosshair(); // Restaura la mira normal.
        }
    }

    /// <summary>
    /// Forza a salir del modo ADS y restaura el FOV y la mira.
    /// </summary>
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

    /// <summary>
    /// Detecta el input para iniciar la recarga.
    /// </summary>
    void HandleReloadInput()
    {
        if (currentWeapon == null) return;
        // Comprueba si se pulsa 'R', si no está ya recargando,
        // si el cargador no está lleno y si tiene munición en la reserva.
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmoInMag < currentWeapon.magCapacity && totalAmmo > 0)
        {
            StartCoroutine(ReloadCoroutine());
        }
    }

    /// <summary>
    /// Corrutina que maneja la lógica de recarga (animación, tiempos y munición).
    /// </summary>
    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        PlaySound(currentWeapon.reloadSound);

        // Aplica el multiplicador de PowerUp al tiempo de recarga.
        float reloadTime = currentWeapon.reloadTime * reloadTimeMultiplier;
        float animTime = 1f / reloadAnimSpeed; // Tiempo de la animación de rotación.

        // Animación de "bajada" del arma.
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

        // Calcula cuánta munición se necesita y cuánta se puede cargar.
        int neededAmmo = currentWeapon.magCapacity - currentAmmoInMag;
        int ammoToLoad = Mathf.Min(neededAmmo, totalAmmo);

        // Tiempo de espera (el tiempo real de recarga menos las animaciones).
        float waitTime = Mathf.Max(0, reloadTime - animTime * 2f);

        // Lógica especial para escopetas (recarga bala por bala).
        if (currentWeapon.weaponType == WeaponType.Shotgun && ammoToLoad > 0)
        {
            float timePerBullet = (waitTime > 0 && ammoToLoad > 0) ? waitTime / ammoToLoad : 0;

            for (int i = 0; i < ammoToLoad; i++)
            {
                if (timePerBullet > 0)
                    yield return new WaitForSeconds(timePerBullet);

                currentAmmoInMag++;
                totalAmmo--;
                UpdateAmmoUI(); // Actualiza la UI por cada bala.
            }
        }
        else // Lógica de recarga normal (todas las balas a la vez).
        {
            if (waitTime > 0)
                yield return new WaitForSeconds(waitTime);

            currentAmmoInMag += ammoToLoad;
            totalAmmo -= ammoToLoad;
        }

        // Animación de "subida" del arma.
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

        // Actualiza la UI al final (si no es escopeta).
        if (currentWeapon.weaponType != WeaponType.Shotgun)
        {
            UpdateAmmoUI();
        }
    }

    // === DISPARO ===

    /// <summary>
    /// Maneja el input de disparo y lo dirige al método de disparo correcto.
    /// </summary>
    void HandleShooting()
    {
        if (currentWeapon == null) return;

        // Switch para diferentes lógicas de disparo
        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    if (currentWeapon.isUpgraded)
                        StartCoroutine(BurstFire()); // Dispara ráfaga si está mejorada.
                    else
                        Shoot(); // Disparo simple.
                }
                break;

            case WeaponType.Rifle:
                if (Input.GetButton("Fire1") && Time.time >= nextFireTime) // Automático (GetButton)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    ShootRifle();
                }
                break;

            case WeaponType.Shotgun:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime) // Semiautomático (GetButtonDown)
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

    /// <summary>
    /// Lógica de disparo base (usada por la pistola).
    /// </summary>
    void Shoot()
    {
        // Comprueba si hay munición.
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

        // Gasta munición, reproduce efectos y lanza el rayo.
        PlaySound(currentWeapon.shootSound);
        currentAmmoInMag--;
        UpdateAmmoUI();
        StartCoroutine(MuzzleFlashRoutine());

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage * damageMultiplier);

        ApplyRecoil();
    }

    /// <summary>
    /// Lógica de disparo en ráfaga (Pistola mejorada).
    /// </summary>
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
                break; // Sale del bucle si se queda sin balas a mitad de ráfaga.
            }

            // Lógica de disparo (copiada de Shoot()).
            PlaySound(currentWeapon.shootSound);
            currentAmmoInMag--;
            UpdateAmmoUI();
            StartCoroutine(MuzzleFlashRoutine());

            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
                HandleHit(hit, currentWeapon.damage * damageMultiplier);

            ApplyRecoil();
            yield return new WaitForSeconds(currentWeapon.fireRate); // Espera entre disparos de ráfaga.
        }

        yield return new WaitForSeconds(0.1f); // Pequeño cooldown después de la ráfaga.
        isBursting = false;
    }

    /// <summary>
    /// Lógica de disparo del Rifle (idéntica a Shoot, pero separada por si acaso).
    /// </summary>
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

    /// <summary>
    /// Lógica de disparo de la Escopeta (múltiples rayos).
    /// </summary>
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

        // Lanza múltiples rayos (perdigones).
        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            // Calcula una dirección aleatoria dentro del ángulo de dispersión (spread).
            Vector3 direction = playerCamera.transform.forward;
            direction = Quaternion.Euler(
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                0
            ) * direction;

            Ray ray = new Ray(playerCamera.transform.position, direction);

            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
                HandleHit(hit, currentWeapon.damage * damageMultiplier); // Cada perdigón hace daño completo.
        }

        ApplyRecoil();

        // Reproduce el sonido de "pump action" después de un delay.
        if (currentWeapon.pumpActionSound != null)
        {
            yield return new WaitForSeconds(currentWeapon.actionSoundDelay);
            PlaySound(currentWeapon.pumpActionSound);
        }
    }

    /// <summary>
    /// Lógica de disparo del Sniper (penetración).
    /// </summary>
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

        // Usa RaycastAll para obtener todos los impactos.
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit[] hits = Physics.RaycastAll(ray, currentWeapon.range);

        if (hits.Length > 0)
        {
            // Ordena los impactos por distancia (cercano a lejano).
            var sortedHits = hits.OrderBy(h => h.distance);
            // HashSet para evitar dañar al mismo zombi múltiples veces con un disparo.
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
                    // Si es un zombi y no ha sido dañado por este disparo...
                    if (!alreadyDamaged.Contains(zombieHealth))
                    {
                        HandleHit(hit, currentWeapon.damage * damageMultiplier);
                        alreadyDamaged.Add(zombieHealth);
                        targetsHit++;

                        // Si alcanzamos el límite de penetración, paramos.
                        if (targetsHit >= currentWeapon.penetrationCount)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    // Si no es un zombi (ej. una pared), aplica el impacto (agujero de bala).
                    HandleHit(hit, currentWeapon.damage * damageMultiplier);
                }
            }
        }

        // Reproduce el sonido del cerrojo (bolt action) después de un delay.
        if (currentWeapon.boltActionSound != null)
        {
            yield return new WaitForSeconds(currentWeapon.actionSoundDelay);
            PlaySound(currentWeapon.boltActionSound);
        }
    }

    // === GESTIÓN DE IMPACTOS ===

    /// <summary>
    /// Maneja lo que sucede cuando un Raycast impacta algo.
    /// Aplica daño y crea agujeros de bala.
    /// </summary>
    void HandleHit(RaycastHit hit, float damage)
    {
        // Lógica para encontrar el script de salud del zombi,
        // ya sea en el hitbox o en el collider principal.
        ZombieHitbox hitbox = hit.collider.GetComponent<ZombieHitbox>();
        ZombieController zombieHealth = null;

        if (hitbox != null) // Si golpea un hitbox...
        {
            zombieHealth = hitbox.zombieController;
            if (zombieHealth != null)
            {
                // Pasa el daño y el tipo de hitbox (para multiplicadores).
                zombieHealth.TakeDamage(damage, hitbox.hitboxType);
            }
        }
        else // Si golpea el collider principal...
        {
            zombieHealth = hit.collider.GetComponent<ZombieController>();
            if (zombieHealth != null)
            {
                zombieHealth.TakeDamage(damage); // Daño normal (al cuerpo).
            }
        }

        if (zombieHealth != null)
        {
            // Log de depuración.
            float remainingHealth = zombieHealth.GetHP();
            Debug.Log($"El Zombie {hit.collider.name} ha recibido {damage} de daño y le quedan {remainingHealth:F1} de vida.");
        }

        // Lógica para crear agujeros de bala.
        if (hit.collider.CompareTag("Zombie") && currentWeapon.bulletHolePrefab != null)
        {
            // Calcula la rotación del agujero para que quede plano sobre la superficie golpeada.
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0);
            GameObject hole = Instantiate(currentWeapon.bulletHolePrefab,
                                        hit.point + hit.normal * 0.001f, // Un pequeño offset para evitar z-fighting.
                                        hitRotation);

            hole.transform.SetParent(hit.transform); // Emparenta el agujero al zombi (para que se mueva con él).
            hole.transform.Rotate(0, 0, Random.Range(0, 360)); // Rota aleatoriamente.

            Collider holeCollider = hole.GetComponent<Collider>();
            if (holeCollider != null) holeCollider.enabled = false; // Desactiva el collider del agujero.

            Destroy(hole, 5f); // Destruye el agujero después de 5 segundos.
        }
    }


    // === CAMBIO DE ARMA ===

    /// <summary>
    /// Equipar una nueva arma. Maneja el guardado de munición y la instanciación del modelo.
    /// </summary>
    public void EquipWeapon(WeaponData weaponData)
    {
        // 1. Guarda la munición del arma actual (si hay una) en el caché.
        if (currentWeapon != null && currentWeaponModel != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        // 2. Destruye el modelo 3D del arma antigua.
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        // 3. Asigna la nueva arma.
        currentWeapon = weaponData;

        // 4. Resetea el estado de apuntado.
        StopAiming();

        // 5. Si el arma nueva es nula (ej. sin arma), limpia la UI y sale.
        if (currentWeapon == null)
        {
            if (crosshairImage != null) crosshairImage.enabled = false;
            if (ammoText != null) ammoText.text = "";
            return;
        }

        // 6. Instancia el nuevo modelo 3D y lo emparenta al weaponHolder.
        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;

            // Busca la luz del MuzzleFlash en los hijos del modelo nuevo.
            Light newMuzzleLight = currentWeaponModel.GetComponentInChildren<Light>();
            muzzleLight = newMuzzleLight;
            if (muzzleLight == null) Debug.LogWarning($"El arma {weaponData.weaponName} no tiene un componente Light.");
        }

        // 7. Carga la munición del caché para la nueva arma.
        if (ammoInMagCache.ContainsKey(currentWeapon.weaponType))
        {
            currentAmmoInMag = ammoInMagCache[currentWeapon.weaponType];
            totalAmmo = totalAmmoCache[currentWeapon.weaponType];
        }
        else // Si es la primera vez que se equipa esta arma...
        {
            // ...dale la munición por defecto y guárdala en el caché.
            currentAmmoInMag = currentWeapon.magCapacity;
            totalAmmo = currentWeapon.maxAmmo - currentAmmoInMag;
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        // 8. Resetea el estado y actualiza la UI.
        isReloading = false;
        UpdateAmmoUI();
        UpdateCrosshair();
    }

    // === RECOIL ===

    /// <summary>
    /// Aplica el retroceso (kick) a la cámara y al modelo del arma.
    /// </summary>
    private void ApplyRecoil()
    {
        // 1. Recoil de Cámara (Rotación):
        if (cameraController != null)
        {
            // Pide al CameraController que aplique un offset de rotación.
            float vertical = Random.Range(currentWeapon.recoilVerticalMin, currentWeapon.recoilVerticalMax);
            float horizontal = Random.Range(currentWeapon.recoilHorizontalMin, currentWeapon.recoilHorizontalMax);
            cameraController.AddRecoil(vertical, horizontal);
        }

        // 2. Kickback del Arma (Posición):
        if (weaponHolder != null)
        {
            // Aplica un offset hacia atrás al modelo del arma.
            weaponCurrentOffset = new Vector3(0, 0, -currentWeapon.weaponKickbackDistance);
        }
    }

    // === ACTUALIZACIÓN DE HUD ===

    /// <summary>
    /// Actualiza el texto de munición en el HUD.
    /// </summary>
    private void UpdateAmmoUI()
    {
        if (ammoText == null) return;

        if (currentWeapon != null)
        {
            ammoText.text = $"{currentAmmoInMag} / {totalAmmo}";

            // Cambia a color rojo si no queda munición.
            if (currentAmmoInMag == 0 && totalAmmo == 0)
            {
                ammoText.color = Color.red;
            }
            else
            {
                ammoText.color = defaultAmmoColor;
            }
        }
        else
        {
            ammoText.text = "";
            ammoText.color = defaultAmmoColor;
        }
    }

    /// <summary>
    /// Actualiza el sprite y tamaño de la mira (crosshair).
    /// </summary>
    private void UpdateCrosshair()
    {
        if (crosshairRectTransform == null) return;

        if (currentWeapon != null && currentWeapon.crosshairIcon != null)
        {
            // Asigna el sprite y el tamaño definidos en el WeaponData.
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
            crosshairImage.enabled = false; // Oculta la mira si no hay arma.
        }
    }

    // === OTROS MÉTODOS ===

    /// <summary>
    /// Corrutina para el efecto de luz del fogonazo.
    /// </summary>
    private IEnumerator MuzzleFlashRoutine()
    {
        if (muzzleLight == null) yield break;
        muzzleLight.enabled = true;
        yield return new WaitForSeconds(flashDuration);
        muzzleLight.enabled = false;
    }

    // --- Métodos de Guardado/Carga ---

    /// <summary>
    /// Llamado por SaveLoadManager. Guarda la munición actual en el caché
    /// y devuelve una lista de todos los datos de munición.
    /// </summary>
    public List<WeaponAmmoData> GetAmmoData()
    {
        // Asegura que la munición del arma equipada esté actualizada en el caché.
        if (currentWeapon != null)
        {
            ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
            totalAmmoCache[currentWeapon.weaponType] = totalAmmo;
        }

        // Convierte los diccionarios de caché en una lista para serializar.
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

    /// <summary>
    /// Llamado por SaveLoadManager. Recibe los datos de munición guardados
    /// y los carga en el caché.
    /// </summary>
    public void LoadAmmoData(List<WeaponAmmoData> dataList)
    {
        ammoInMagCache.Clear();
        totalAmmoCache.Clear();

        if (dataList == null) return;

        // Rellena los diccionarios de caché con los datos guardados.
        foreach (var data in dataList)
        {
            ammoInMagCache[data.weaponType] = data.currentMagAmmo;
            totalAmmoCache[data.weaponType] = data.currentTotalAmmo;
        }
        Debug.Log("Datos de munición cargados en el caché de PlayerShooting.");
    }

    /// <summary>
    /// Llamado por la Tienda de Armas. Rellena la munición del arma actual al máximo.
    /// </summary>
    public void ForceCurrentWeaponAmmoToFull()
    {
        if (currentWeapon == null) return;

        currentAmmoInMag = currentWeapon.magCapacity;
        totalAmmo = currentWeapon.maxAmmo - currentWeapon.magCapacity;

        // Actualiza el caché también.
        ammoInMagCache[currentWeapon.weaponType] = currentAmmoInMag;
        totalAmmoCache[currentWeapon.weaponType] = totalAmmo;

        UpdateAmmoUI();
    }

    /// <summary>
    /// Llamado por la Tienda de Armas. Comprueba si un arma (esté equipada o no)
    /// ya tiene la munición al máximo.
    /// </summary>
    public bool IsAmmoFull(WeaponData weaponData)
    {
        if (weaponData == null) return true;

        int currentMag = 0;
        int currentTotal = 0;

        // Comprueba la munición del arma equipada.
        if (currentWeapon != null && weaponData.weaponType == currentWeapon.weaponType)
        {
            currentMag = currentAmmoInMag;
            currentTotal = totalAmmo;
        }
        // O comprueba la munición del caché si el arma no está equipada.
        else if (ammoInMagCache.ContainsKey(weaponData.weaponType))
        {
            currentMag = ammoInMagCache[weaponData.weaponType];
            currentTotal = totalAmmoCache[weaponData.weaponType];
        }
        else
        {
            return false; // Si no está en el caché, no la tenemos, ergo no está llena.
        }

        // Compara la munición actual con la máxima capacidad del arma.
        int maxMag = weaponData.magCapacity;
        int maxTotal = weaponData.maxAmmo - weaponData.magCapacity;

        bool isFull = currentMag >= maxMag && currentTotal >= maxTotal;
        return isFull;
    }

    /// <summary>
    /// Getter para saber qué tipo de arma está equipada.
    /// </summary>
    public WeaponType GetEquippedWeaponType()
    {
        if (currentWeapon != null)
        {
            return currentWeapon.weaponType;
        }
        return (WeaponType)(-1); // Devuelve un valor inválido si no hay arma.
    }

    /// <summary>
    /// Helper para reproducir sonidos one-shot.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}