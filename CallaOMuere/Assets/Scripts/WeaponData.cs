using UnityEngine;

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun,
    Sniper
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public WeaponType weaponType = WeaponType.Pistol;

    [Header("Atributos del arma")]
    public string weaponName;
    public float damage = 20f;
    public float range = 100f;
    public float fireRate = 0.2f;

    [Header("Precios de la Tienda")]
    [Tooltip("Precio para comprar el arma por primera vez")]
    public float price = 0;

    [Tooltip("Precio para rellenar la munición (si ya la tienes)")]
    public int ammoPrice = 50;


    [Header("Munición")]
    [Tooltip("Balas que caben en el cargador")]
    public int magCapacity = 8;
    [Tooltip("Máxima munición")]
    public int maxAmmo = 32;
    [Tooltip("Tiempo que tarda el arma en recargar (en segundos)")]
    public float reloadTime = 2.0f;

    [Header("Mejoras del arma")]
    public bool isUpgraded = false;
    public int upgradeCost = 100;

    [Header("Visuales y efectos")]
    public GameObject weaponModelPrefab;
    public GameObject bulletHolePrefab;
    public Sprite crosshairIcon;

    [Header("Parámetros de escopeta")]
    [Tooltip("Número de perdigones que se disparan en un tiro (solo para Shotgun)")]
    public int pelletCount = 4;
    [Tooltip("Ángulo máximo (grados) de dispersión desde la dirección de la mira")]
    [Range(0f, 45f)]
    public float spreadAngle = 10f;

    [Header("Parámetros de Sniper")]
    public int penetrationCount = 1;

    [Header("Recoil / Kickback")]
    public float recoilVerticalMin = 1f;
    public float recoilVerticalMax = 2f;
    public float recoilHorizontalMin = -0.5f;
    public float recoilHorizontalMax = 0.5f;
    [Tooltip("Distancia que retrocede el arma al disparar")]
    public float weaponKickbackDistance = 0.05f;
    [Tooltip("Velocidad de regreso del arma a la posición original")]
    public float weaponKickbackReturnSpeed = 8f;
}