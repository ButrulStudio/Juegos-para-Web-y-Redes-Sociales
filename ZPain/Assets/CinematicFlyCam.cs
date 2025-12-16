using UnityEngine;

public class CinematicFlyCam : MonoBehaviour
{
    [Header("Configuración de Velocidad")]
    public float velocidadNormal = 10f;
    public float velocidadRapida = 20f; // Mantén Shift
    public float velocidadLenta = 2f;   // Mantén Control

    [Header("Configuración de Ratón")]
    public float sensibilidad = 2f;

    // Variables privadas para guardar la rotación
    private float rotacionX = 0f;
    private float rotacionY = 0f;

    void Start()
    {
        // Oculta el ratón y lo bloquea en el centro al iniciar
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Inicializa la rotación actual para que no salte de golpe
        Vector3 rot = transform.localRotation.eulerAngles;
        rotacionY = rot.y;
        rotacionX = rot.x;
    }

    void Update()
    {
        // 1. ROTACIÓN (Mirar con el ratón)
        float mouseX = Input.GetAxis("Mouse X") * sensibilidad;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidad;

        rotacionY += mouseX;
        rotacionX -= mouseY;

        // Limita que no puedas dar la vuelta completa hacia arriba/abajo
        rotacionX = Mathf.Clamp(rotacionX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotacionX, rotacionY, 0);

        // 2. VELOCIDAD
        float velocidadActual = velocidadNormal;

        if (Input.GetKey(KeyCode.LeftShift))
        {
            velocidadActual = velocidadRapida;
        }
        else if (Input.GetKey(KeyCode.LeftControl))
        {
            velocidadActual = velocidadLenta;
        }

        // 3. MOVIMIENTO (WASD)
        float moveX = Input.GetAxis("Horizontal"); // A y D
        float moveZ = Input.GetAxis("Vertical");   // W y S
        float moveY = 0f;

        // Teclas para subir y bajar (Grúa)
        if (Input.GetKey(KeyCode.E)) moveY = 1f; // Subir
        if (Input.GetKey(KeyCode.Q)) moveY = -1f; // Bajar

        Vector3 direccion = new Vector3(moveX, moveY, moveZ);

        // Moverse en la dirección hacia donde mira la cámara
        transform.Translate(direccion * velocidadActual * Time.deltaTime);

        // 4. DESBLOQUEAR RATÓN (Si presionas ESC)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}