using UnityEngine;
using UnityEngine.EventSystems;

public class TouchField : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [HideInInspector]
    public Vector2 TouchDist;

    public void OnDrag(PointerEventData eventData)
    {
        // Guardamos cuánto se ha movido el dedo en este frame
        TouchDist = eventData.delta;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        TouchDist = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        TouchDist = Vector2.zero;
    }

    void LateUpdate()
    {
        TouchDist = Vector2.zero;
    }
}