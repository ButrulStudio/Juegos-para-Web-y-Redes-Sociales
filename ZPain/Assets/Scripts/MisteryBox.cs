using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class MysteryBox : MonoBehaviour
{
    [Header("Configuración de la Ruleta")]
    public List<WeaponData> possibleWeapons; // Arrastra aquí todos tus ScriptableObjects de armas
    public int pointsCost = 950;

    [Header("Referencias Visuales")]
    public Transform spinningPart;   // El bloque que da vueltas (la tapa o el mecanismo)
    public Transform weaponSpawnPoint; // Dónde aparecerá el arma flotando

    [Header("UI e Interacción")]
    public float interactionDistance = 3f;
    public KeyCode interactionKey = KeyCode.E;
    public TextMeshProUGUI interactionText; // Referencia al mismo texto de UI que usa la tienda

    [Header("Animación")]
    public float spinDuration = 4f; // Duración total del giro
    public float rotationSpeed = 800f; // Velocidad inicial de giro

    // --- ESTADOS INTERNOS ---
    private enum BoxState { Idle, Spinning, WeaponReady, Resetting }
    private BoxState currentState = BoxState.Idle;

    private Camera playerCamera;
    private PlayerShooting playerShooting;
    private bool playerLooking = false;

    private WeaponData selectedWeapon;     // El arma que ganó la ruleta
    private GameObject visualWeaponModel;  // El modelo 3D temporal que mostramos

    void Start()
    {
        playerCamera = Camera.main;
        playerShooting = FindObjectOfType<PlayerShooting>();

        // Ocultar texto al inicio si es necesario
        if (interactionText != null) interactionText.gameObject.SetActive(false);
    }

    void Update()
    {
        CheckForInteraction();
    }

    void CheckForInteraction()
    {
        // 1. Si estamos girando o reseteando, no permitir interacción ni mostrar texto
        if (currentState == BoxState.Spinning || currentState == BoxState.Resetting)
        {
            if (playerLooking && interactionText != null)
            {
                interactionText.gameObject.SetActive(false);
                playerLooking = false;
            }
            return;
        }

        // 2. Raycast para detectar si miramos la caja
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

        // 3. Si dejamos de mirar
        if (playerLooking)
        {
            playerLooking = false;
            if (interactionText != null) interactionText.gameObject.SetActive(false);
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
                    interactionText.text = $"Pulsa [{interactionKey}] para coger {selectedWeapon.weaponName}";
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
        // Verificar puntos con tu ScoreManager
        if (ScoreManager.Instance.TrySpendPoints(pointsCost))
        {
            StartCoroutine(SpinRoutine());
        }
        else
        {
            Debug.Log("No tienes suficientes puntos.");
            // Opcional: Mostrar mensaje temporal de "Faltan puntos"
        }
    }

    // --- LÓGICA DE LA ANIMACIÓN Y SORTEO ---
    IEnumerator SpinRoutine()
    {
        currentState = BoxState.Spinning;
        if (interactionText != null) interactionText.gameObject.SetActive(false);

        // 1. Elegir arma aleatoria
        if (possibleWeapons.Count > 0)
        {
            int randomIndex = Random.Range(0, possibleWeapons.Count);
            selectedWeapon = possibleWeapons[randomIndex];
        }

        // 2. Animación de giro del bloque
        float timer = 0f;
        float currentSpeed = rotationSpeed;

        // Calculamos vueltas aleatorias extras para que no siempre pare igual
        float randomExtraRot = Random.Range(0f, 360f);

        while (timer < spinDuration)
        {
            timer += Time.deltaTime;

            // Hacemos que la velocidad decaiga suavemente (Lerp)
            float progress = timer / spinDuration;
            currentSpeed = Mathf.Lerp(rotationSpeed, 0f, progress);

            if (spinningPart != null)
            {
                // Giramos sobre el eje Y (o el que necesites, cámbialo a Vector3.right o forward si gira mal)
                spinningPart.Rotate(Vector3.forward * currentSpeed * Time.deltaTime);
            }

            yield return null;
        }

        // 3. Mostrar el arma ganadora flotando
        ShowFloatingWeapon();

        currentState = BoxState.WeaponReady;

        // 4. Iniciar temporizador: si no la coges en 10 segundos, desaparece
        StartCoroutine(TimeoutRoutine());
    }

    void ShowFloatingWeapon()
    {
        if (selectedWeapon != null && selectedWeapon.weaponModelPrefab != null)
        {
            // Instanciar solo visualmente
            visualWeaponModel = Instantiate(selectedWeapon.weaponModelPrefab, weaponSpawnPoint.position, weaponSpawnPoint.rotation);
            visualWeaponModel.transform.SetParent(weaponSpawnPoint);

            // Desactivar scripts o coliders del modelo visual para que no molesten
            var colliders = visualWeaponModel.GetComponentsInChildren<Collider>();
            foreach (var col in colliders) col.enabled = false;
        }
    }

    IEnumerator TimeoutRoutine()
    {
        // Espera 10 segundos mientras está en estado WeaponReady
        float timer = 10f;
        while (timer > 0 && currentState == BoxState.WeaponReady)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        // Si se acabó el tiempo y nadie la cogió
        if (currentState == BoxState.WeaponReady)
        {
            CloseBox();
        }
    }

    void EquipReward()
    {
        if (playerShooting != null && selectedWeapon != null)
        {
            // Usamos la lógica existente en PlayerShooting
            playerShooting.EquipWeapon(selectedWeapon);
            playerShooting.ForceCurrentWeaponAmmoToFull();

            // IMPORTANTE: Registrarla en la tienda para que sepa que ya la tenemos (opcional, según tu diseño)
            WeaponStore.RegisterStartingWeapon(selectedWeapon);

            Debug.Log($"¡Has obtenido {selectedWeapon.weaponName} de la caja!");
        }

        CloseBox();
    }

    void CloseBox()
    {
        currentState = BoxState.Resetting;

        // Destruir el modelo visual
        if (visualWeaponModel != null) Destroy(visualWeaponModel);
        selectedWeapon = null;

        // Breve pausa antes de poder volver a usarla
        Invoke("ResetToIdle", 2f);
    }

    void ResetToIdle()
    {
        currentState = BoxState.Idle;
    }
}