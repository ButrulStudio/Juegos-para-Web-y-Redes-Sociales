using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class SceneMusic
{
    public string sceneName;
    public AudioClip musicClip;

    [Range(0.1f, 3f)]
    public float musicPitch = 1f; // Permite variar el pitch (velocidad/tono) de la música por escena.
}

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    // --- Implementación del Patrón Singleton ---
    // 'instance' es estática para ser accesible globalmente.
    public static MusicManager instance;

    // Array configurable en el inspector.
    // Aquí es donde arrastramos las escenas y sus clips de música.
    [SerializeField] private SceneMusic[] sceneMusicMap;

    // Referencia al componente que reproduce la música.
    private AudioSource audioSource;

    private void Awake()
    {
        // Lógica estándar del Singleton
        if (instance == null)
        {
            // Si soy el primero, me asigno como la instancia única.
            instance = this;
            // ¡Clave! Evita que este GameObject se destruya al cargar una nueva escena.
            DontDestroyOnLoad(gameObject);

            // Obtenemos y configuramos el AudioSource.
            audioSource = GetComponent<AudioSource>();
            audioSource.loop = true; // La música de fondo siempre debe ser un loop.
        }
        else
        {
            // Si ya existe una instancia (ej. al volver al menú principal),
            // me destruyo a mí mismo para evitar duplicados.
            Destroy(gameObject);
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        // Es buena práctica limpiar los 'delegates' al deshabilitar/destruir el objeto.
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string newSceneName = scene.name;

        // Buscamos en nuestro array (mapa) si tenemos música definida para esta escena.
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

        // --- Optimización ---
        // Si la música que queremos poner es LA MISMA que ya está sonando
        if (audioSource.clip == musicaDeEscena.musicClip)
        {
            audioSource.pitch = musicaDeEscena.musicPitch;
            return;
        }

        // Si la música es diferente:
        // 1. Paramos la música actual (si la hay).
        audioSource.Stop();
        // 2. Asignamos el nuevo clip y el nuevo pitch.
        audioSource.clip = musicaDeEscena.musicClip;
        audioSource.pitch = musicaDeEscena.musicPitch;
        // 3. Reproducimos la nueva música.
        audioSource.Play();
    }

    public AudioSource GetAudioSource()
    {
        return audioSource;
    }
}