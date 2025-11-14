using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("Animador de Transición de Escena")]
    public Animator transitionAnimator;

    [Header("Animador del Panel del Mapa")]
    public Animator mapPanelAnimator; 


    public void Start()
    {

    }

    public IEnumerator LoadGameScene()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Tutorial");
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
            // Usa el animador del PANEL
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
            // Usa el animador del PANEL
            mapPanelAnimator.SetBool("IsOpen", false);
        }
        else
        {
            Debug.LogError("¡No has asignado el 'mapPanelAnimator' en el Inspector!");
        }
    }

}