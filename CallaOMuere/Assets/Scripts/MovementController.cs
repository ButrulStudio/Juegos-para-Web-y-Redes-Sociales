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
    [Tooltip("El AudioSource para los sonidos de pasos, saltos, etc.")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float walkStepInterval = 1f;
    [SerializeField] private float sprintStepInterval = 0.5f;
    [Tooltip("Array de sonidos de pasos para que suenen aleatorios")]
    [SerializeField] private AudioClip[] footstepSounds;

    private float nextStepTime = 0f;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float defaultSpeed;       // Velocidad base normal
    private float currentSpeed;       // Velocidad actual (puede ser modificada)
    public float speedMultiplier = 1f; // Multiplicador temporal por PowerUps

    void Start()
    {
        if (audioSource == null)
        {
            Debug.LogWarning("MovementController: No se ha asignado un AudioSource para los pasos.");
        }

        controller = GetComponent<CharacterController>();
        defaultSpeed = moveSpeed;
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Comprobar si está tocando el suelo
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0)
            velocity.y = -2f;

        // Input de movimiento
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        // Comprobar si nos estamos moviendo (magnitud > 0)
        bool isMoving = move.magnitude > 0.1f;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // --- AÑADIR: Lógica de Sonido de Pasos ---
        if (isMoving && isGrounded)
        {
            // Comprobar si es hora de un nuevo paso
            if (Time.time > nextStepTime)
            {
                // Elegir el intervalo correcto
                float interval = isSprinting ? sprintStepInterval : walkStepInterval;

                // Reproducir sonido
                PlayRandomFootstep();

                // Asignar el tiempo para el siguiente paso
                nextStepTime = Time.time + interval;
            }
        }
        // Sprint
        // Esta línea ya aplica el multiplicador de power-up permanentemente
        float targetSpeed = currentSpeed * speedMultiplier;
        if (isSprinting)
            targetSpeed *= sprintMultiplier;

        controller.Move(move * targetSpeed * Time.deltaTime);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void PlayRandomFootstep()
    {
        if (audioSource != null && footstepSounds != null && footstepSounds.Length > 0)
        {
            // Elige un clip aleatorio del array
            int index = Random.Range(0, footstepSounds.Length);
            AudioClip clip = footstepSounds[index];

            // Reproduce ese clip
            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }
    }

    // ---------------- MÉTODOS PARA POWERUPS ----------------

    /// <summary>
    /// Establece un multiplicador de velocidad permanente por PowerUp
    /// </summary>
    public void SetPermanentSpeedMultiplier(float multiplier)
    {
        speedMultiplier = multiplier;
    }

    // Velocidad base normal
    public float GetBaseSpeed() => defaultSpeed;

    // Velocidad actual (sin sprint)
    public float GetVelocity() => currentSpeed;

    // Cambiar velocidad base (sin sprint)
    public void SetVelocity(float newSpeed) => currentSpeed = newSpeed;

    // Restaurar velocidad base normal
    public void ResetVelocity() => currentSpeed = defaultSpeed;

    // Multiplicador de sprint
    public float GetSprintMultiplier() => sprintMultiplier;

    // Cambiar multiplicador de sprint
    public void SetSprintMultiplier(float newMultiplier) => sprintMultiplier = newMultiplier;
}