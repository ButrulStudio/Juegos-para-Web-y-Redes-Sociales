using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float destroyTime = 1f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0.5f, 0);

    private TextMeshPro textMesh;
    private Color textColor;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null)
        {
            textColor = textMesh.color;
        }
    }

    public void Setup(int amount)
    {
        if (textMesh != null)
        {
            textMesh.text = "+" + amount.ToString();
        }

        // Destruir el objeto automáticamente después de X segundos
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        // 1. Mover hacia arriba suavemente
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. Efecto de desvanecimiento (Fade Out)
        if (textMesh != null)
        {
            float alphaChange = Time.deltaTime / destroyTime;
            textColor.a -= alphaChange;
            textMesh.color = textColor;
        }

        // 3. Hacer que el texto siempre mire a la cámara (Billboarding)
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}