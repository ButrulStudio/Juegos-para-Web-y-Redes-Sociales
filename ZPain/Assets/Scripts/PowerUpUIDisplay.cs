using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class PowerUpUIDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El objeto de la UI (con un Layout Group) que contendrá los iconos.")]
    [SerializeField] private Transform iconContainer; 

    [Tooltip("El prefab de la UI que usaremos para mostrar cada icono (debe tener un componente Image).")]
    [SerializeField] private GameObject iconPrefab; 

    private Dictionary<PowerUpType, GameObject> activeIcons = new Dictionary<PowerUpType, GameObject>();

    public GameObject AddPowerUpIcon(PowerUpData powerUp)
    {
        if (powerUp.powerUpType != PowerUpType.Armadura && activeIcons.ContainsKey(powerUp.powerUpType))
        {
            Debug.Log($"El icono de {powerUp.powerUpName} ya está activo. Evitando duplicado.");
            return activeIcons[powerUp.powerUpType];
        }
        if (powerUp.icon == null)
        {
            Debug.LogWarning($"PowerUp '{powerUp.powerUpName}' no tiene un icono asignado.");
            return null;
        }

        if (iconContainer == null || iconPrefab == null)
        {
            Debug.LogError("Faltan referencias (Container o Prefab) en PowerUpUIDisplay.");
            return null;
        }
        GameObject newIcon = Instantiate(iconPrefab, iconContainer);

        Image iconImage = newIcon.GetComponentInChildren<Image>();

        if (iconImage != null)
        {

            iconImage.sprite = powerUp.icon;
            if (powerUp.powerUpType != PowerUpType.Armadura)
            {
                activeIcons.Add(powerUp.powerUpType, newIcon);
            }

            return newIcon; 
        }
        else
        {
            Debug.LogError($"El prefab 'iconPrefab' no tiene un componente Image en él o en sus hijos.");
            Destroy(newIcon); 
            return null;
        }
    }
    public void ClearIcons()
    {
        foreach (var icon in activeIcons.Values)
        {
            Destroy(icon);
        }
        activeIcons.Clear();
    }
}