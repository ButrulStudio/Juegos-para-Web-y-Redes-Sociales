using UnityEngine;
using UnityEngine.UI; // Necesario para el Slider
using UnityEngine.Audio; // Necesario para el AudioMixer

// Se eliminó 'UnityEngine.SceneManagement' porque ya no es necesario aquí
public class OptionsMenu : MonoBehaviour
{
    [Header("Componentes UI")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Audio")]
    [SerializeField] private AudioMixer mainAudioMixer;

    // Usamos claves constantes para evitar errores de tipeo
    public const string SENS_KEY = "MasterSensitivity";
    public const string MUSIC_VOL_KEY = "MasterMusicVolume";
    public const string SFX_VOL_KEY = "MasterSFXVolume";

    // --- Se eliminaron las variables 'mainMenu' y 'backButton' ---

    void Start()
    {
        // 1. Cargar valores guardados y actualizar los Sliders
        LoadSettings();

        // 2. Asignar los "listeners" (qué pasa cuando se mueven)
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // --- Se eliminó la lógica del botón "Volver" de aquí ---
    }

    /// <summary>
    /// Carga los PlayerPrefs y los aplica a los sliders/mixers
    /// </summary>
    private void LoadSettings()
    {
        // Cargar Sensibilidad (con un valor por defecto de 100 si no existe)
        float sensitivity = PlayerPrefs.GetFloat(SENS_KEY, 100f);
        sensitivitySlider.value = sensitivity;

        // Cargar Música (con un valor por defecto de 0.75 si no existe)
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.75f);
        musicSlider.value = musicVol;
        SetMusicVolume(musicVol); // Aplicarlo al AudioMixer

        // Cargar SFX (con un valor por defecto de 0.75 si no existe)
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f);
        sfxSlider.value = sfxVol;
        SetSFXVolume(sfxVol); // Aplicarlo al AudioMixer
    }

    // --- MÉTODOS PÚBLICOS (para los Sliders) ---

    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SENS_KEY, value);
    }

    public void SetMusicVolume(float value)
    {
        mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, value);
    }

    public void SetSFXVolume(float value)
    {
        mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(SFX_VOL_KEY, value);
    }
}