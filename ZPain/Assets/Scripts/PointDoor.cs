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

    [Header("Sincronización")] 
    [Tooltip("Otra puerta PointDoor que debe desaparecer simultáneamente (opcional).")]

    public PointDoor syncedDoor;

    [Header("Interacción")]
    [SerializeField] private float interactionDistance = 3f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;

    [Header("UI y Referencias")]
    [Tooltip("El TextMeshProUGUI que muestra el precio/mensaje de interacción.")]
    [SerializeField] private TextMeshProUGUI hudText;
    private Renderer doorRenderer;
    private Camera playerCamera;
    private bool isPlayerLooking = false;

    private bool isOpened = false;

    void Start()
    {

        playerCamera = Camera.main;
        doorRenderer = GetComponent<Renderer>();

        if (hudText != null)
        {
            hudText.gameObject.SetActive(false);
        }

        if (ScoreManager.Instance == null)
        {
            Debug.LogError("PointDoor: ScoreManager no encontrado en la escena. ¡Necesario para el pago!");
        }
    }

    void Update()
    {
        if (isOpened) return;

        CheckForInteraction();
    }

    private void CheckForInteraction()
    {
        if (playerCamera == null || ScoreManager.Instance == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {
            if (hit.collider.gameObject == gameObject)
            {
                if (!isPlayerLooking)
                {
                    isPlayerLooking = true;
                }

                ShowInteractionMessage();

                if (Input.GetKeyDown(interactionKey))
                {
                    TryOpenDoor();
                }
                return;
            }
        }

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
            hudText.text = $"Pulsa [{interactionKey}] para abrir: <color=yellow>{cost} pts</color>";
        }
        else
        {
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
        if (ScoreManager.Instance == null || isOpened) return;
        if (ScoreManager.Instance.TrySpendPoints(cost))
        {
            OpenDoorInternal();
            if (syncedDoor != null)
            {
                syncedDoor.OpenDoorInternal();
            }
        }
        else
        {
            Debug.Log($"No tienes suficientes puntos para abrir la puerta ({cost} pts).");
        }
    }
    public void OpenDoorInternal()
    {
        if (isOpened) return; 

        isOpened = true; 

        StartCoroutine(DisappearAnimation());
    }

    private IEnumerator DisappearAnimation()
    {

        Collider doorCollider = GetComponent<Collider>();
        if (doorCollider != null) doorCollider.enabled = false;

        HideInteractionMessage();

        Vector3 startPosition = transform.position;
        Vector3 endPosition = startPosition + Vector3.up * liftDistance; 

        float timer = 0f;

        while (timer < disappearDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / disappearDuration;
            float curveValue = liftCurve.Evaluate(progress); 

            transform.position = Vector3.Lerp(startPosition, endPosition, curveValue);

            yield return null;
        }

        transform.position = endPosition;

        Destroy(gameObject);
    }
}