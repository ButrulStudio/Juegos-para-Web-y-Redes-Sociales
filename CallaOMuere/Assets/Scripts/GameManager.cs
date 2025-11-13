using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // ¡NUEVO! Necesario para los Sliders
using UnityEngine.Audio; // ¡NUEVO! Necesario para el AudioMixer

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

    // --- ¡NUEVAS VARIABLES PARA LOS AJUSTES DE PAUSA! ---
    [Header("Ajustes del Menú de Pausa")]
    [Tooltip("Arrastra el slider de Sensibilidad de tu pausePanel")]
    [SerializeField] private Slider sensitivitySlider_Pause;
    [Tooltip("Arrastra el slider de Música de tu pausePanel")]
    [SerializeField] private Slider musicSlider_Pause;
    [Tooltip("Arrastra el slider de SFX de tu pausePanel")]
    [SerializeField] private Slider sfxSlider_Pause;
    [Tooltip("Arrastra tu 'MainMixer' (el mismo que en OptionsMenu)")]
    [SerializeField] private AudioMixer mainAudioMixer;
    [Tooltip("Arrastra el objeto 'Camera' que tiene el CameraController")]
    [SerializeField] private CameraController cameraController;
    // --- FIN NUEVAS VARIABLES ---

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

        // --- ¡NUEVA LLAMADA! ---
        // Configura los sliders del menú de pausa
        SetupPauseMenuSliders();
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
            // --- ¡NUEVA LLAMADA! ---
            // Actualiza los sliders a los valores guardados CADA VEZ que abres el menú
            if (sensitivitySlider_Pause != null) // Solo si los has asignado
            {
                LoadCurrentSettingsToSliders();
            }
            // ---
            pausePanel.SetActive(true);
            Time.timeScale = 0;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    // --- ¡SECCIÓN DE MÉTODOS COMPLETAMENTE NUEVA! ---

    //  Se llama en Start() para configurar los listeners de los sliders

    private void SetupPauseMenuSliders()
    {
        // Si no has asignado los sliders en el inspector, no hagas nada
        if (sensitivitySlider_Pause == null || musicSlider_Pause == null || sfxSlider_Pause == null)
        {
            Debug.LogWarning("No se han asignado todos los Sliders de opciones en el GameManager. Las opciones del menú de pausa no funcionarán.");
            return;
        }

        // Carga los valores iniciales (por si acaso)
        LoadCurrentSettingsToSliders();

        // Asigna los "listeners" (qué pasa cuando se mueven)
        sensitivitySlider_Pause.onValueChanged.AddListener(SetSensitivity_Pause);
        musicSlider_Pause.onValueChanged.AddListener(SetMusicVolume_Pause);
        sfxSlider_Pause.onValueChanged.AddListener(SetSFXVolume_Pause);
    }

    // Lee los PlayerPrefs y actualiza los sliders del menú de pausa

    private void LoadCurrentSettingsToSliders()
    {
        // Usamos las MISMAS claves que en OptionsMenu.cs para que estén sincronizados
        sensitivitySlider_Pause.value = PlayerPrefs.GetFloat("MasterSensitivity", 100f);
        musicSlider_Pause.value = PlayerPrefs.GetFloat("MasterMusicVolume", 0.75f);
        sfxSlider_Pause.value = PlayerPrefs.GetFloat("MasterSFXVolume", 0.75f);
    }

    // Esta función debe llamarse desde el OnValueChanged() del SLIDER DE SENSIBILIDAD

    public void SetSensitivity_Pause(float value)
    {
        PlayerPrefs.SetFloat("MasterSensitivity", value);

        // Actualiza la cámara en tiempo real
        if (cameraController != null)
        {
            cameraController.SetSensibility(value);
        }
    }

    public void SetMusicVolume_Pause(float value)
    {
        if (mainAudioMixer == null) return;
        // Convierte el valor lineal (0.0001-1) a logarítmico (decibelios)
        mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterMusicVolume", value);
    }

    public void SetSFXVolume_Pause(float value)
    {
        if (mainAudioMixer == null) return;
        mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MasterSFXVolume", value);
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