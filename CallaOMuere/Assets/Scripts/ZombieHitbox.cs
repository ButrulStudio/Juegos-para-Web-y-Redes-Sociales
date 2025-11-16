using UnityEngine;

public enum EHitboxType
{
    Head,
    Body,
    Legs
}



public class ZombieHitbox : MonoBehaviour
{
    [Tooltip("Define qué parte del cuerpo es este collider.")]
    public EHitboxType hitboxType = EHitboxType.Body;

    [Tooltip("Referencia al controlador principal del zombi.")]
    public ZombieController zombieController;

    void Awake()
    {
        if (zombieController == null)
        {
            zombieController = GetComponentInParent<ZombieController>();
        }
    }
}