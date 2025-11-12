using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Patrón Singleton
    public static GameManager Instance { get; private set; }

    [Header("UI (Paneles)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;

    // ¡NUEVO! Arrastra aquí tu Panel_Fade (el que tiene el Animator)
    [SerializeField] private Animator transitionAnimator;

    [Header("UI (Componentes)")]
    [SerializeField] private TMPro.TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI pauseText;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        // Estado inicial del juego
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PanelOpen();
        }
    }

    public void PlayerDied()
    {
        Debug.Log("Game Over. Player Died.");
        Time.timeScale = 0; // Pausa el juego

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Libera el cursor
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void PanelOpen()
    {
        if (pausePanel.activeSelf)
        {
            // Reanudar juego
            pausePanel.SetActive(false);
            Time.timeScale = 1;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // Pausar juego
            pausePanel.SetActive(true);
            Time.timeScale = 0;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // --- ¡SECCIÓN MODIFICADA PARA LA TRANSICIÓN! ---

    // Esta es la función que debe llamar tu botón "Salir al Menú".

    public void QuitToMainMenu()
    {
        // Inicia la corrutina que hace el trabajo sucio
        StartCoroutine(QuitToMainMenuCoroutine());
    }

    private IEnumerator QuitToMainMenuCoroutine()
    {
        // 1. Activa el fundido a negro
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        // 2. Espera 0.3 segundos de TIEMPO REAL (ignora la pausa)
        yield return new WaitForSecondsRealtime(0.3f);

        // 3. Limpia el estado del juego (¡fundamental!)
        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // 4. Carga la escena del menú
        SceneManager.LoadScene("MainMenu");
    }

    public void RetryButton()
    {
        // Inicia la corrutina que hace el trabajo sucio
        StartCoroutine(RetryCoroutine());
    }

    private IEnumerator RetryCoroutine()
    {
        // 1. Activa el fundido a negro
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        // 2. Espera 0.3 segundos de TIEMPO REAL (ignora la pausa)
        yield return new WaitForSecondsRealtime(0.3f);

        // 3. Limpia el estado del juego (¡fundamental!)
        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}