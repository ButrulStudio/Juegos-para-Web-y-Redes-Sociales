using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class VirtualJoystick : MonoBehaviour, IDragHandler, IPointerUpHandler, IPointerDownHandler
{
    private Image bgImage;
    private Image handleImage;
    public Vector3 InputVector { get; private set; }

    private void Start()
    {
        bgImage = GetComponent<Image>();
        handleImage = transform.GetChild(0).GetComponent<Image>();
    }

    public void OnDrag(PointerEventData ped)
    {
        Vector2 pos;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(bgImage.rectTransform, ped.position, ped.pressEventCamera, out pos))
        {
            pos.x = (pos.x / bgImage.rectTransform.sizeDelta.x);
            pos.y = (pos.y / bgImage.rectTransform.sizeDelta.y);

            InputVector = new Vector3(pos.x * 2 - 1, 0, pos.y * 2 - 1);
            InputVector = (InputVector.magnitude > 1.0f) ? InputVector.normalized : InputVector;

            handleImage.rectTransform.anchoredPosition = new Vector3(InputVector.x * (bgImage.rectTransform.sizeDelta.x / 3), InputVector.z * (bgImage.rectTransform.sizeDelta.y / 3));
        }
    }

    public void OnPointerDown(PointerEventData ped) { OnDrag(ped); }

    public void OnPointerUp(PointerEventData ped)
    {
        InputVector = Vector3.zero;
        handleImage.rectTransform.anchoredPosition = Vector3.zero;
    }
}