using UnityEngine;
using UnityEngine.EventSystems;

public class MobileButtonHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum ButtonType { Shoot, Interact }
    public ButtonType buttonType;

    private PlayerShooting playerShooting;

    void Start()
    {
        playerShooting = FindObjectOfType<PlayerShooting>();
    }

    // Al poner el dedo
    public void OnPointerDown(PointerEventData eventData)
    {
        if (playerShooting == null) return;

        if (buttonType == ButtonType.Shoot)
        {
            playerShooting.SetMobileFiring(true); // Activa el disparo
        }
        else if (buttonType == ButtonType.Interact)
        {
            playerShooting.MobilePressInteract(); // Simula pulsar 'E'
        }
    }

    // Al levantar el dedo
    public void OnPointerUp(PointerEventData eventData)
    {
        if (playerShooting == null) return;

        if (buttonType == ButtonType.Shoot)
        {
            playerShooting.SetMobileFiring(false); // Corta el disparo
        }
    }
}