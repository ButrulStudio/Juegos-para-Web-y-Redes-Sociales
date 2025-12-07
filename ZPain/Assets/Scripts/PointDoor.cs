using UnityEngine;
using System.Collections;
using TMPro;

public class PointDoor : MonoBehaviour
{
    [Header("Configuración de la Puerta")]
    [Tooltip("Puntos necesarios para abrir la puerta.")]
    [SerializeField] private int cost = 500;
    [Tooltip("Duración de la animación de desaparición.")]
    [SerializeField] private float disappearDuration = 1.0f;
    [Tooltip("Distancia vertical que sube la puerta antes de destruirse.")]
    [SerializeField] private float liftDistance = 5.0f;
    [Tooltip("Curva de animación para la subida (opcional).")]
    [SerializeField] private AnimationCurve liftCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Interacción")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI y Referencias")]
    [Tooltip("El TextMeshProUGUI que muestra el precio/mensaje de interacción.")]
    [SerializeField] private TextMeshProUGUI hudText;
    private Renderer doorRenderer;
    private Camera playerCamera;
    private bool isPlayerLooking = false;

    void Start()
    {
        // 1. Obtener referencias
        playerCamera = Camera.main;
        doorRenderer = GetComponent<Renderer>();

        // 2. Inicializar UI
        if (hudText != null)
        {
            // La puerta debe asignar su propio HUDText al objeto UI global
            hudText.gameObject.SetActive(false);
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogError("PointDoor: ScoreManager no encontrado en la escena. ¡Necesario para el pago!");
        }
    }

    void Update()
    {
        CheckForInteraction();
    }

    private void CheckForInteraction()
    {
        if (playerCamera == null || ScoreManager.Instance == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        // 1. Lanzamos el Raycast
        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            // 2. Comprobamos si el rayo golpea ESTE objeto
            if (hit.collider.gameObject == gameObject)
            {
                if (!isPlayerLooking)
                {
                    isPlayerLooking = true;
                }

                ShowInteractionMessage();

                // 3. Intentar interactuar
                if (Input.GetKeyDown(interactionKey))
                {
                    TryOpenDoor();
                }
                return;
            }
        }

        // Si el rayo no golpea la puerta, ocultar el mensaje
        if (isPlayerLooking)
        {
            isPlayerLooking = false;
            HideInteractionMessage();
        }
    }

    private void ShowInteractionMessage()
    {
        if (hudText == null) return;
        hudText.gameObject.SetActive(true);

        int currentPoints = ScoreManager.Instance.GetCurrentScore();

        if (currentPoints >= cost)
        {
            // Suficientes puntos
            hudText.text = $"Pulsa [{interactionKey}] para abrir: <color=yellow>{cost} pts</color>";
        }
        else
        {
            // Puntos insuficientes
            hudText.text = $"Pulsa [{interactionKey}] para abrir: <color=red>{cost} pts</color>";
        }
    }

    private void HideInteractionMessage()
    {
        if (hudText != null)
        {
            hudText.gameObject.SetActive(false);
        }
    }

    private void TryOpenDoor()
    {
        if (ScoreManager.Instance == null) return;

        // Intentamos gastar los puntos usando el método existente de ScoreManager
        if (ScoreManager.Instance.TrySpendPoints(cost))
        {
            // Pago exitoso, iniciar animación
            StartCoroutine(DisappearAnimation());
        }
        else
        {
            // Pago fallido
            Debug.Log($"No tienes suficientes puntos para abrir la puerta ({cost} pts).");
        }
    }

    private IEnumerator DisappearAnimation()
    {
        // Desactivar el collider para que el jugador pueda pasar inmediatamente
        Collider doorCollider = GetComponent<Collider>();
        if (doorCollider != null) doorCollider.enabled = false;

        // Ocultar el mensaje de UI
        HideInteractionMessage();

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * liftDistance; // Mover hacia arriba

        float timer = 0f;

        while (timer < disappearDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / disappearDuration;
            float curveValue = liftCurve.Evaluate(progress); // Usar la curva de animación

            // Interpolación de posición (mover hacia arriba)
            transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);

            // Interpolación de transparencia del material (opcional, para que se desvanezca)
            // (Si usas un shader transparente, puedes añadir aquí el desvanecimiento)

            yield return null;
        }

        // Asegurar que la puerta subió completamente
        transform.position = endPosition;

        // Finalmente, destruir el objeto
        Destroy(gameObject);
    }
}