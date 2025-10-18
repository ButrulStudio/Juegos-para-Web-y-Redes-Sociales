using UnityEngine;
using UnityEngine.UI; // Necesario para los Sliders

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

    [Header("Regeneraci�n")]
    [SerializeField] private float timeUntilRegenStarts = 3.0f;
    [SerializeField] private float regenRatePerSecond = 20.0f;

    private float lastDamageTime;

    void Start()
    {
        currentHealth = maxHealth;
        currentArmor = 0f;

        // Configuraci�n de la barra de salud
        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        // Configuraci�n de la barra de armadura (si existe)
        if (armorSlider != null)
        {
            armorSlider.maxValue = maxArmor;
            armorSlider.value = currentArmor;
        }

        lastDamageTime = Time.time;
    }

    void Update()
    {
        // L�GICA DE REGENERACI�N (S�LO HEALTH)
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
        // El jugador no puede recibir da�o si el juego est� pausado (Time.timeScale = 0)
        if (Time.timeScale == 0) return;

        float damageRemaining = amount;

        // 1. PRIORIDAD: DEDUCIR DA�O DE LA ARMADURA
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

        // 2. DEDUCIR DA�O RESTANTE DE LA VIDA
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

        // 3. COMPROBAR MUERTE
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

    // M�TODO DE COMPRA
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
    }
}