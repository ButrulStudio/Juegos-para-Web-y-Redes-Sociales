using UnityEngine;
using TMPro;

public class PowerUps : MonoBehaviour
{
    // ENUM con los tipos de compra (Solo Armadura por ahora)
    public enum PurchaseType
    {
        MaxArmor
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemName = "Armadura";
        // Asegúrate de que el tipo por defecto sea MaxArmor en el Inspector
        public PurchaseType type = PurchaseType.MaxArmor;
        public int cost = 500;
    }

    [Header("Configuración de la Zona de Compra")]
    [SerializeField] private KeyCode interactionKey = KeyCode.F;
    [SerializeField] private ShopItem itemForSale;

    [Header("Referencias UI")]
    [SerializeField] private TextMeshProUGUI promptText;

    private PlayerHealth playerHealth;
    // Eliminamos la referencia a PlayerShooting

    private bool playerInZone = false;

    private void Start()
    {
        if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (playerInZone && Input.GetKeyDown(interactionKey))
        {
            AttemptPurchase();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerHealth = other.GetComponent<PlayerHealth>();

            // Solo verificamos PlayerHealth y ScoreManager
            if (playerHealth != null && ScoreManager.Instance != null)
            {
                playerInZone = true;
                if (promptText != null)
                {
                    promptText.text = $"Presiona F para comprar {itemForSale.itemName} ({itemForSale.cost} Puntos)";
                    promptText.gameObject.SetActive(true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInZone = false;
            playerHealth = null; // Limpiar referencia
            if (promptText != null) promptText.gameObject.SetActive(false);
        }
    }

    private void AttemptPurchase()
    {
        // 1. Verificación de Armadura Máxima (usa el público maxArmor)
        if (itemForSale.type == PurchaseType.MaxArmor && playerHealth.currentArmor == playerHealth.maxArmor)
        {
            ShowConfirmationText("🛡️ Armadura al máximo. ¡Compra innecesaria!", 2f);
            return;
        }

        // 2. Verificación de Puntos
        if (ScoreManager.Instance.GetCurrentScore() < itemForSale.cost)
        {
            ShowConfirmationText($"❌ Puntos insuficientes. Necesitas {itemForSale.cost} puntos.", 2f);
            return;
        }

        // 3. Ejecutar la compra
        HandlePurchase();
    }

    private void HandlePurchase()
    {
        if (ScoreManager.Instance.TrySpendPoints(itemForSale.cost))
        {
            // Solo tenemos un caso, pero mantenemos el switch para futura expansión
            switch (itemForSale.type)
            {
                case PurchaseType.MaxArmor:
                    playerHealth.BuyMaxArmor();
                    ShowConfirmationText($"✅ Armadura comprada por {itemForSale.cost} Puntos. Protección máxima.", 3f);
                    break;
            }
        }
    }

    // --- Lógica de UI ---
    private void ShowConfirmationText(string message, float duration)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
            CancelInvoke("ResetPromptText");
            Invoke("ResetPromptText", duration);
        }
    }

    private void ResetPromptText()
    {
        if (playerInZone && promptText != null)
        {
            promptText.text = $"Presiona F para comprar {itemForSale.itemName} ({itemForSale.cost} Puntos)";
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}