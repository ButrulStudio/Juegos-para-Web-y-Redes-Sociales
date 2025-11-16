using UnityEngine;

public enum EHitboxType
{
    Head,
    Body,
    Legs
}



public class ZombieHitbox : MonoBehaviour
{

    public EHitboxType hitboxType = EHitboxType.Body;
    public ZombieController zombieController;

    void Awake()
    {
        if (zombieController == null)
        {
            zombieController = GetComponentInParent<ZombieController>();
        }
    }
}