using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class OptionsMenu : MonoBehaviour
{
    [Header("Componentes UI")]
    // Referencias a los sliders de la interfaz de usuario.
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Audio")]
    // Referencia al AudioMixer principal para ajustar los volúmenes.
    [SerializeField] private AudioMixer mainAudioMixer;

    // Usamos claves constantes para evitar errores de tipeo al acceder a PlayerPrefs.
    public const string SENS_KEY = "MasterSensitivity";
    public const string MUSIC_VOL_KEY = "MasterMusicVolume";
    public const string SFX_VOL_KEY = "MasterSFXVolume";

    void Start()
    {
        // 1. Cargar los valores guardados al iniciar esta escena.
        LoadSettings();

        // 2. Suscribir los métodos a los eventos 'onValueChanged' de los sliders.
        //    Esto hace que al mover un slider, se llame a la función correspondiente en tiempo real.
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    /// <summary>
    /// Carga los valores guardados en PlayerPrefs y actualiza los sliders
    /// y el AudioMixer para que reflejen esos valores.
    /// </summary>
    private void LoadSettings()
    {
        // Cargar sensibilidad (o usar 100f como valor por defecto si no existe).
        float sensitivity = PlayerPrefs.GetFloat(SENS_KEY, 100f);
        sensitivitySlider.value = sensitivity;

        // Cargar volumen de música (o 0.75f por defecto).
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.75f);
        musicSlider.value = musicVol;
        SetMusicVolume(musicVol); // Aplicar el valor al AudioMixer al cargar.

        // Cargar volumen de SFX (o 0.75f por defecto).
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f);
        sfxSlider.value = sfxVol;
        SetSFXVolume(sfxVol); // Aplicar el valor al AudioMixer al cargar.
    }

    /// <summary>
    /// Llamado por el 'listener' del slider de sensibilidad.
    /// Guarda el nuevo valor en PlayerPrefs.
    /// </summary>
    public void SetSensitivity(float value)
    {
        PlayerPrefs.SetFloat(SENS_KEY, value);
    }

    /// <summary>
    /// Llamado por el 'listener' del slider de música.
    /// Actualiza el AudioMixer y guarda el valor en PlayerPrefs.
    /// </summary>
    public void SetMusicVolume(float value)
    {
        // El AudioMixer usa una escala logarítmica (Decibelios).
        // Convertimos el valor lineal del slider (0.0001-1) a dB.
        mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);

        // Guardamos el valor lineal (0-1) en PlayerPrefs.
        PlayerPrefs.SetFloat(MUSIC_VOL_KEY, value);
    }

    /// <summary>
    /// Llamado por el 'listener' del slider de SFX.
    /// Actualiza el AudioMixer y guarda el valor en PlayerPrefs.
    /// </summary>
    public void SetSFXVolume(float value)
    {
        // Conversión idéntica a la de música, pero para el grupo "SFXVolume".
        mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat(SFX_VOL_KEY, value);
    }
}