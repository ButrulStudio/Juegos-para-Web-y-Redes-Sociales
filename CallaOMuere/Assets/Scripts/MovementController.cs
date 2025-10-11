using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;


public class MovementController : MonoBehaviour
{
    [SerializeField] private CharacterController characterController;
    public Transform isGrounded;  // Empty para detectar colisión con el suelo

    [Header("Movimiento")]
    [SerializeField] private float velocity = 15f;
    [SerializeField] private float sprint = 1.5f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 2f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundDistance;
    [SerializeField] private LayerMask groundLayerMask;

    private Vector3 velocityVector;
    private bool isSprinting = false;
    private bool inGround;
    private bool jumped = false;
    private int jumps = 2; // Doble salto

    private float inputX;
    private float inputZ;

    void Update()
    {
        // Detectar si está en el suelo
        inGround = Physics.CheckSphere(isGrounded.position, groundDistance, groundLayerMask);

        // Input de movimiento
        inputX = Input.GetAxis("Horizontal");
        inputZ = Input.GetAxis("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Saltar
        if (Input.GetButtonDown("Jump") && jumps > 0)
        {
            jumped = true;
        }
    }

    void FixedUpdate()
    {
        Vector3 move = transform.right * inputX + transform.forward * inputZ;

        // Aplica salto
        if (jumped)
        {
            jumps--;
            velocityVector.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            jumped = false;
        }

        // Resetear salto si toca suelo
        if (inGround && velocityVector.y < 0)
        {
            velocityVector.y = -2f;
            jumps = 2;
        }

        // Gravedad
        velocityVector.y += gravity * Time.fixedDeltaTime;

        // Movimiento final
        Vector3 finalMovement = move * (isSprinting ? velocity * sprint : velocity);
        characterController.Move((finalMovement + velocityVector) * Time.fixedDeltaTime);
    }
}
