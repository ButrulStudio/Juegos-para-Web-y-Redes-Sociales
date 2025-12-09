using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("--- DEBUG (SOLO EDITOR) ---")]
    [Tooltip("Actívalo para probar controles táctiles. Desactívalo para jugar con ratón.")]
    public bool simulateMobileInEditor = true; 

    [Header("Configuración Inicial")]
    public WeaponData startingWeaponAsset;

    [Header("UI Móvil")]
    [SerializeField] private GameObject mobilePauseButtonHUD;

    [Header("Referencias Generales")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator transitionAnimator;
    [SerializeField] private Slider sensitivitySlider_Pause;
    [SerializeField] private Slider musicSlider_Pause;
    [SerializeField] private Slider sfxSlider_Pause;
    [SerializeField] private AudioMixer mainAudioMixer;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private AudioClip backgroundMusic;

    private bool isTogglingPause = false;
    public static bool IsPaused { get; private set; } = false;
    public static bool GameIsOver { get; private set; } = false;

    private PlayerShooting playerShooting;

    void Awake()
    {
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    void Start()
    {
        playerShooting = FindObjectOfType<PlayerShooting>();

        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);

        UpdateGameMode();

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

        if (playerShooting != null && startingWeaponAsset != null)
        {
            playerShooting.InitializeNewGame(startingWeaponAsset);
        }
    }
    void OnValidate()
    {

        if (Application.isPlaying)
        {
            UpdateGameMode();
        }
    }

    private void UpdateGameMode()
    {
        bool isMobileMode = false;

#if UNITY_EDITOR

        isMobileMode = simulateMobileInEditor;
#elif UNITY_ANDROID || UNITY_IOS
            // En móvil real, siempre es true
            isMobileMode = true;
#else
            // En PC Build, siempre es false
            isMobileMode = false;
#endif

        if (mobilePauseButtonHUD != null)
        {
            mobilePauseButtonHUD.SetActive(isMobileMode);
        }

        if (isMobileMode)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {

            if (!IsPaused)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        if (playerShooting == null) playerShooting = FindObjectOfType<PlayerShooting>();

        if (playerShooting != null)
        {
            playerShooting.useAutoFire = isMobileMode;
        }
    }


    private void Update()
    {

#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F5))
        {
            simulateMobileInEditor = !simulateMobileInEditor;
            UpdateGameMode();
            Debug.Log("Modo Móvil: " + simulateMobileInEditor);
        }
#endif

        if (Input.GetKeyDown(KeyCode.Escape) && !isTogglingPause) TogglePause();
    }


    public void TogglePause() { if (!isTogglingPause) StartCoroutine(TogglePauseCoroutine()); }

    public IEnumerator TogglePauseCoroutine()
    {
        isTogglingPause = true;
        if (pausePanel.activeSelf) 
        {
            animator.SetBool("Mobile", false); Time.timeScale = 1; IsPaused = false;
            yield return new WaitForSecondsRealtime(0.3f);
            pausePanel.SetActive(false);

            UpdateGameMode();
        }
        else 
        {
            if (sensitivitySlider_Pause != null) LoadCurrentSettingsToSliders();
            if (mobilePauseButtonHUD != null) mobilePauseButtonHUD.SetActive(false);
            pausePanel.SetActive(true); animator.SetBool("Mobile", true);
            yield return new WaitForSecondsRealtime(0.3f);
            Time.timeScale = 0; IsPaused = true; Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
        }
        isTogglingPause = false;
    }

    public void PlayerDied() { Time.timeScale = 0; GameIsOver = true; if (gameOverPanel != null) gameOverPanel.SetActive(true); Cursor.visible = true; Cursor.lockState = CursorLockMode.None; }
    private void SetupPauseMenuSliders() { if (sensitivitySlider_Pause == null) return; LoadCurrentSettingsToSliders(); sensitivitySlider_Pause.onValueChanged.AddListener(SetSensitivity_Pause); musicSlider_Pause.onValueChanged.AddListener(SetMusicVolume_Pause); sfxSlider_Pause.onValueChanged.AddListener(SetSFXVolume_Pause); }
    private void LoadCurrentSettingsToSliders() { sensitivitySlider_Pause.value = PlayerPrefs.GetFloat("MasterSensitivity", 100f); musicSlider_Pause.value = PlayerPrefs.GetFloat("MasterMusicVolume", 0.75f); sfxSlider_Pause.value = PlayerPrefs.GetFloat("MasterSFXVolume", 0.75f); }
    public void SetSensitivity_Pause(float value) { PlayerPrefs.SetFloat("MasterSensitivity", value); if (cameraController != null) cameraController.SetSensibility(value); }
    public void SetMusicVolume_Pause(float value) { if (mainAudioMixer == null) return; mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20); PlayerPrefs.SetFloat("MasterMusicVolume", value); }
    public void SetSFXVolume_Pause(float value) { if (mainAudioMixer == null) return; mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20); PlayerPrefs.SetFloat("MasterSFXVolume", value); }
    public void QuitToMainMenu() { StartCoroutine(QuitToMainMenuCoroutine()); }
    private IEnumerator QuitToMainMenuCoroutine() { if (transitionAnimator != null) transitionAnimator.SetTrigger("StartTransition"); yield return new WaitForSecondsRealtime(0.3f); PowerUpStore.ResetOwnedPowerUps(); WeaponStore.ResetOwnedWeapons(); Time.timeScale = 1; Cursor.visible = true; Cursor.lockState = CursorLockMode.None; SceneManager.LoadScene("MainMenu"); }
    public void RetryButton() { StartCoroutine(RetryCoroutine()); }
    private IEnumerator RetryCoroutine() { if (transitionAnimator != null) transitionAnimator.SetTrigger("StartTransition"); yield return new WaitForSecondsRealtime(0.3f); PowerUpStore.ResetOwnedPowerUps(); WeaponStore.ResetOwnedWeapons(); Time.timeScale = 1; Cursor.visible = true; Cursor.lockState = CursorLockMode.None; SceneManager.LoadScene(SceneManager.GetActiveScene().name); }
}