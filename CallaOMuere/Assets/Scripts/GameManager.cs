using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    // Patrón Singleton
    public static GameManager Instance { get; private set; }

    // --- ¡AÑADIDO! ---
    // Banderas estáticas para que otros scripts sepan el estado del juego.
    public static bool IsPaused { get; private set; } = false;
    public static bool GameIsOver { get; private set; } = false;
    // -----------------

    [Header("UI (Paneles)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Animator animator; // El Animator del pausePanel

    [SerializeField] private Animator transitionAnimator;

    [Header("UI (Componentes)")]
    [SerializeField] private TMPro.TextMeshProUGUI gameOverText;
    [SerializeField] private TextMeshProUGUI pauseText;

    // --- Variables para los Ajustes de Pausa ---
    [Header("Ajustes del Menú de Pausa")]
    [SerializeField] private Slider sensitivitySlider_Pause;
    [SerializeField] private Slider musicSlider_Pause;
    [SerializeField] private Slider sfxSlider_Pause;
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private CameraController cameraController;

    // Variable de seguridad para evitar "spam" de la tecla Esc
    private bool isTogglingPause = false;

    [Header("Música de Ambiente")]
    [SerializeField] private AudioSource musicAudioSource; // El componente que añadiste
    [SerializeField] private AudioClip backgroundMusic;

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

        // --- ¡AÑADIDO! ---
        // Resetea las banderas al empezar la escena
        IsPaused = false;
        GameIsOver = false;
        // -----------------

        // Configura los sliders del menú de pausa
        SetupPauseMenuSliders();

        // --- AÑADE ESTAS LÍNEAS PARA LA MÚSICA ---
        if (musicAudioSource != null && backgroundMusic != null)
        {
            musicAudioSource.clip = backgroundMusic;
            musicAudioSource.loop = true; // Asegúrate de que se repita
            musicAudioSource.Play();
        }

        // Comprueba si el SaveLoadManager...
        if (SaveLoadManager.ShouldLoadGame)
        {
            SaveLoadManager.Instance.LoadGame();
        }
    }

    private void Update()
    {
        // Comprueba si se pulsa Escape Y si no estamos ya en mitad de una animación
        if (Input.GetKeyDown(KeyCode.Escape) && !isTogglingPause)
        {
            // Llama a la corrutina correctamente
            StartCoroutine(TogglePauseCoroutine());
        }
    }

    public void PlayerDied()
    {
        Debug.Log("Game Over. Player Died.");
        Time.timeScale = 0; // Pausa el juego

        GameIsOver = true; // <-- ¡AÑADIDO!

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.DeleteSave();
        }
    }

    // Corrutina que maneja la lógica de pausa con animación
    public IEnumerator TogglePauseCoroutine()
    {
        // 1. Ponemos el "seguro"
        isTogglingPause = true;

        if (pausePanel.activeSelf)
        {
            // --- REANUDAR JUEGO ---

            // Inicia animación de salida
            animator.SetBool("Mobile", false);

            // Reanuda el juego INMEDIATAMENTE
            Time.timeScale = 1;
            IsPaused = false; // <-- ¡AÑADIDO!

            // Espera a que termine la animación de salida (usando tiempo real)
            yield return new WaitForSecondsRealtime(0.3f);

            // Termina de reanudar
            pausePanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // --- PAUSAR JUEGO ---

            if (sensitivitySlider_Pause != null)
            {
                LoadCurrentSettingsToSliders();
            }

            // Muestra el panel e inicia la animación
            pausePanel.SetActive(true);
            animator.SetBool("Mobile", true);

            // Espera a que termine la animación de entrada (usando tiempo real)
            yield return new WaitForSecondsRealtime(0.3f);

            // Termina de pausar
            Time.timeScale = 0;
            IsPaused = true; // <-- ¡AÑADIDO!
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        // 2. Quitamos el "seguro"
        isTogglingPause = false;
    }


    // --- MÉTODOS PARA LOS SLIDERS DE PAUSA (sin cambios) ---

    private void SetupPauseMenuSliders()
    {
        if (sensitivitySlider_Pause == null || musicSlider_Pause == null || sfxSlider_Pause == null)
        {
            return;
        }
        LoadCurrentSettingsToSliders();
        sensitivitySlider_Pause.onValueChanged.AddListener(SetSensitivity_Pause);
        musicSlider_Pause.onValueChanged.AddListener(SetMusicVolume_Pause);
        sfxSlider_Pause.onValueChanged.AddListener(SetSFXVolume_Pause);
    }

    private void LoadCurrentSettingsToSliders()
    {
        sensitivitySlider_Pause.value = PlayerPrefs.GetFloat("MasterSensitivity", 100f);
        musicSlider_Pause.value = PlayerPrefs.GetFloat("MasterMusicVolume", 0.75f);
        sfxSlider_Pause.value = PlayerPrefs.GetFloat("MasterSFXVolume", 0.75f);
    }

    public void SetSensitivity_Pause(float value)
    {
        PlayerPrefs.SetFloat("MasterSensitivity", value);
        if (cameraController != null)
        {
            cameraController.SetSensibility(value);
        }
    }

    public void SetMusicVolume_Pause(float value)
    {
        if (mainAudioMixer == null) return;
        mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterMusicVolume", value);
    }

    public void SetSFXVolume_Pause(float value)
    {
        if (mainAudioMixer == null) return;
        mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterSFXVolume", value);
    }

    // --- MÉTODOS DE NAVEGACIÓN (sin cambios) ---

    public void SaveAndQuit()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame();
        }
        QuitToMainMenu();
    }

    public void QuitToMainMenu()
    {
        StartCoroutine(QuitToMainMenuCoroutine());
    }

    private IEnumerator QuitToMainMenuCoroutine()
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        yield return new WaitForSecondsRealtime(0.3f);

        PowerUpStore.ResetOwnedPowerUps();
        WeaponStore.ResetOwnedWeapons();

        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        SceneManager.LoadScene("MainMenu");
    }

    public void RetryButton()
    {
        StartCoroutine(RetryCoroutine());
    }

    private IEnumerator RetryCoroutine()
    {
        if (transitionAnimator != null)
        {
            transitionAnimator.SetTrigger("StartTransition");
        }

        yield return new WaitForSecondsRealtime(0.3f);

        PowerUpStore.ResetOwnedPowerUps();
        WeaponStore.ResetOwnedWeapons();

        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}