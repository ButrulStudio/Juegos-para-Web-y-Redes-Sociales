using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    public Animator transitionAnimator;


    public void Start()
    {
        // transitionAnimator = GetComponentInChildren<Animator>(); // <-- Comentado para evitar NullReferenceException
    }

    public IEnumerator LoadGameScene()
    {
        transitionAnimator.SetTrigger("StartTransition");
        yield return new WaitForSeconds(0.3f);
        SceneManager.LoadScene("Game");
    }


    public void StartGameButton()
    {
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

}