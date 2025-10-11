using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Patrón Singleton
    public static GameManager Instance { get; private set; }

    // Referencias a los componentes de UI del Canvas
    // IMPORTANTE: Este GameObject debe contener todo (texto, botón, fondo)
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMPro.TextMeshProUGUI gameOverText;
    [SerializeField] private UnityEngine.UI.Button retryButton;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // No destruir el GameManager al cargar una escena nueva si deseas mantenerlo.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 1. INICIALIZACIÓN: Asegurarse de que el Panel esté oculto al iniciar.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // 2. CURSOR: Ocultar el cursor del ratón y bloquearlo al centro de la pantalla durante el juego.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 3. CONFIGURAR BOTÓN
        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(RestartGame);
        }

        Time.timeScale = 1;
    }

    /// <summary>
    /// Método llamado por PlayerHealth cuando la salud llega a cero.
    /// </summary>
    public void PlayerDied()
    {
        Debug.Log("Game Over. Player Died.");

        // 1. Detener el tiempo del juego
        Time.timeScale = 0;

        // 2. Mostrar la UI de Game Over
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            if (gameOverText != null)
            {
                gameOverText.text = "¡Has Muerto!";
            }
        }

        // 3. CURSOR: Hacer visible el cursor para que el jugador pueda interactuar con la UI.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None; // Desbloquear el cursor
    }

    /// <summary>
    /// Método llamado por el botón de reintento.
    /// </summary>
    public void RestartGame()
    {
        // 1. Restablecer la escala de tiempo
        Time.timeScale = 1;

        // 2. CURSOR: Ocultar y bloquear el cursor de nuevo antes de recargar la escena.
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 3. Recargar la escena actual
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}