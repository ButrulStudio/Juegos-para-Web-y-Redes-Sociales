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
        // Disparo con clic izquierdo y control de cadencia
        if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + currentWeapon.fireRate;
            Shoot();
        }
    }

    void Shoot()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            Debug.Log($"Impacto en: {hit.collider.name}");

            // Instanciar bullet hole en el punto de impacto
            if (currentWeapon.bulletHolePrefab != null)
            {
                Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0); ;
                GameObject hole = Instantiate(currentWeapon.bulletHolePrefab,
                                               hit.point + hit.normal * 0.001f,
                                               hitRotation);

                // Rotación aleatoria sobre Z para que se vea más natural
                hole.transform.Rotate(0, 0, Random.Range(0, 360));

                Destroy(hole, 5f); // Se destruye automáticamente tras 5 segundos
            }

            // Aquí más adelante aplicaremos daño si el objeto es un enemigo
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
