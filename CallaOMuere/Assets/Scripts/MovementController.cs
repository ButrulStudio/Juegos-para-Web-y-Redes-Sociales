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

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;

    private float defaultSpeed;       // Velocidad base normal
    private float currentSpeed;       // Velocidad actual (puede ser modificada)
    public float speedMultiplier = 1f; // Multiplicador temporal por PowerUps

    void Start()
    {
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

        // Sprint
        float targetSpeed = currentSpeed * speedMultiplier;
        if (Input.GetKey(KeyCode.LeftShift))
            targetSpeed *= sprintMultiplier;

        controller.Move(move * targetSpeed * Time.deltaTime);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Gravedad
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    // ---------------- MÉTODOS PARA POWERUPS ----------------

    /// <summary>
    /// Aplica un multiplicador de velocidad temporal por PowerUp
    /// </summary>
    public void ApplySpeedMultiplier(float multiplier, float duration)
    {
        StopAllCoroutines();
        StartCoroutine(SpeedCoroutine(multiplier, duration));
    }

    private IEnumerator SpeedCoroutine(float multiplier, float duration)
    {
        speedMultiplier = multiplier;
        yield return new WaitForSeconds(duration);
        speedMultiplier = 1f;
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
