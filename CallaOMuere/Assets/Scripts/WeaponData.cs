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

    [Header("Visuales y efectos")]
    public GameObject weaponModelPrefab;
    public GameObject bulletHolePrefab;
    public Sprite crosshairIcon;

    [Header("Par�metros de escopeta")]
    [Tooltip("N�mero de perdigones que se disparan en un tiro (solo para Shotgun)")]
    public int pelletCount = 5;
    [Tooltip("�ngulo m�ximo (grados) de dispersi�n desde la direcci�n de la mira")]
    [Range(0f, 45f)]
    public float spreadAngle = 10f;

}
