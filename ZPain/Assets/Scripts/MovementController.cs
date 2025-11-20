using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Sonidos de Movimiento")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float walkStepInterval = 1f;
    [SerializeField] private float sprintStepInterval = 0.5f;
    [SerializeField] private AudioClip[] footstepSounds;

    // Control de tiempo para el próximo sonido de paso.
    private float nextStepTime = 0f;

    private CharacterController controller;

    private Vector3 velocity;
    private bool isGrounded;

    // Almacena la velocidad de 'moveSpeed' al inicio.
    private float defaultSpeed;
    // Velocidad base actual, puede ser modificada por efectos de estado.
    private float currentSpeed;
    // Multiplicador de PowerUp
    public float speedMultiplier = 1f;

    void Start()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("MovementController: No se ha asignado un AudioSource para los pasos.");
        }

        controller = GetComponent<CharacterController>();
        // Cachear la velocidad inicial para poder resetearla.
        defaultSpeed = moveSpeed;
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        // Toda la lógica de movimiento está encapsulada en HandleMovement().
        HandleMovement();
    }

    /// <summary>
    /// Maneja todo el input de movimiento, gravedad y salto en cada frame.
    /// </summary>
    private void HandleMovement()
    {
        // Detiene todo movimiento si el juego está pausado.
        if (GameManager.IsPaused || GameManager.GameIsOver)
            return;

        // Comprobar si el CharacterController está tocando el suelo.
        isGrounded = controller.isGrounded;

        // Si estamos en el suelo y la velocidad Y es negativa, resetearla a un valor bajo.
        // Esto evita que 'velocity.y' acumule gravedad indefinidamente.
        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // --- Input Horizontal ---
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // Calcula el vector de movimiento relativo a la rotación actual del jugador
        // (transform.right y transform.forward) en lugar de ejes globales.
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // --- Lógica de Sonido de Pasos ---
        bool isMoving = move.magnitude > 0.1f;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        if (isMoving && isGrounded)
        {
            // Usar Time.time para un cooldown simple de sonido de pasos.
            if (Time.time > nextStepTime)
            {
                // Seleccionar el intervalo basado en si está esprintando.
                float interval = isSprinting ? sprintStepInterval : walkStepInterval;

                PlayRandomFootstep();

                // Programar el siguiente paso.
                nextStepTime = Time.time + interval;
            }
        }

        // --- Aplicación de Velocidad ---
        float targetSpeed = currentSpeed * speedMultiplier;
        if (isSprinting)
            targetSpeed *= sprintMultiplier;

        // Mover el CharacterController horizontalmente.
        controller.Move(move * targetSpeed * Time.deltaTime);

        // --- Salto ---
        // Comprobar input de salto y si está en el suelo.
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Aplicar la fórmula de salto (v = sqrt(h * -2 * g))
            // para calcular la velocidad vertical necesaria para alcanzar 'jumpHeight'.
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // --- Gravedad ---
        // Acumular gravedad a la velocidad vertical.
        velocity.y += gravity * Time.deltaTime;
        // Mover el CharacterController verticalmente.
        // Es importante llamar a .Move() dos veces (una para horizontal, otra para vertical)
        // para que la gravedad y el movimiento no interfieran.
        controller.Move(velocity * Time.deltaTime);
    }

    /// <summary>
    /// Reproduce un sonido de paso aleatorio del array 'footstepSounds'.
    /// </summary>
    private void PlayRandomFootstep()
    {
        // Comprobaciones para evitar errores si no hay AudioSource o clips asignados.
        if (audioSource != null && footstepSounds != null && footstepSounds.Length > 0)
        {
            int index = Random.Range(0, footstepSounds.Length);
            AudioClip clip = footstepSounds[index];

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    // ---------------- MÉTODOS PARA POWERUPS ----------------
    // Funcion pública para que otros scripts (PowerUpManager) modifiquen la velocidad.

    /// <summary>
    /// Establece el multiplicador de velocidad permanente (ej. por un PowerUp).
    /// </summary>
    public void SetPermanentSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // --- Getters y Setters para controlar el estado de velocidad ---

    public float GetBaseSpeed() => defaultSpeed;

    public float GetVelocity() => currentSpeed;

    public void SetVelocity(float newSpeed) => currentSpeed = newSpeed;

    public void ResetVelocity() => currentSpeed = defaultSpeed;

    public float GetSprintMultiplier() => sprintMultiplier;

    public void SetSprintMultiplier(float newMultiplier) => sprintMultiplier = newMultiplier;
}