using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PowerUpUIAnimator : MonoBehaviour
{
    [Header("Referencias de UI")]
    [Tooltip("El prefab del icono grande que aparecerá en el centro")]
    [SerializeField] private GameObject floatingIconPrefab;

    [Header("Configuración de Animación")]
    [Tooltip("Duración de la aparición/desaparición del icono central")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Tiempo que el icono central permanece visible ANTES de empezar a desaparecer")]
    [SerializeField] private float displayDuration = 1.0f;

    public void AnimatePowerUpIcon(PowerUpData powerUpData)
    {
        if (powerUpData == null || powerUpData.icon == null) return;
        
        StartCoroutine(AnimateIconSequence(powerUpData));
    }

    private IEnumerator AnimateIconSequence(PowerUpData powerUpData)
    {
        GameObject floatingIconGO = Instantiate(floatingIconPrefab, transform); 
        Image floatingImage = floatingIconGO.GetComponent<Image>();
        
        if (floatingImage == null)
        {
            Destroy(floatingIconGO);
            yield break;
        }

        floatingImage.sprite = powerUpData.icon;
        floatingImage.color = new Color(1f, 1f, 1f, 0f);

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / fadeDuration);
            floatingImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        floatingImage.color = new Color(1f, 1f, 1f, 1f);

        yield return new WaitForSeconds(displayDuration);

        timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            floatingImage.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
        
        Destroy(floatingIconGO);
    }
}