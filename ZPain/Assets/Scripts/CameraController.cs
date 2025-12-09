using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Sensibilidad")]
    [SerializeField] private float sensibility = 100f;
    [Tooltip("Multiplicador para ajustar la velocidad del dedo respecto al ratón")]
    [SerializeField] private float mobileSensitivityMultiplier = 0.2f;

    [Header("Referencias")]
    public Transform jugador;

    [Header("Controles Móviles")]
    public TouchField mobileTouchField;

    [Header("Recoil")]
    [SerializeField] private float recoilRecoverySpeed = 5f;

    private Vector2 recoilOffset;
    [SerializeField, Range(0f, 1f)] private float recoilMultiplier = 0.01f;
    private float verticalRotation = 0f;
    private float sensitivityMultiplier = 1f;

    void Start()
    {
        // Cargamos la sensibilidad guardada
        sensibility = PlayerPrefs.GetFloat("MasterSensitivity", this.sensibility);

        // NOTA: Ya no gestionamos el Cursor.lockState aquí.
        // El GameManager se encarga de eso según el modo (PC/Móvil).
    }

    void Update()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver)
            return;

        // 1. INPUT DEL RATÓN (PC)
        // Leemos esto SIEMPRE. Si no mueves el ratón, valdrá 0.
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // 2. INPUT TÁCTIL (MÓVIL)
        // Solo sumamos el input táctil si el objeto está activo en la jerarquía
        // (El GameManager desactiva el HUD en modo PC, así que esto será seguro)
        if (mobileTouchField != null && mobileTouchField.gameObject.activeInHierarchy)
        {
            mouseX += mobileTouchField.TouchDist.x * mobileSensitivityMultiplier;
            mouseY += mobileTouchField.TouchDist.y * mobileSensitivityMultiplier;
        }

        // 3. APLICAR SENSIBILIDAD Y TIEMPO
        // Multiplicamos el valor final acumulado
        float finalInputX = mouseX * sensibility * sensitivityMultiplier * Time.deltaTime;
        float finalInputY = mouseY * sensibility * sensitivityMultiplier * Time.deltaTime;

        // --- Rotación Vertical (Cabeza) ---
        verticalRotation -= finalInputY;
        verticalRotation -= recoilOffset.x;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        // --- Rotación Horizontal (Cuerpo) ---
        jugador.Rotate(Vector3.up * (finalInputX + recoilOffset.y));

        // --- Aplicación Final ---
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Interpolar suavemente el offset de retroceso de vuelta a cero
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
    }

    public void AddRecoil(float vertical, float horizontal)
    {
        recoilOffset += new Vector2(vertical, horizontal) * recoilMultiplier;
    }

    public void SetSensibility(float newSensibility)
    {
        sensibility = newSensibility;
    }

    public void SetSensitivityMultiplier(float multiplier)
    {
        sensitivityMultiplier = multiplier;
    }
}