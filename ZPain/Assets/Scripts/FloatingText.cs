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

    private Vector3 randomDirection;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh != null) textColor = textMesh.color;
        mainCam = Camera.main;
    }

    void Start()
    {
        float randomX = Random.Range(-0.8f, 0.8f);
        randomDirection = new Vector3(randomX, 1f, 0f).normalized; 

        if (mainCam != null) transform.rotation = mainCam.transform.rotation;

        Destroy(gameObject, destroyTime);
    }

    public void Setup(int amount)
    {
        if (textMesh != null) textMesh.text = "+" + amount.ToString();
    }

    void Update()
    {

        transform.position += randomDirection * moveSpeed * Time.deltaTime;

        if (textMesh != null)
        {
            float alphaChange = Time.deltaTime / destroyTime;
            textColor.a -= alphaChange;
            textMesh.color = textColor;
        }

        if (mainCam != null) transform.rotation = mainCam.transform.rotation;
    }
}