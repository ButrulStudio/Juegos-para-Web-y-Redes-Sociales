using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    [Header("Animador de Transición de Escena")]
    // Animator para controlar el fundido (fade) entre escenas.
    public Animator transitionAnimator;

    [Header("Animador del Panel del Mapa")]
    // Animator para mostrar/ocultar el panel de pausa
    public Animator mapPanelAnimator;

    [Header("Audio")]
    [SerializeField] private AudioMixer mainAudioMixer;

    // Claves estáticas para PlayerPrefs, evitan errores de tipeo.
    public const string MUSIC_VOL_KEY = "MasterMusicVolume";
    public const string SFX_VOL_KEY = "MasterSFXVolume";

    void Start()
    {
        // Al iniciar el menú, cargar los ajustes de audio guardados por el usuario.
        LoadAudioSettings();
    }

    /// <summary>
    /// Carga los valores de PlayerPrefs y los aplica al AudioMixer principal.
    /// </summary>
    private void LoadAudioSettings()
    {
        if (mainAudioMixer == null)
        {
            Debug.LogWarning("MainMenu: No se ha asignado el AudioMixer.");
            return;
        }

        // Cargar volumen de música, usar 0.75f como fallback.
        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.75f);
        // Convertir valor lineal (0-1) a logarítmico (dB) para el mixer.
        mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVol) * 20);

        // Cargar volumen de SFX, usar 0.75f como fallback.
        float sfxVol = PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f);
        mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVol) * 20);
    }

    // --- Lógica de Carga de Escenas ---

    // Todas las funciones de carga usan corrutinas para permitir que la animación
    // de transición se reproduzca *antes* de cargar la nueva escena.

    public IEnumerator LoadGameScene()
    {
        transitionAnimator.SetTrigger("StartTransition");
        // Esperar a que la animación de fundido termine (0.3s).
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Game");
    }

    /// <summary>
    /// Función pública llamada por el botón "Start Game".
    /// Comprueba si existe un guardado y ajusta la bandera en el SaveLoadManager.
    /// </summary>
    public void StartGameButton()
    {
        // Consultar al Singleton si existe un archivo de guardado.
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.DoesSaveExist())
        {
            // Si existe, marcar la bandera para que GameManager cargue la partida.
            SaveLoadManager.Instance.SetLoadGameFlag(true);
        }
        else
        {
            // Si no, marcar la bandera para que GameManager inicie una partida nueva.
            SaveLoadManager.Instance.SetLoadGameFlag(false);
        }
        StartCoroutine(LoadGameScene());
    }

    public IEnumerator LoadOptionsMenu()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("OptionsMenu");
    }

    // Método público para el botón de Opciones.
    public void StartOptionsButton()
    {
        StartCoroutine(LoadOptionsMenu());
    }

    public IEnumerator LoadCreditsScene()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Credits");
    }

    // Método público para el botón de Créditos.
    public void StartCreditsButton()
    {
        StartCoroutine(LoadCreditsScene());
    }

    public IEnumerator LoadMainMenu()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("MainMenu");
    }

    // Método público para botones de "Volver al Menú".
    public void StartMenuButton()
    {
        StartCoroutine(LoadMainMenu());
    }

    public IEnumerator LoadMapSelector()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("MapSelector");
    }

    public void StartMapSelector()
    {
        StartCoroutine(LoadMapSelector());
    }

    public IEnumerator LoadShop()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Shop");
    }

    public void StartShop()
    {
        StartCoroutine(LoadShop());
    }

    public IEnumerator QuitGame()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        Application.Quit();
    }

    public void StartQuit()
    {
        StartCoroutine(QuitGame());
    }


    // --- Control de Paneles UI ---

    /// <summary>
    /// Muestra un panel (ej. Mapa) activando un booleano en su Animator.
    /// </summary>
    public void ShowPanel()
    {
        if (mapPanelAnimator != null)
        {
            mapPanelAnimator.SetBool("IsOpen", true);
        }
        else
        {
            Debug.LogError("¡No has asignado el 'mapPanelAnimator' en el Inspector!");
        }
    }

    /// <summary>
    /// Oculta un panel (ej. Mapa) desactivando un booleano en su Animator.
    /// </summary>
    public void HidePanel()
    {
        if (mapPanelAnimator != null)
        {
            mapPanelAnimator.SetBool("IsOpen", false);
        }
        else
        {
            Debug.LogError("¡No has asignado el 'mapPanelAnimator' en el Inspector!");
        }
    }
}