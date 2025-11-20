using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);

    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    public float speed = 10f;

    public AudioClip hoverSound;
    public AudioClip clickSound;

    // Volumen de los audios
    [Range(0.0f, 1.0f)] 
    public float hoverVolume = 1.0f;

    [Range(0.0f, 1.0f)]
    public float clickVolume = 1.0f;

    private RectTransform rect;
    private Image img;
    private bool isHovered = false;

    private AudioSource audioSource;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        // Animar tamaño
        if (rect != null)
        {
            rect.localScale = Vector3.Lerp(
                rect.localScale,
                isHovered ? hoverScale : normalScale,
                Time.deltaTime * speed
            );
        }

        // Animar color
        if (img != null)
        {
            img.color = Color.Lerp(
                img.color,
                isHovered ? hoverColor : normalColor,
                Time.deltaTime * speed
            );
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;

        // Reproducir audio dl hover
        if (hoverSound != null)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }

    // Reproducir audio del click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null)
        {
            audioSource.PlayOneShot(clickSound, clickVolume);
        }
    }
}