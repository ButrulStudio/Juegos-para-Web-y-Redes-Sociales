using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    [Header("Animador de Transición de Escena")]
    public Animator transitionAnimator;

    [Header("Animador del Panel del Mapa")]
    public Animator mapPanelAnimator;

    [Header("Audio")]
    [SerializeField] private AudioMixer mainAudioMixer;

    public const string MUSIC_VOL_KEY = "MasterMusicVolume";
    public const string SFX_VOL_KEY = "MasterSFXVolume";

    void Start()
    {
        LoadAudioSettings();
    }

    private void LoadAudioSettings()
    {
        if (mainAudioMixer == null)
        {
            Debug.LogWarning("MainMenu: No se ha asignado el AudioMixer.");
            return;
        }

        float musicVol = PlayerPrefs.GetFloat(MUSIC_VOL_KEY, 0.75f);
        mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(musicVol) * 20);

        float sfxVol = PlayerPrefs.GetFloat(SFX_VOL_KEY, 0.75f);
        mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(sfxVol) * 20);
    }

    public IEnumerator LoadGameScene()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Game");
    }

    public void StartGameButton()
    {
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.DoesSaveExist())
        {
            SaveLoadManager.Instance.SetLoadGameFlag(true);
        }
        else
        {
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