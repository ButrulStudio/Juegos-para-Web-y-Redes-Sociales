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

    [Header("UI (Paneles)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;

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

        // Configura los sliders del menú de pausa
        SetupPauseMenuSliders();

        // Comprueba si el SaveLoadManager (desde el Menú Principal)
        // ha marcado que debemos cargar una partida guardada.
        if (SaveLoadManager.ShouldLoadGame)
        {
            // Si es así, llama al método LoadGame() del SaveLoadManager.
            SaveLoadManager.Instance.LoadGame();
        }
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

        // Si el jugador muere, busca el SaveLoadManager
        // y llama a su función para borrar el archivo de guardado.
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.DeleteSave();
        }
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
            // Carga los valores actuales en los sliders CADA VEZ que abres el menú
            if (sensitivitySlider_Pause != null)
            {
                LoadCurrentSettingsToSliders();
            }

            pausePanel.SetActive(true);
            Time.timeScale = 0;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // --- MÉTODOS PARA LOS SLIDERS DE PAUSA (ya implementados) ---

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

    // Debes crear un nuevo botón "Guardar y Salir" en tu
    // 'pausePanel' y conectarlo a esta función.
    public void SaveAndQuit()
    {
        // Primero, le dice al SaveLoadManager que guarde el estado actual.
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame();
        }

        // Después, simplemente llama a la función de salir al menú.
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