using UnityEngine;

public class UnidirectionalExit : MonoBehaviour
{
    [Tooltip("El vector de dirección que los personajes deben seguir para salir del Trigger.")]
    public Vector3 exitDirection = Vector3.forward;

    [Tooltip("Fuerza constante aplicada en la dirección de salida (para forzar el escape).")]
    public float escapeForce = 8f; 

    [Header("Configuración de Tags")]
    public string playerTag = "Player";
    public string zombieTag = "Zombie";

    private void Start()
    {
        exitDirection.Normalize();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag(playerTag) || other.CompareTag(zombieTag))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();
            CharacterController cc = other.GetComponent<CharacterController>();

            if (rb == null && cc == null) return;
                        
            if (rb != null)
            {
                rb.AddForce(exitDirection * escapeForce, ForceMode.Acceleration);
            }
            else if (cc != null)
            {
                cc.Move(exitDirection * escapeForce * Time.deltaTime); 
            }
        }
    }
}