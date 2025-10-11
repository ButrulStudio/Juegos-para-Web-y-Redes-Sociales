using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;  // C�mara del jugador
    [SerializeField] private Transform weaponHolder; // Empty donde se instanciar� el arma

    [Header("Arma actual")]
    public WeaponData currentWeapon;  // Arma equipada actualmente
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;

    private bool isBursting = false;

    void Start()
    {
        EquipWeapon(currentWeapon);  // Instancia la pistola al iniciar
    }

    void Update()
    {
        // Control de disparo seg�n el tipo de arma
        switch (currentWeapon.weaponType)
        {
            case WeaponType.Pistol:
                if (Input.GetButtonDown("Fire1") && Time.time >= nextFireTime && !isBursting)
                {
                    nextFireTime = Time.time + currentWeapon.fireRate;

                    if (currentWeapon.isUpgraded)
                    {
                        StartCoroutine(BurstFire());
                    }
                    else
                    {
                        Shoot(); // disparo normal
                    }
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

    private IEnumerator BurstFire()
    {
        if (isBursting) yield break; // Si ya est� disparando una r�faga, salir

        isBursting = true; // Marca que estamos en r�faga
        int burstCount = 3; // N�mero de disparos por r�faga

        for (int i = 0; i < burstCount; i++)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            {
                HandleHit(hit, currentWeapon.damage);
            }

            // Peque�o delay entre balas individuales de la r�faga
            yield return new WaitForSeconds(currentWeapon.fireRate);
        }

        // Cadencia entre r�fagas (puedes ajustar el tiempo)
        yield return new WaitForSeconds(0.1f);

        isBursting = false; // Permite una nueva rafaga
    }

    void ShootShotgun()
    {
        for (int i = 0; i < currentWeapon.pelletCount; i++)
        {
            // Direcci�n base
            Vector3 direction = playerCamera.transform.forward;

            // Aplicar rotaci�n aleatoria dentro del cono de spread (pitch y yaw)
            direction = Quaternion.Euler(
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                Random.Range(-currentWeapon.spreadAngle, currentWeapon.spreadAngle),
                0
            ) * direction;

            Ray ray = new Ray(playerCamera.transform.position, direction);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            {
                HandleHit(hit, currentWeapon.damage); // cada perdig�n hace el da�o completo
            }
        }
    }

    void ShootRifle()
    {
        // Raycast desde la c�mara hacia adelante
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // Si impacta algo
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            // Aplica el da�o normal del arma
            HandleHit(hit, currentWeapon.damage);
        }

        // (Opcional) aqu� m�s adelante puedes a�adir:
        // - retroceso visual
        // - sonido de r�faga
        // - part�culas del ca��n
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
            Debug.Log($"El Zombie {hit.collider.name} ha recibido {damageAmount} de da�o y le quedan {remainingHealth:F1} de vida.");
        }

        // Instanciar bullet hole (si hay prefab)
        if (hit.collider.CompareTag("Zombie") && currentWeapon.bulletHolePrefab != null)
        {
            Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0);
            GameObject hole = Instantiate(currentWeapon.bulletHolePrefab,
                                           hit.point + hit.normal * 0.001f,
                                           hitRotation);

            // Adherir al objeto impactado para que se mueva con �l
            hole.transform.SetParent(hit.transform);

            // Rotaci�n aleatoria en Z
            hole.transform.Rotate(0, 0, Random.Range(0, 360));

            // Desactivar collider del agujero si tiene (para evitar interferencias)
            Collider holeCollider = hole.GetComponent<Collider>();
            if (holeCollider != null) holeCollider.enabled = false;

            Destroy(hole, 5f);
        }
    }

    // M�todo para equipar un arma y mostrar su modelo
    public void EquipWeapon(WeaponData weaponData)
    {
        if (currentWeaponModel != null)
            Destroy(currentWeaponModel);

        currentWeapon = Instantiate(weaponData);

        if (currentWeapon.weaponModelPrefab != null && weaponHolder != null)
        {
            currentWeaponModel = Instantiate(currentWeapon.weaponModelPrefab, weaponHolder);
            currentWeaponModel.transform.localPosition = Vector3.zero;
            currentWeaponModel.transform.localRotation = Quaternion.identity;
        }
    }
}
