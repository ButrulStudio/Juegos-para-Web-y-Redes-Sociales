using UnityEngine;
using UnityEngine.UI;

public class PowerUpUIDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El objeto de la UI (con un Layout Group) que contendrá los iconos.")]
    [SerializeField] private Transform iconContainer;
    
    [Tooltip("El prefab de la UI que usaremos para mostrar cada icono (debe tener un componente Image).")]
    [SerializeField] private GameObject iconPrefab;

    public GameObject AddPowerUpIcon(PowerUpData powerUp)
    {
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
            return newIcon; // ¡Devuelve el icono!
        }
        else
        {
            Debug.LogError($"El prefab 'iconPrefab' no tiene un componente Image en él o en sus hijos.");
            Destroy(newIcon);
            return null;
        }
    }
}