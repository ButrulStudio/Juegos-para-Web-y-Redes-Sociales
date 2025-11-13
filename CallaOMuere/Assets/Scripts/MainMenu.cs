using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Animator transitionAnimator;

    public void Start()
    {
 
    }

    public IEnumerator LoadGameScene()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Tutorial");
    }

    // Conecta tu botón "Jugar" (o "Start") a esta función.
    public void StartGameButton()
    {
        // Comprueba si el SaveLoadManager existe Y si tiene un archivo guardado
        if (SaveLoadManager.Instance != null && SaveLoadManager.Instance.DoesSaveExist())
        {
            SaveLoadManager.Instance.SetLoadGameFlag(true);
        }
        else
        {
            // NO HAY GUARDADO: le decimos al GameManager que NO debe cargar (empezar de cero).
            SaveLoadManager.Instance.SetLoadGameFlag(false);
        }

        // En cualquier caso, cargamos la escena del juego.
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

}