using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerController : MonoBehaviour
{
    // COMPONENTES DE UNITY
    [SerializeField] private CharacterController characterController;
    public Transform isGrounded;  // Empty para detectar colisión con el suelo
    [SerializeField] private TextMeshProUGUI scoreText;

    // MOVIMIENTO Y GRAVEDAD
    [SerializeField] private float velocity = 15f;
    [SerializeField] private float sprint = 1.5f;
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float doubleJumpForce = 1f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundDistance;
    [SerializeField] private LayerMask groundLayerMask;

    private Vector3 velocityVector;
    private bool isSprinting = false;
    private bool inGround;

    // ESTADOS DE SALTO
    public int jumps = 1;
    private bool jumped = false;

    // INPUT MOVIMIENTO
    private float inputX;
    private float inputZ;

    // MONEDAS
    private int score = 0;

    void Update()
    {
        // Detecta si está tocando el suelo
        inGround = Physics.CheckSphere(isGrounded.position, groundDistance, groundLayerMask);

        // Captura input horizontal y vertical
        inputX = Input.GetAxis("Horizontal");  // Teclas A y D
        inputZ = Input.GetAxis("Vertical");    // Teclas W y S

        // Detecta sprint (shift izquierdo)
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Maneja el salto simple y doble salto
        if (Input.GetButtonDown("Jump") && jumps > 0 && inGround)
        {
            jumps = 1;
            jumped = true;
        }

    }

    void FixedUpdate()
    {
        Vector3 move = transform.right * inputX + transform.forward * inputZ;

        // Aplica salto simple 
        if (jumped)
        {
            jumps--;
            velocityVector.y = Mathf.Sqrt(gravity * -2 * jumpForce);
            jumped = false;
        }


        // Resetea la velocidad vertical cuando está en el suelo
        if (inGround && velocityVector.y < 0)
        {
            velocityVector.y = -2f;
            jumps = 2;
        }

        // Aplica gravedad constante
        velocityVector.y += gravity * Time.fixedDeltaTime;

        // Calcula movimiento final con o sin sprint
        Vector3 finalMovement = move * (isSprinting ? velocity * sprint : velocity);

        // Mueve el CharacterController con la suma de movimiento y gravedad
        characterController.Move((finalMovement + velocityVector) * Time.fixedDeltaTime);
    }

    // Método para añadir monedas y actualizar el texto en pantalla
    public void AddScore()
    {
        score++;
        scoreText.text = $"{score}";
    }
}
