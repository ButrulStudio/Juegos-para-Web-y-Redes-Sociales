using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq; 

[RequireComponent(typeof(AudioSource))]
public class MysteryBox : MonoBehaviour
{
    [Header("Configuración de la Ruleta")]
    [Tooltip("Lista de todas las armas que pueden salir en la caja")]
    public List<WeaponData> possibleWeapons;
    public int pointsCost = 950;

    [Header("Referencias Visuales")]
    public Transform spinningPart;    
    public Transform weaponSpawnPoint; 

    [Header("UI e Interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public TextMeshProUGUI interactionText;

    [Header("Animación")]
    public float spinDuration = 4f;
    public float rotationSpeed = 800f;

    [Header("Sonidos")]
    public AudioSource audioSource;
    public AudioClip spinSound;
    public AudioClip weaponReadySound;

    private enum BoxState
    {
        Idle,
        Spinning,
        WeaponReady,
        Resetting
    }
    private BoxState currentState = BoxState.Idle;

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    private WeaponData selectedWeapon;
    private GameObject visualWeaponModel;

    void Start()
    {
        playerCamera = Camera.main;
        playerShooting = FindObjectOfType<PlayerShooting>();

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        CheckForInteraction();
    }

    void CheckForInteraction()
    {

        if (currentState == BoxState.Spinning || currentState == BoxState.Resetting)
        {
            if (playerLooking && interactionText != null)
            {
                interactionText.gameObject.SetActive(false);
                playerLooking = false;
            }
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance))
        {

            if (hit.collider.gameObject == gameObject || hit.collider.transform.parent == transform)
            {
                playerLooking = true;
                UpdateInteractionMessage();

                if (Input.GetKeyDown(interactionKey))
                {
                    HandleInput();
                }
                return;
            }
        }

        if (playerLooking)
        {
            playerLooking = false;
            if (interactionText != null)
            {
                interactionText.gameObject.SetActive(false);
            }
        }
    }

    void UpdateInteractionMessage()
    {
        if (interactionText == null) return;

        interactionText.gameObject.SetActive(true);

        switch (currentState)
        {
            case BoxState.Idle:
                interactionText.text = $"Pulsa [{interactionKey}] para probar suerte por {pointsCost} puntos";
                break;

            case BoxState.WeaponReady:
                if (selectedWeapon != null)
                {
                    interactionText.text = $"Pulsa [{interactionKey}] para coger {selectedWeapon.weaponName}";
                }
                break;
        }
    }

    void HandleInput()
    {
        if (currentState == BoxState.Idle)
        {
            TryStartRoulette();
        }
        else if (currentState == BoxState.WeaponReady)
        {
            EquipReward();
        }
    }

    void TryStartRoulette()
    {
        if (ScoreManager.Instance.TrySpendPoints(pointsCost))
        {
            StartCoroutine(SpinRoutine());
        }
        else
        {
            Debug.Log("No tienes suficientes puntos.");
        }
    }

    IEnumerator SpinRoutine()
    {
        currentState = BoxState.Spinning;

        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }

        if (audioSource != null && spinSound != null)
        {
            audioSource.clip = spinSound;
            audioSource.loop = true;
            audioSource.Play();
        }

        List<WeaponData> availableWeapons = new List<WeaponData>();

        if (playerShooting != null)
        {
            foreach (var weapon in possibleWeapons)
            {

                if (!playerShooting.HasWeapon(weapon.weaponType))
                {
                    availableWeapons.Add(weapon);
                }
            }
        }
        else
        {

            availableWeapons = new List<WeaponData>(possibleWeapons);
        }

        if (availableWeapons.Count == 0)
        {
            availableWeapons = new List<WeaponData>(possibleWeapons);
        }

        if (availableWeapons.Count > 0)
        {
            int randomIndex = Random.Range(0, availableWeapons.Count);
            selectedWeapon = availableWeapons[randomIndex];
        }

        float timer = 0f;
        float currentSpeed = rotationSpeed;

        while (timer < spinDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / spinDuration;

            currentSpeed = Mathf.Lerp(rotationSpeed, 0f, progress);

            if (spinningPart != null)
            {
                spinningPart.Rotate(Vector3.forward * currentSpeed * Time.deltaTime);
            }

            yield return null;
        }

        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;

            if (weaponReadySound != null)
            {
                audioSource.PlayOneShot(weaponReadySound);
            }
        }

        ShowFloatingWeapon();
        currentState = BoxState.WeaponReady;

        StartCoroutine(TimeoutRoutine());
    }

    void ShowFloatingWeapon()
    {
        if (selectedWeapon != null && selectedWeapon.weaponModelPrefab != null)
        {
            visualWeaponModel = Instantiate(selectedWeapon.weaponModelPrefab, weaponSpawnPoint.position, weaponSpawnPoint.rotation);
            visualWeaponModel.transform.SetParent(weaponSpawnPoint);

            var colliders = visualWeaponModel.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = false;
            }
        }
    }

    IEnumerator TimeoutRoutine()
    {
        float timer = 10f;
        while (timer > 0 && currentState == BoxState.WeaponReady)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        if (currentState == BoxState.WeaponReady)
        {
            CloseBox();
        }
    }

    void EquipReward()
    {
        if (playerShooting != null && selectedWeapon != null)
        {
            playerShooting.EquipWeapon(selectedWeapon);
            playerShooting.ForceCurrentWeaponAmmoToFull();

            WeaponStore.RegisterStartingWeapon(selectedWeapon);

            Debug.Log($"¡Has obtenido {selectedWeapon.weaponName} de la caja!");
        }

        CloseBox();
    }

    void CloseBox()
    {
        currentState = BoxState.Resetting;

        if (visualWeaponModel != null)
        {
            Destroy(visualWeaponModel);
        }

        selectedWeapon = null;

        Invoke("ResetToIdle", 2f);
    }

    void ResetToIdle()
    {
        currentState = BoxState.Idle;
    }
}