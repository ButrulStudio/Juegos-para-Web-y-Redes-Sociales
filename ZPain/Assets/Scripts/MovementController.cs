using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Arrastra aquí la Main Camera (hija del jugador)")]
    [SerializeField] private Transform playerCameraRoot;

    [Header("Controles Móviles")] // --- NUEVO: Asigna aquí tu JoystickBG
    public VirtualJoystick mobileJoystick;

    [Header("Movimiento Base")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -15.0f;

    [Header("Correr (Mantener Shift)")]
    [SerializeField] private float sprintMultiplier = 1.5f;
    [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Agacharse (Mantener Control)")]
    [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;

    [Header("Configuración Física Agachado")]
    [Tooltip("Altura total del collider de pie")]
    [SerializeField] private float standingHeight = 2.0f;
    [Tooltip("Centro Y del collider de pie")]
    [SerializeField] private float standingCenterY = 0f;

    [Space(10)]
    [Tooltip("Altura total del collider agachado")]
    [SerializeField] private float crouchHeight = 1.0f;
    [Tooltip("Centro Y del collider agachado")]
    [SerializeField] private float crouchCenterY = 0f;

    [Space(10)]
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    [Header("Configuración Cámara")]
    [SerializeField] private float cameraStandY = 1.6f;
    [SerializeField] private float cameraCrouchY = 0.8f;

    [Header("Sonidos")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float sprintStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.8f;
    [SerializeField] private AudioClip[] footstepSounds;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float nextStepTime = 0f;

    private float currentSpeed;
    private float defaultSpeed;
    public float speedMultiplier = 1f;

    // --- VARIABLE NUEVA PARA AGACHARSE EN MÓVIL ---
    private bool isMobileCrouching = false;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        defaultSpeed = moveSpeed;
        currentSpeed = moveSpeed;

        // Inicializamos con los valores de "De Pie"
        controller.height = standingHeight;
        controller.center = new Vector3(0, standingCenterY, 0);

        if (playerCameraRoot == null)
        {
            Camera mainCam = GetComponentInChildren<Camera>();
            if (mainCam != null) playerCameraRoot = mainCam.transform;
        }
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (GameManager.IsPaused || GameManager.GameIsOver) return;

        // 1. Ground Check
        isGrounded = controller.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. Inputs (Teclado)
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // --- LÓGICA MÓVIL (JOYSTICK) ---
        // Si hay un joystick asignado y se está moviendo, sobrescribimos el teclado
        if (mobileJoystick != null && mobileJoystick.InputVector != Vector3.zero)
        {
            moveX = mobileJoystick.InputVector.x;
            moveZ = mobileJoystick.InputVector.z;
        }
        // -------------------------------

        // 3. Estados
        // Modificado para incluir el toggle móvil (Teclado O Móvil)
        bool isCrouchingInput = Input.GetKey(crouchKey) || isMobileCrouching;
        bool isSprintingInput = Input.GetKey(sprintKey) && !isCrouchingInput;

        // --- LÓGICA FÍSICA (INTERPOLACIÓN DE ALTURA Y CENTRO) ---
        float targetHeight = isCrouchingInput ? crouchHeight : standingHeight;
        float targetCenterY = isCrouchingInput ? crouchCenterY : standingCenterY;

        float currentHeight = controller.height;
        float currentCenterY = controller.center.y;

        // Si hay diferencia, interpolamos suavemente
        if (Mathf.Abs(currentHeight - targetHeight) > 0.01f || Mathf.Abs(currentCenterY - targetCenterY) > 0.01f)
        {
            float newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
            float newCenterY = Mathf.Lerp(currentCenterY, targetCenterY, crouchTransitionSpeed * Time.deltaTime);

            controller.height = newHeight;
            controller.center = new Vector3(0, newCenterY, 0);
        }

        // --- CÁMARA ---
        if (playerCameraRoot != null)
        {
            float targetCamY = isCrouchingInput ? cameraCrouchY : cameraStandY;
            Vector3 camPos = playerCameraRoot.localPosition;
            camPos.y = Mathf.Lerp(camPos.y, targetCamY, crouchTransitionSpeed * Time.deltaTime);
            playerCameraRoot.localPosition = camPos;
        }

        // 4. Velocidad
        float finalSpeed = currentSpeed;
        if (isCrouchingInput) finalSpeed = crouchSpeed;
        else if (isSprintingInput) finalSpeed = currentSpeed * sprintMultiplier;

        finalSpeed *= speedMultiplier;

        // 5. Mover
        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * finalSpeed * Time.deltaTime);

        // 6. Gravedad (Salto con Espacio)
        if (Input.GetButtonDown("Jump") && isGrounded && !isCrouchingInput)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // 7. Sonidos
        HandleFootsteps(move.magnitude > 0.1f, isCrouchingInput, isSprintingInput);
    }

    private void HandleFootsteps(bool isMoving, bool crouching, bool sprinting)
    {
        if (!isMoving || !isGrounded) return;

        if (Time.time > nextStepTime)
        {
            float interval = walkStepInterval;
            float volume = 1f;

            if (crouching) { interval = crouchStepInterval; volume = 0.3f; }
            else if (sprinting) { interval = sprintStepInterval; }

            if (audioSource != null && footstepSounds.Length > 0)
            {
                audioSource.PlayOneShot(footstepSounds[Random.Range(0, footstepSounds.Length)], volume);
            }
            nextStepTime = Time.time + interval;
        }
    }

    // --- MÉTODOS PÚBLICOS PARA LOS BOTONES DEL MÓVIL ---

    // Asigna esto al botón de SALTAR (OnClick)
    public void MobileJump()
    {
        // Solo salta si toca el suelo y NO está agachado
        if (isGrounded && !isMobileCrouching && !Input.GetKey(crouchKey))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    // Asigna esto al botón de AGACHARSE (OnClick)
    public void MobileToggleCrouch()
    {
        isMobileCrouching = !isMobileCrouching;
    }

    // --- PowerUps ---
    public void SetPermanentSpeedMultiplier(float multiplier) => speedMultiplier = multiplier;
    public float GetBaseSpeed() => defaultSpeed;
    public float GetVelocity() => currentSpeed;
    public void SetVelocity(float newSpeed) => currentSpeed = newSpeed;
    public void ResetVelocity() => currentSpeed = defaultSpeed;
    public float GetSprintMultiplier() => sprintMultiplier;
    public void SetSprintMultiplier(float newMultiplier) => sprintMultiplier = newMultiplier;
}
