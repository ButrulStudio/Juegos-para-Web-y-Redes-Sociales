using UnityEngine;
using UnityEngine.UI; // Necesario para trabajar con Image

public class PowerUpUIDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El objeto de la UI (con un Layout Group) que contendrá los iconos.")]
    [SerializeField] private Transform iconContainer;
    
    [Tooltip("El prefab de la UI que usaremos para mostrar cada icono (debe tener un componente Image).")]
    [SerializeField] private GameObject iconPrefab;

    /// <summary>
    /// Añade un nuevo icono de PowerUp al contenedor de la UI.
    /// </summary>
    /// <param name="powerUp">El ScriptableObject del power-up que contiene el icono.</param>
    public void AddPowerUpIcon(PowerUpData powerUp)
    {
        if (powerUp.icon == null)
        {
            Debug.LogWarning($"PowerUp '{powerUp.powerUpName}' no tiene un icono asignado.");
            return;
        }

        if (iconContainer == null || iconPrefab == null)
        {
            Debug.LogError("Faltan referencias (Container o Prefab) en PowerUpUIDisplay.");
            return;
        }

        // 1. Instanciar el prefab del icono
        GameObject newIcon = Instantiate(iconPrefab, iconContainer);

        // 2. Obtener su componente Image
        // Usamos GetComponentInChildren por si la imagen no está en la raíz del prefab
        Image iconImage = newIcon.GetComponentInChildren<Image>();

        if (iconImage != null)
        {
            // 3. Asignar el sprite correcto
            iconImage.sprite = powerUp.icon;
        }
        else
        {
            Debug.LogError($"El prefab 'iconPrefab' no tiene un componente Image en él o en sus hijos.");
        }
    }
}
