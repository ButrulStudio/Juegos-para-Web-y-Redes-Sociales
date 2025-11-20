using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


public class PowerUpUIDisplay : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El objeto de la UI (con un Layout Group) que contendrá los iconos.")]
    [SerializeField] private Transform iconContainer; // Panel (con Layout Group) donde se añadirán los iconos.

    [Tooltip("El prefab de la UI que usaremos para mostrar cada icono (debe tener un componente Image).")]
    [SerializeField] private GameObject iconPrefab; // Prefab del icono (debe tener un componente Image).

    // Diccionario para rastrear los iconos permanentes que ya se están mostrando.
    // Evita duplicados (ej. comprar velocidad dos veces).
    private Dictionary<PowerUpType, GameObject> activeIcons = new Dictionary<PowerUpType, GameObject>();

    /// <summary>
    /// Añade un nuevo icono de PowerUp al 'iconContainer'.
    /// Evita duplicados si el PowerUp es permanente y ya está activo.
    /// </summary>
    /// <param name="powerUp">El ScriptableObject del PowerUp que se va a mostrar.</param>
    /// <returns>El GameObject del icono instanciado, o null si falla.</returns>
    public GameObject AddPowerUpIcon(PowerUpData powerUp)
    {
        // --- Validación 1: Evitar duplicados ---
        // Comprueba si no es Armadura (que es instantánea) y si ya existe
        // un icono de este tipo en el diccionario 'activeIcons'.
        if (powerUp.powerUpType != PowerUpType.Armadura && activeIcons.ContainsKey(powerUp.powerUpType))
        {
            // Si ya existe, simplemente devuelve la referencia existente y no crea uno nuevo.
            Debug.Log($"El icono de {powerUp.powerUpName} ya está activo. Evitando duplicado.");
            return activeIcons[powerUp.powerUpType];
        }

        // --- Validación 2: Comprobaciones de Referencias ---
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

        // --- Instanciación ---
        // Crea el icono usando el prefab y lo asigna como hijo del 'iconContainer'.
        // (El Layout Group del container se encargará de posicionarlo).
        GameObject newIcon = Instantiate(iconPrefab, iconContainer);

        // Busca el componente Image (puede estar en un hijo del prefab).
        Image iconImage = newIcon.GetComponentInChildren<Image>();

        if (iconImage != null)
        {
            // Asigna el sprite correcto.
            iconImage.sprite = powerUp.icon;

            // --- Registro en Diccionario ---
            // Si el PowerUp NO es de Armadura (es decir, es permanente),
            // lo añade al diccionario para rastrearlo y evitar duplicados futuros.
            if (powerUp.powerUpType != PowerUpType.Armadura)
            {
                activeIcons.Add(powerUp.powerUpType, newIcon);
            }

            return newIcon; // Devuelve el icono creado.
        }
        else
        {
            // Error si el prefab no tiene un componente Image.
            Debug.LogError($"El prefab 'iconPrefab' no tiene un componente Image en él o en sus hijos.");
            Destroy(newIcon); // Destruye el objeto vacío para evitar basura en la jerarquía.
            return null;
        }
    }

    /// <summary>
    /// Elimina todos los iconos de PowerUp de la UI y limpia el diccionario.
    /// Útil al cargar una partida o reiniciar.
    /// </summary>
    public void ClearIcons()
    {
        // Destruye todos los GameObjects de los iconos que estaban activos.
        foreach (var icon in activeIcons.Values)
        {
            Destroy(icon);
        }
        // Limpia el diccionario de seguimiento.
        activeIcons.Clear();
    }
}