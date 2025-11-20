using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Sensibilidad")]
    [SerializeField] private float sensibility = 100f;

    [Header("Referencias")]
    // Referencia al transform del 'jugador' (cuerpo) para la rotación horizontal.
    public Transform jugador;

    [Header("Recoil")]
    [SerializeField] private float recoilRecoverySpeed = 5f;

    // Almacena el offset de retroceso aditivo (X=Vertical, Y=Horizontal).
    private Vector2 recoilOffset;

    [SerializeField, Range(0f, 1f)] private float recoilMultiplier = 0.01f;

    // Acumulador para la rotación vertical. Debe ser un campo de clase para persistir.
    private float verticalRotation = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;

        // Cargar la sensibilidad del usuario o usar el valor 'sensibility' como fallback.
        sensibility = PlayerPrefs.GetFloat("MasterSensitivity", this.sensibility);
    }

    void Update()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver)
            return;

        float mouseX = Input.GetAxis("Mouse X") * sensibility * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibility * Time.deltaTime;

        // --- Rotación Vertical ---

        verticalRotation -= mouseY;
        verticalRotation -= recoilOffset.x;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f); // Asi no se parte el cuello

        // --- Rotación Horizontal ---
        jugador.Rotate(Vector3.up * (mouseX + recoilOffset.y));

        // --- Aplicación Final ---
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Interpolar suavemente el offset de retroceso de vuelta a cero.
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
    }

    /// <summary>
    /// Funcion publica para que otros scripts (ej. PlayerShooting)
    /// </summary>
    public void AddRecoil(float vertical, float horizontal)
    {
        recoilOffset += new Vector2(vertical, horizontal) * recoilMultiplier;
    }

    /// <summary>
    /// Funcion pública para que los menús de opciones
    /// actualicen la sensibilidad en tiempo real.
    /// </summary>
    public void SetSensibility(float newSensibility)
    {
        sensibility = newSensibility;
    }
}