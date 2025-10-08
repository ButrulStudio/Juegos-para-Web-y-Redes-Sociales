using UnityEngine;

[CreateAssetMenu(fileName = "NewWeapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Atributos del arma")]
    public string weaponName;
    public float damage = 20f;
    public float range = 100f;
    public float fireRate = 0.2f;

    [Header("Visuales y efectos")]
    public GameObject weaponModelPrefab;
    public GameObject bulletHolePrefab;
    public Sprite crosshairIcon;
}
