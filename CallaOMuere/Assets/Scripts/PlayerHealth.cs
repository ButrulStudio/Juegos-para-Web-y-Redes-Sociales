using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider armorSlider;
    [SerializeField] private GameObject gameOverPanel;

    [Header("Salud")]
    [SerializeField] private float maxHealth = 100f;
    public float currentHealth;

    [Header("Armadura")]
    [SerializeField] public float maxArmor = 100f; // Ahora es public para accesibilidad
    public float currentArmor;

    [Header("Regeneración")]
    [SerializeField] private float timeUntilRegenStarts = 3.0f;
    [SerializeField] private float regenRatePerSecond = 20.0f;

    private float lastDamageTime;

    void Start()
    {
        currentHealth = maxHealth;
        currentArmor = 0f;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;

        if (armorSlider != null)
        {
            armorSlider.maxValue = maxArmor;
            armorSlider.value = currentArmor;
        }

        gameOverPanel.SetActive(false);
        lastDamageTime = Time.time;
    }

    void Update()
    {
        // LÓGICA DE REGENERACIÓN (SÓLO HEALTH)
        if (Time.time >= lastDamageTime + timeUntilRegenStarts)
        {
            if (currentHealth < maxHealth)
            {
                currentHealth += regenRatePerSecond * Time.deltaTime;
                currentHealth = Mathf.Min(currentHealth, maxHealth);
                healthSlider.value = currentHealth;
            }
        }
    }

    public void TakeDamage(float amount) // El daño es FLOAT
    {
        float damageRemaining = amount;

        // 1. PRIORIDAD: DEDUCIR DAÑO DE LA ARMADURA
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

        // 2. DEDUCIR DAÑO RESTANTE DE LA VIDA
        if (damageRemaining > 0f)
        {
            currentHealth -= damageRemaining;
            healthSlider.value = currentHealth;
            lastDamageTime = Time.time;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    // MÉTODO DE COMPRA (Importante para PowerUps.cs)
    public void BuyMaxArmor()
    {
        currentArmor = maxArmor;
        if (armorSlider != null) armorSlider.value = currentArmor;
    }

    public void Die()
    {
        Debug.Log("Has muerto");
        Time.timeScale = 0;
        gameOverPanel.SetActive(true);
    }
}