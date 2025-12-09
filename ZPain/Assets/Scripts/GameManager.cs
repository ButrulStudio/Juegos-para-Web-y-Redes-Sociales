using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public static bool IsPaused { get; private set; } = false;
    public static bool GameIsOver { get; private set; } = false;

    [Header("Configuración Inicial")]
    [Tooltip("El arma con la que el jugador empezará la partida.")]
    public WeaponData startingWeaponAsset; // MOVIDO DESDE SAVELOADMANAGER

    [Header("UI (Paneles)")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator transitionAnimator;

    [Header("UI (Componentes)")]
    [SerializeField] private TextMeshProUGUI pauseText;

    [Header("Ajustes del Menú de Pausa")]
    [SerializeField] private Slider sensitivitySlider_Pause;
    [SerializeField] private Slider musicSlider_Pause;
    [SerializeField] private Slider sfxSlider_Pause;
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private CameraController cameraController;

    private bool isTogglingPause = false;

    [Header("Música de Ambiente")]
    [SerializeField] private AudioSource musicAudioSource;
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
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        // Estado inicial
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        Time.timeScale = 1;
        IsPaused = false;
        GameIsOver = false;

        SetupPauseMenuSliders();

        if (musicAudioSource != null && backgroundMusic != null)
        {
            musicAudioSource.clip = backgroundMusic;
            musicAudioSource.loop = true;
            musicAudioSource.Play();
        }

        // --- INICIO DE PARTIDA (SIN CARGA DE GUARDADO) ---
        Debug.Log("GameManager: Iniciando partida nueva.");

        PlayerShooting playerShooting = FindObjectOfType<PlayerShooting>();

        if (playerShooting != null && startingWeaponAsset != null)
        {
            playerShooting.InitializeNewGame(startingWeaponAsset);
        }
        else
        {
            if (startingWeaponAsset == null) Debug.LogWarning("GameManager: No has asignado el 'Starting Weapon Asset' en el Inspector.");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !isTogglingPause)
        {
            StartCoroutine(TogglePauseCoroutine());
        }
    }

    public void PlayerDied()
    {
        Debug.Log("Game Over. Player Died.");
        Time.timeScale = 0;
        GameIsOver = true;

        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Ya no borramos partida porque no existe sistema de guardado.
    }

    public IEnumerator TogglePauseCoroutine()
    {
        isTogglingPause = true;

        if (pausePanel.activeSelf)
        {
            // REANUDAR
            animator.SetBool("Mobile", false);
            Time.timeScale = 1;
            IsPaused = false;
            yield return new WaitForSecondsRealtime(0.3f);
            pausePanel.SetActive(false);
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // PAUSAR
            if (sensitivitySlider_Pause != null) LoadCurrentSettingsToSliders();
            pausePanel.SetActive(true);
            animator.SetBool("Mobile", true);
            yield return new WaitForSecondsRealtime(0.3f);
            Time.timeScale = 0;
            IsPaused = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        isTogglingPause = false;
    }

    private void SetupPauseMenuSliders()
    {
        if (sensitivitySlider_Pause == null || musicSlider_Pause == null || sfxSlider_Pause == null) return;
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
        if (cameraController != null) cameraController.SetSensibility(value);
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

    // Renombrado de SaveAndQuit a QuitToMainMenu ya que no guardamos.
    public void QuitToMainMenu()
    {
        StartCoroutine(QuitToMainMenuCoroutine());
    }

    private IEnumerator QuitToMainMenuCoroutine()
    {
        if (transitionAnimator != null) transitionAnimator.SetTrigger("StartTransition");

        yield return new WaitForSecondsRealtime(0.3f);

        // Reseteamos tiendas estáticas
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
        if (transitionAnimator != null) transitionAnimator.SetTrigger("StartTransition");

        yield return new WaitForSecondsRealtime(0.3f);

        PowerUpStore.ResetOwnedPowerUps();
        WeaponStore.ResetOwnedWeapons();

        Time.timeScale = 1;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}