using UnityEngine;
using UnityEngine.EventSystems;

public class TouchField : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    [HideInInspector]
    public Vector2 TouchDist;

    void Update()
    {
        TouchDist = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
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
}