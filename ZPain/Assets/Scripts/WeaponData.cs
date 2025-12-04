using UnityEngine;

public enum WeaponType
{
    Knife,
    Pistol,
    Rifle,
    Shotgun,
    Sniper,
    flamethrower
}

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    public WeaponType weaponType = WeaponType.Pistol;

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

    [Header("Apuntado (ADS - Aim Down Sights)")]
    [Tooltip("¿Puede esta arma apuntar con el clic derecho?")]
    public bool canAim = false;
    [Tooltip("FOV de la cámara al apuntar")]
    public float aimedFOV = 60f;
    [Tooltip("Sprite de la mirilla del francotirador")]
    public Sprite sniperScopeSprite;

    [Tooltip("Posición local del arma cuando estás apuntando (para centrarla)")]
    public Vector3 aimPosition;
    [Tooltip("Rotación local del arma cuando estás apuntando (si es necesaria)")]
    public Vector3 aimRotation;

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

    [Header("Sonidos (SFX)")]
    public AudioClip shootSound;
    public AudioClip reloadSound;
    public AudioClip emptyClipSound; // Sonido de "clic" cuando no hay balas

    [Header("Sonidos Específicos (Delay)")]
    [Tooltip("Delay (en segundos) tras disparar para reproducir el sonido de 'acción' (cerrojo, cartucho)")]
    public float actionSoundDelay = 0.4f;
    public AudioClip boltActionSound; // Para el cerrojo del Sniper
    public AudioClip pumpActionSound; // Para el cartucho de la Escopeta

    [Header("Parámetros de Lanzallamas")]
    [Tooltip("Radio del chorro de fuego (grosor del cilindro de detección)")]
    public float flameRadius = 0.5f;
    [Header("Consumo de Munición")]
    [Tooltip("Cada cuántos disparos se gasta 1 bala real. 1 = Normal. 3 = El lanzallamas gasta 1 bala cada 3 frames de daño.")]
    public int requiredKillsForUlt = 20;
}