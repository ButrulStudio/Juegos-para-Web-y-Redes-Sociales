using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Configuración Visual")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float destroyTime = 1f;

    private TextMeshPro textMesh;
    private Color textColor;
    private Camera mainCam;

    // Vector de movimiento que calcularemos aleatoriamente
    private Vector3 randomDirection;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null) textColor = textMesh.color;
        mainCam = Camera.main;
    }

    void Start()
    {
        // CALCULAMOS LA ALEATORIEDAD AQUÍ
        // X: Entre -0.8 y 0.8 (Izquierda/Derecha aleatoria)
        // Y: 1 (Siempre hacia arriba)
        // Z: 0 (No queremos que se acerque o aleje en profundidad)
        float randomX = Random.Range(-0.8f, 0.8f);
        randomDirection = new Vector3(randomX, 1f, 0f).normalized; // Normalizamos para que la velocidad sea constante

        // Orientación inicial
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;

        // Destrucción automática
        Destroy(gameObject, destroyTime);
    }

    public void Setup(int amount)
    {
        if (textMesh != null) textMesh.text = "+" + amount.ToString();
    }

    void Update()
    {
        // 1. Mover en la dirección aleatoria calculada
        transform.position += randomDirection * moveSpeed * Time.deltaTime;

        // 2. Desvanecer
        if (textMesh != null)
        {
            float alphaChange = Time.deltaTime / destroyTime;
            textColor.a -= alphaChange;
            textMesh.color = textColor;
        }

        // 3. Orientación (Billboard)
        if (mainCam != null) transform.rotation = mainCam.transform.rotation;
    }
}