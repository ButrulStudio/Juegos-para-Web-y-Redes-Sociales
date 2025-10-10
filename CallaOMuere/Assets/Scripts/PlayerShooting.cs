using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;  // Cámara del jugador
    [SerializeField] private Transform weaponHolder; // Empty donde se instanciará el arma

    [Header("Arma actual")]
    public WeaponData currentWeapon;  // Arma equipada actualmente
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;

    void Start()
    {
        EquipWeapon(currentWeapon);  // Instancia la pistola al iniciar
    }

    void Update()
    {
        // Control de disparo según el tipo de arma
        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;
                    Shoot(); // disparo simple
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
                    ShootShotgun(); // varios perdigones
                }
                break;
        }
    }

    void Shoot()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            HandleHit(hit,currentWeapon.damage);
        }
    }

    void ShootShotgun()
    {
        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            // Dirección base
            Vector3 direction = playerCamera.transform.forward;

            // Aplicar rotación aleatoria dentro del cono de spread (pitch y yaw)
            direction = Quaternion.Euler(
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                0
            ) * direction;

            Ray ray = new Ray(playerCamera.transform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            {
                HandleHit(hit, currentWeapon.damage); // cada perdigón hace el daño completo
            }
        }
    }

    void ShootRifle()
    {
        // Raycast desde la cámara hacia adelante
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Si impacta algo
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            // Aplica el daño normal del arma
            HandleHit(hit, currentWeapon.damage);
        }

        // (Opcional) aquí más adelante puedes añadir:
        // - retroceso visual
        // - sonido de ráfaga
        // - partículas del cañón
    }

    void HandleHit(RaycastHit hit,float damage)
    {
        // Intentar obtener el componente del zombie (tu clase actualmente es ZombieController)
        ZombieController zombieHealth = hit.collider.GetComponent<ZombieController>();

        if (zombieHealth != null)
        {
            float damageAmount = damage;
            zombieHealth.TakeDamage(damageAmount);
            float remainingHealth = zombieHealth.GetHP();
            Debug.Log($"El Zombie {hit.collider.name} ha recibido {damageAmount} de daño y le quedan {remainingHealth:F1} de vida.");
        }

        // Instanciar bullet hole (si hay prefab)
        if (hit.collider.CompareTag("Zombie") && currentWeapon.bulletHolePrefab != null)
        {
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0);
            GameObject hole = Instantiate(currentWeapon.bulletHolePrefab,
                                           hit.point + hit.normal * 0.001f,
                                           hitRotation);

            // Adherir al objeto impactado para que se mueva con él
            hole.transform.SetParent(hit.transform);

            // Rotación aleatoria en Z
            hole.transform.Rotate(0, 0, Random.Range(0, 360));

            // Desactivar collider del agujero si tiene (para evitar interferencias)
            Collider holeCollider = hole.GetComponent<Collider>();
            if (holeCollider != null) holeCollider.enabled = false;

            Destroy(hole, 5f);
        }
    }

    // Método para equipar un arma y mostrar su modelo
    public void EquipWeapon(WeaponData weaponData)
    {
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeapon = weaponData;

        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
        }
    }
}
