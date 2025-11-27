using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;

    [Header("Salud")]
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;

    [Header("Armadura")]
    [SerializeField] public float maxArmor = 100f;
    public float currentArmor;

    [Header("Regeneracin")]
    [SerializeField] private float timeUntilRegenStarts = 3.0f;
    [SerializeField] private float regenRatePerSecond = 20.0f;

    private float lastDamageTime;

    [Header("Efectos Visuales (Sangre)")]
    [Tooltip("Arrastra aquí tu imagen UI de sangre (manchas rojas)")]
    [SerializeField] private Image damageOverlay;
    [Tooltip("Qué tan rápido desaparece la sangre. Un valor bajo (0.3) dura más tiempo.")]
    [SerializeField] private float fadeSpeed = 0.5f;

    [Header("Sonido de Daño")]
    [Tooltip("El AudioSource para los sonidos de dolor/daño del jugador.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sonidos que suenan aleatoriamente cuando el jugador recibe daño.")]
    [SerializeField] private AudioClip[] damageSounds;

    void Start()
    {
        currentHealth = maxHealth;
        currentArmor = 0f;

        
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

       
        if (armorSlider != null)
        {
            armorSlider.maxValue = maxArmor;
            armorSlider.value = currentArmor;
        }

        
        if (damageOverlay != null)
        {
            
            Color c = damageOverlay.color;
            c.a = 0f;
            damageOverlay.color = c;
        }

        lastDamageTime = Time.time;
    }

    void Update()
    {
        
        if (Time.timeScale > 0 && Time.time >= lastDamageTime + timeUntilRegenStarts)
        {
            if (currentHealth < maxHealth)
            {
                currentHealth += regenRatePerSecond * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth);

                if (healthSlider != null)
                {
                    healthSlider.value = currentHealth;
                }
            }
        }

        
        if (damageOverlay != null)
        {
            
            if (damageOverlay.color.a > 0)
            {
                Color c = damageOverlay.color;
                c.a -= fadeSpeed * Time.deltaTime;
                damageOverlay.color = c;
            }
        }
    }

    public void TakeDamage(float amount)
    {
       
        if (Time.timeScale == 0) return;

        PlayRandomDamageSound();

       
        if (damageOverlay != null)
        {
            
            Color c = damageOverlay.color;
            c.a = 0.8f;
            damageOverlay.color = c;
        }
        

        float damageRemaining = amount;

        if (currentArmor > 0f)
        {
            if (currentArmor >= damageRemaining)
            {
                currentArmor -= damageRemaining;
                damageRemaining = 0f;
            }
            else
            {
                damageRemaining -= currentArmor;
                currentArmor = 0f;
            }
            if (armorSlider != null) armorSlider.value = currentArmor;
        }

        if (damageRemaining > 0f)
        {
            currentHealth -= damageRemaining;
            currentHealth = Mathf.Max(currentHealth, 0f); 

            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            lastDamageTime = Time.time;
        }

        if (currentHealth <= 0f)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
            else
            {
                Debug.LogError("Player died but GameManager.Instance is null.");
            }
        }
    }

    // Compra de armadura
    public void BuyMaxArmor()
    {
        currentArmor = maxArmor;
        if (armorSlider != null) armorSlider.value = currentArmor;
    }

    public void RestoreArmor(float amount)
    {
        currentArmor += amount;
        currentArmor = Mathf.Min(currentArmor, maxArmor);
        if (armorSlider != null) armorSlider.value = currentArmor;
    }

    public void SetHealthAndArmor(float newHealth, float newArmor)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        currentArmor = Mathf.Clamp(newArmor, 0, maxArmor);

        if (healthSlider != null) healthSlider.value = currentHealth;
        if (armorSlider != null) armorSlider.value = currentArmor;
    }

    private void PlayRandomDamageSound()
    {
        if (audioSource != null && damageSounds != null && damageSounds.Length > 0)
        {
            int index = Random.Range(0, damageSounds.Length);
            AudioClip clip = damageSounds[index];
            if (clip != null) audioSource.PlayOneShot(clip);
        }
    }
}