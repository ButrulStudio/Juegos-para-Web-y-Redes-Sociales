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

    [Header("Sonido de Daño")]
    [Tooltip("El AudioSource para los sonidos de dolor/daño del jugador.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Sonidos que suenan aleatoriamente cuando el jugador recibe daño.")]
    [SerializeField] private AudioClip[] damageSounds;

    void Start()
    {
        currentHealth = maxHealth;
        currentArmor = 0f;

        // Configuracin de la barra de salud
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Configuracin de la barra de armadura (si existe)
        if (armorSlider != null)
        {
            armorSlider.maxValue = maxArmor;
            armorSlider.value = currentArmor;
        }

        lastDamageTime = Time.time;
    }

    void Update()
    {
        // LGICA DE REGENERACIN (SLO HEALTH)
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
    }

    public void TakeDamage(float amount)
    {
        // El jugador no puede recibir dao si el juego est pausado (Time.timeScale = 0)
        if (Time.timeScale == 0) return;

        PlayRandomDamageSound();

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
            currentHealth = Mathf.Max(currentHealth, 0f); // Asegura que la vida no sea negativa

            if (healthSlider != null)
            {
                healthSlider.value = currentHealth;
            }

            lastDamageTime = Time.time;
        }

        if (currentHealth <= 0f)
        {
            // Llama al GameManager para manejar la muerte del jugador y el Game Over
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayerDied();
            }
            else
            {
                Debug.LogError("Player died but GameManager.Instance is null. Cannot handle game over properly.");
            }
        }
    }

    // MTODO DE COMPRA
    public void BuyMaxArmor()
    {
        currentArmor = maxArmor;
        if (armorSlider != null) armorSlider.value = currentArmor;
    }

    // -------------------- MÉTODOS PARA POWER-UPS --------------------
    public void RestoreArmor(float amount)
    {
        currentArmor += amount;
        currentArmor = Mathf.Min(currentArmor, maxArmor);
        Debug.Log($"Armadura restaurada: +{amount}, actual: {currentArmor}");

        if (armorSlider != null)
        {
            armorSlider.value = currentArmor;
        }
    }

    // Establece la vida y armadura al cargar una partida guardada.
    public void SetHealthAndArmor(float newHealth, float newArmor)
    {
        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        currentArmor = Mathf.Clamp(newArmor, 0, maxArmor);

        // Actualiza los sliders inmediatamente
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (armorSlider != null) armorSlider.value = currentArmor;
    }

    private void PlayRandomDamageSound()
    {
        if (audioSource != null && damageSounds != null && damageSounds.Length > 0)
        {
            // Elige un clip aleatorio del array
            int index = Random.Range(0, damageSounds.Length);
            AudioClip clip = damageSounds[index];

            // Reproduce ese clip
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }
}