using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneMusic
{
    public string sceneName;
    public AudioClip musicClip;

    [Range(0.1f, 3f)]
    public float musicPitch = 1f; 
}

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{

    public static MusicManager instance;

    [SerializeField] private SceneMusic[] sceneMusicMap;

    private AudioSource audioSource;

    private void Awake()
    {

        if (instance == null)
        {

            instance = this;

            DontDestroyOnLoad(gameObject);

            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true; 
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;

        SceneMusic musicaDeEscena = null;
        foreach (SceneMusic sm in sceneMusicMap)
        {
            if (sm.sceneName == newSceneName)
            {
                musicaDeEscena = sm;
                break;
            }
        }

        if (musicaDeEscena == null || musicaDeEscena.musicClip == null)
        {
            return;
        }

        if (audioSource.clip == musicaDeEscena.musicClip)
        {
            audioSource.pitch = musicaDeEscena.musicPitch;
            return;
        }

        audioSource.Stop();

        audioSource.clip = musicaDeEscena.musicClip;
        audioSource.pitch = musicaDeEscena.musicPitch;
 
        audioSource.Play();
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }
}