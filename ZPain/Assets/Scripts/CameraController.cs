using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform playerBody;

    [Tooltip("Arrastra aquí tu objeto TouchField (el cuadrado blanco del Canvas)")]
    public TouchField touchField;

    [Header("Ajustes")]
    public float mouseSensitivity = 100f;
    [Tooltip("Sensibilidad para el móvil (prueba con 0.2 o 0.5)")]
    public float mobileSensitivity = 0.2f;

    private float xRotation = 0f;

    private float sensitivityMultiplier = 1f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameManager.IsPaused) return;

        float mouseX = 0;
        float mouseY = 0;


        if (Application.isMobilePlatform)
        {
            if (touchField != null)
            {
                mouseX = touchField.TouchDist.x * mobileSensitivity * sensitivityMultiplier;
                mouseY = touchField.TouchDist.y * mobileSensitivity * sensitivityMultiplier;
            }
        }
        else
        {
            float currentSens = mouseSensitivity * sensitivityMultiplier;
            mouseX = Input.GetAxis("Mouse X") * currentSens * Time.deltaTime;
            mouseY = Input.GetAxis("Mouse Y") * currentSens * Time.deltaTime;
        }


        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    public void SetSensitivityMultiplier(float multiplier)
    {
        sensitivityMultiplier = multiplier;
    }

    public void SetSensibility(float value)
    {
        mouseSensitivity = value;
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        xRotation -= vertical;
        playerBody.Rotate(Vector3.up * horizontal);
    }
}