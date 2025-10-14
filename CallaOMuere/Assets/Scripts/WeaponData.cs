using UnityEngine;

public enum WeaponType
{
    Pistol,
    Rifle,
    Shotgun
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
    public float price = 0;

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

    [Header("Recoil / Kickback")]
    public float recoilVerticalMin = 1f;     // mínimo recoil vertical
    public float recoilVerticalMax = 2f;     // máximo recoil vertical
    public float recoilHorizontalMin = -0.5f;// mínimo recoil horizontal
    public float recoilHorizontalMax = 0.5f; // máximo recoil horizontal

    [Tooltip("Distancia que retrocede el arma al disparar")]
    public float weaponKickbackDistance = 0.05f;

    [Tooltip("Velocidad de regreso del arma a la posición original")]
    public float weaponKickbackReturnSpeed = 8f;
}
