using UnityEngine;

[CreateAssetMenu(fileName = "ZombieData", menuName = "Scriptable Objects/ZombieData")]
public class ZombieData : ScriptableObject
{
    public string zombieName;
    public GameObject prefab;
    public float speed = 3f;
    public float maxHp = 100f;
    public float damage = 10f;
    public float attackRange = 2f;
    public float attackCooldown = 1.5f;
}
