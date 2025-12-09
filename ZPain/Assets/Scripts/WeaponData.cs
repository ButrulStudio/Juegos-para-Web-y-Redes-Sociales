// DEFINICIÓN DE TODAS TUS ARMAS ESPECÍFICAS
using UnityEngine;

public enum WeaponType
{
    // Melee
    Knife,

    // Pistolas
    Glock,

    // Escopetas
    Remington,      // Corredera
    HuntingShotgun, // Caza (Doble cañón)
    AA12,           // Automática

    // Rifles Asalto / Batalla
    AK47,
    M4A1,
    MTAR,
    Fal,
    M14,

    // Subfusiles (SMG)
    UZI,
    Mp7,

    // Ametralladoras (LMG)
    RPD,

    // Francotiradores
    L11,    // Cerrojo
    SVU,    // Semi-auto

    // Especiales
    Flamethrower
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public WeaponType weaponType;

    [Header("Configuración de Mejora")]
    public bool canBeUpgraded = true; // DESMARCAR PARA EL LANZALLAMAS

    [Tooltip("El tamaño (Ancho, Alto) de la mira 'desde la cadera'")]
    public Vector2 crosshairSize = new Vector2(50, 50);
    [Tooltip("El tamaño (Ancho, Alto) de la mira al apuntar (ADS)")]
    public Vector2 aimedCrosshairSize = new Vector2(100, 100);

    [Header("Atributos del arma")]
    public string weaponName;
    public float damage = 20f;
    public float range = 100f;
    public float fireRate = 0.2f;

    [Header("Precios de la Tienda")]
    public float price = 0;
    public int ammoPrice = 50;

    [Header("Munición")]
    public int magCapacity = 8;
    public int maxAmmo = 32;
    public float reloadTime = 2.0f;

    [Header("Mejoras del arma")]
    public bool isUpgraded = false;
    public int upgradeCost = 100;

    [Header("Visuales y efectos")]
    public GameObject weaponModelPrefab;
    public GameObject bulletHolePrefab;
    public Sprite crosshairIcon;

    [Header("Apuntado (ADS)")]
    public bool canAim = false;
    public float aimedFOV = 60f;
    public Sprite sniperScopeSprite;
    public Vector3 aimPosition;
    public Vector3 aimRotation;

    [Header("Parámetros de escopeta")]
    public int pelletCount = 4;
    [Range(0f, 45f)]
    public float spreadAngle = 10f;

    [Header("Parámetros de Sniper")]
    public int penetrationCount = 1;

    [Header("Parámetros de Subfusil (SMG)")]
    public int vampireAmmoRestore = 3;

    [Header("Parámetros de LMG (Ametralladora)")]
    public float maxHeatDamageMultiplier = 2.0f;
    public float heatRampUpTime = 3.0f;
    public float heatCooldownTime = 1.5f;

    [Header("Recoil / Kickback")]
    public float recoilVerticalMin = 1f;
    public float recoilVerticalMax = 2f;
    public float recoilHorizontalMin = -0.5f;
    public float recoilHorizontalMax = 0.5f;
    public float weaponKickbackDistance = 0.05f;
    public float weaponKickbackReturnSpeed = 8f;

    [Header("Sonidos (SFX)")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound;

    [Header("Sonidos Específicos (Delay)")]
    public float actionSoundDelay = 0.4f;
    public AudioClip boltActionSound;
    public AudioClip pumpActionSound;

    [Header("Parámetros de Lanzallamas")]
    public float flameRadius = 0.5f;
    public int requiredKillsForUlt = 20;

    [Header("--- EFECTOS ESPECIALES ---")]
    [Tooltip("Si es true, el arma ralentiza al zombi")]
    public bool causesSlow = false;
    public float slowAmount = 0.5f;
    public float slowDuration = 2.0f;

    [Space(10)]
    [Tooltip("Si es true, empuja al zombi hacia atrás")]
    public bool causesKnockback = false;
    public float knockbackForce = 2.0f;
}