using System.Collections;
using UnityEngine;

public class PlayerShooting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private CameraController cameraController;

    [Header("Arma actual")]
    public WeaponData currentWeapon;
    private GameObject currentWeaponModel;
    private float nextFireTime = 0f;

    private bool isBursting = false;

    private Vector3 weaponInitialLocalPos;
    private Vector3 weaponCurrentOffset;

    void Start()
    {
        EquipWeapon(currentWeapon);
        if (weaponHolder != null) weaponInitialLocalPos = weaponHolder.localPosition;
    }

    void Update()
    {
        HandleShooting();

        // Movimiento de retorno del arma
        if (weaponHolder != null)
        {
            weaponCurrentOffset = Vector3.Lerp(weaponCurrentOffset, Vector3.zero, Time.deltaTime * currentWeapon.weaponKickbackReturnSpeed);
            weaponHolder.localPosition = weaponInitialLocalPos + weaponCurrentOffset;
        }
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
        }
    }

    void Shoot()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage);

        ApplyRecoil();
    }

    private IEnumerator BurstFire()
    {
        if (isBursting) yield break;
        isBursting = true;

        int burstCount = 3;
        for (int i = 0; i < burstCount; i++)
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
                HandleHit(hit, currentWeapon.damage);

            ApplyRecoil();
            yield return new WaitForSeconds(currentWeapon.fireRate);
        }

        yield return new WaitForSeconds(0.1f);
        isBursting = false;
    }

    void ShootRifle()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
            HandleHit(hit, currentWeapon.damage);

        ApplyRecoil();
    }

    void ShootShotgun()
    {
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
                HandleHit(hit, currentWeapon.damage);
        }

        ApplyRecoil();
    }

    void HandleHit(RaycastHit hit, float damage)
    {
        ZombieController zombieHealth = hit.collider.GetComponent<ZombieController>();

        if (zombieHealth != null)
        {
            zombieHealth.TakeDamage(damage);
            float remainingHealth = zombieHealth.GetHP(); // Feedback de vida
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
}
