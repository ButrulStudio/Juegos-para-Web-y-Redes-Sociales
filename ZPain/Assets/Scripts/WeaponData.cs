using UnityEngine;

// Enum con la lista de todas las armas específicas
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
    [Header("Identidad del Arma")]
    public WeaponType weaponType;
    public string weaponName;

    [Header("Configuración de Mejora")]
    [Tooltip("Si es false, el arma no podrá mejorarse en la máquina (Ej: Lanzallamas).")]
    public bool canBeUpgraded = true;

    [Header("UI - Mira")]
    [Tooltip("El tamaño (Ancho, Alto) de la mira 'desde la cadera'")]
    public Vector2 crosshairSize = new Vector2(50, 50);
    [Tooltip("El tamaño (Ancho, Alto) de la mira al apuntar (ADS)")]
    public Vector2 aimedCrosshairSize = new Vector2(100, 100);
    public Sprite crosshairIcon;

    [Header("Estadísticas de Combate")]
    public float damage = 20f;
    public float range = 100f;
    [Tooltip("Tiempo entre disparos (Menor número = Más rápido)")]
    public float fireRate = 0.2f;

    [Header("Economía (Tienda)")]
    public float price = 0;
    public int ammoPrice = 50;

    [Header("Munición y Recarga")]
    public int magCapacity = 8;
    public int maxAmmo = 32;
    [Tooltip("Tiempo total de la animación de recarga. En escopetas de tubo, ajusta esto para sincronizar.")]
    public float reloadTime = 2.0f;

    [Header("Estado de Mejora (Pack-a-Punch)")]
    public bool isUpgraded = false;
    public int upgradeCost = 5000;

    [Header("Visuales 3D")]
    public GameObject weaponModelPrefab;
    public GameObject bulletHolePrefab;

    [Header("Apuntado (ADS)")]
    public bool canAim = false;
    public float aimedFOV = 60f;
    public Sprite sniperScopeSprite;
    public Vector3 aimPosition;
    public Vector3 aimRotation;

    [Header("Mecánicas: Escopeta")]
    public int pelletCount = 1; // 1 para rifles/pistolas, 8+ para escopetas
    [Range(0f, 45f)]
    public float spreadAngle = 0f; // 0 para rifles, 5-10 para escopetas

    [Header("Mecánicas: Sniper")]
    public int penetrationCount = 1; // Cuántos zombis atraviesa

    [Header("Mecánicas: SMG (Vampiro)")]
    public int vampireAmmoRestore = 0; // Balas recuperadas al matar

    [Header("Mecánicas: LMG (Calor)")]
    public float maxHeatDamageMultiplier = 1.0f;
    public float heatRampUpTime = 3.0f;
    public float heatCooldownTime = 1.5f;

    [Header("Mecánicas: Lanzallamas (Ultimate)")]
    public float flameRadius = 0.5f;
    public int requiredKillsForUlt = 20;

    [Header("--- EFECTOS DE IMPACTO ---")]
    [Tooltip("Si es true, congela/ralentiza al zombi")]
    public bool causesSlow = false;
    public float slowAmount = 0.5f; // 0.5 = 50% velocidad
    public float slowDuration = 2.0f;

    [Space(5)]
    [Tooltip("Si es true, empuja al zombi hacia atrás")]
    public bool causesKnockback = false;
    public float knockbackForce = 2.0f;

    [Header("Retroceso (Recoil)")]
    public float recoilVerticalMin = 1f;
    public float recoilVerticalMax = 2f;
    public float recoilHorizontalMin = -0.5f;
    public float recoilHorizontalMax = 0.5f;
    public float weaponKickbackDistance = 0.05f;
    public float weaponKickbackReturnSpeed = 8f;

    [Header("Audio: Básico")]
    public AudioClip shootSound;
    public AudioClip reloadSound;     // Sonido general (ropa/movimiento)
    public AudioClip emptyClipSound;

    [Header("Audio: Específico")]
    [Tooltip("Sonido individual de meter UN cartucho (Solo para Remington/Caza)")]
    public AudioClip shellInsertSound;

    [Tooltip("Sonido de cerrojo (Sniper) o corredera (Escopeta)")]
    public AudioClip boltActionSound;
    public AudioClip pumpActionSound;

    [Tooltip("Tiempo de espera para reproducir el sonido de acción extra")]
    public float actionSoundDelay = 0.4f;
}