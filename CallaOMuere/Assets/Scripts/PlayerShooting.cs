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
        // Lanza el Raycast
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, currentWeapon.range))
        {
            // Debug.Log($"Impacto en: {hit.collider.name}"); // Desactivado para evitar redundancia

            // 1. Intentar obtener el componente ZombieController
            ZombieController zombieHealth = hit.collider.GetComponent<ZombieController>();

            if (zombieHealth != null)
            {
                // A. APLICAR DAÑO Y MENSAJE DE DEBUG (Solo si es un Zombie)
                float damageAmount = currentWeapon.damage;
                zombieHealth.TakeDamage(damageAmount);
                float remainingHealth = zombieHealth.GetHP(); // Usa la nueva función

                Debug.Log($"El Zombie {hit.collider.name} ha recibido {damageAmount} de daño y le quedan {remainingHealth:F1} de vida.");
                // :F1 formatea el float a un decimal

                // B. ADHERIR BULLET HOLE AL ZOMBIE
                if (currentWeapon.bulletHolePrefab != null)
                {
                    // Calcular rotación para que el agujero mire hacia afuera
                    Quaternion hitRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal) * Quaternion.Euler(0, 180f, 0);

                    GameObject hole = Instantiate(currentWeapon.bulletHolePrefab,
                                                   hit.point + hit.normal * 0.001f, // Desplazamiento mínimo para evitar Z-fighting
                                                   hitRotation);

                    // Adherir el bullet hole al objeto impactado (el zombie)
                    hole.transform.SetParent(hit.transform);

                    // Rotación aleatoria sobre Z para que se vea más natural
                    hole.transform.Rotate(0, 0, Random.Range(0, 360));

                    // Desactivar el Collider del bullet hole para que no interfiera con el zombie
                    Collider holeCollider = hole.GetComponent<Collider>();
                    if (holeCollider != null)
                    {
                        holeCollider.enabled = false;
                    }

                    Destroy(hole, 5f); // Se destruye automáticamente tras 5 segundos
                }
            }
            // 2. Restricción: Si impacta algo pero NO tiene ZombieController, 
            // no se pone el bullet hole ni se aplica daño (cumple la restricción de 'solo en zombies').
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
