using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private float maxHealth = 100;
    public float currentHealth; // Vida visible (entero)

    [Header("Regeneración")]
    [SerializeField] private float timeUntilRegenStarts = 3.0f; // Tiempo en segundos antes de que empiece a regenerar
    [SerializeField] private float regenRatePerSecond = 20.0f;  // Cantidad de vida por segundo

    private float lastDamageTime;
    // NUEVA VARIABLE: Almacena la vida con decimales para la regeneración gradual
    private float currentHealthFloat;

    void Start()
    {
        currentHealth = maxHealth;
        currentHealthFloat = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        gameOverPanel.SetActive(false);
        lastDamageTime = Time.time;
    }

    void Update()
    {
        // Debug de vida
        if (Input.GetKeyDown(KeyCode.H))
        {
            TakeDamage(10);
        }

        // --- LÓGICA DE REGENERACIÓN ---

        // 1. Verificar si ha pasado el tiempo de espera (3 segundos)
        if (Time.time >= lastDamageTime + timeUntilRegenStarts)
        {
            // 2. Verificar si la vida no está al máximo
            if (currentHealth < maxHealth)
            {
                // Aplicar curación en el float
                currentHealthFloat += regenRatePerSecond * Time.deltaTime;

                // Asegurar que el float no exceda la vida máxima
                currentHealthFloat = Mathf.Min(currentHealthFloat, (float)maxHealth);

                // 3. Comprobar si la parte entera de la vida ha cambiado
                if (currentHealthFloat != currentHealth)
                {
                    // Si ha cambiado, actualizar la vida entera y el Slider
                    currentHealth = currentHealthFloat;
                    healthSlider.value = currentHealth; // Actualiza el Slider
                }
            }
        }
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

        // Sincronizar la vida float con la vida int
        currentHealthFloat = currentHealth;

        healthSlider.value = currentHealth;

        // REINICIA EL TEMPORIZADOR AL RECIBIR DAÑO
        lastDamageTime = Time.time;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Adaptamos Heal para usar el float si se llama desde otro sitio, pero la lógica principal
    // de regeneración está ahora en Update.
    public void Heal(int amount)
    {
        if (currentHealth < maxHealth)
        {
            currentHealth += amount;

            if (currentHealth > maxHealth)
            {
                currentHealth = maxHealth;
            }
            // Sincronizar la vida float
            currentHealthFloat = currentHealth;

            healthSlider.value = currentHealth;
        }
    }

    public void Die()
    {
        Debug.Log("Has muerto ajaj, tonto tonto");
        Time.timeScale = 0;
        gameOverPanel.SetActive(true);
    }
}