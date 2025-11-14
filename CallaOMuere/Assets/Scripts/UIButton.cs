using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Vector3 normalScale = Vector3.one;
    public Vector3 hoverScale = new Vector3(1.1f, 1.1f, 1.1f);

    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    public float speed = 10f;

    private RectTransform rect;
    private Image img;
    private bool isHovered = false;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        img = GetComponent<Image>();
    }

    void Update()
    {
        // Animar tamaño
        rect.localScale = Vector3.Lerp(
            rect.localScale,
            isHovered ? hoverScale : normalScale,
            Time.deltaTime * speed
        );

        // Animar color
        img.color = Color.Lerp(
            img.color,
            isHovered ? hoverColor : normalColor,
            Time.deltaTime * speed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}