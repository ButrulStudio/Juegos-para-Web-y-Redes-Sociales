using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float sensibility = 100f;
    [SerializeField] private float verticalRotation = 0f;

    public Transform jugador;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibility * Time.deltaTime; // Input de rotacion horizontal
        float mouseY = Input.GetAxis("Mouse Y") * sensibility * Time.deltaTime; // Input de rotacion vertical

        // Rotaci�n vertical (mirar arriba/abajo), invertida normalmente para FPS
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        // Aplica rotaci�n vertical a la c�mara
        transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // Aplica rotaci�n horizontal al jugador (rota el cuerpo)
        jugador.Rotate(Vector3.up * mouseX);
        


    }
}
