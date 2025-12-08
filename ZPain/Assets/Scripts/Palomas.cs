using UnityEngine;

public class Palomas : MonoBehaviour
{
    [SerializeField] private AudioClip destroySound;
    [SerializeField] private AudioSource audioSource;
    public float requiredDamage = 1f;

    private void Start()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    public void TakeDamage(float damage)
    {
        if (damage >= requiredDamage)
        {
            DestroyTarget();
        }
    }

    private void DestroyTarget()
    {
        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();
        if (playerShooting != null)
        {
            playerShooting.RegisterCollectibleFound();
        }

        if (audioSource != null && destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        Destroy(gameObject);
    }
}