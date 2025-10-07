using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{

    [SerializeField] private Slider healthSlider;
    [SerializeField] private GameObject gameOverPanel;
    //[SerializeField] private TextMeshProUGUI gameOver;
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    // Start is called before the first frame update
    void Start()
    {
        currentHealth = maxHealth;
        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
        gameOverPanel.SetActive(false);
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        healthSlider.value = currentHealth;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        healthSlider.value = currentHealth;
        if(currentHealth >= maxHealth)
        {
            currentHealth = maxHealth;
        }
    }

    public void Die()
    {
        Debug.Log("Has muerto ajaj, tonto tonto");
        Time.timeScale = 0;
        gameOverPanel.SetActive(true);
        
    }
}
