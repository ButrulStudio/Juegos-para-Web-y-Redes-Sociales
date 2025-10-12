using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Sensibilidad")]
    [SerializeField] private float sensibility = 100f;

    [Header("Referencias")]
    public Transform jugador;

    [Header("Recoil")]
    [SerializeField] private float recoilRecoverySpeed = 5f;
    private Vector2 recoilOffset;
    [SerializeField, Range(0f, 1f)] private float recoilMultiplier = 0.01f;

    private float verticalRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // --- Entrada del ratón ---
        float mouseX = Input.GetAxis("Mouse X") * sensibility * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibility * Time.deltaTime;

        // --- Rotación vertical (mirar arriba/abajo) ---
        verticalRotation -= mouseY;
        verticalRotation -= recoilOffset.x; // recoil empuja la cámara hacia arriba
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        // --- Rotación horizontal ---
        jugador.Rotate(Vector3.up * (mouseX + recoilOffset.y));

        // --- Aplicar rotación a la cámara ---
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // --- Recuperación suave del recoil ---
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        recoilOffset += new Vector2(vertical, horizontal) * recoilMultiplier;
    }
}
