using UnityEngine;
using System.Collections;

public class ZombieController : MonoBehaviour
{
    [Header("Sonidos de Ambiente")]
    [Tooltip("El AudioSource para los gruñidos del zombi.")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Gruñidos aleatorios que el zombi emite.")]
    [SerializeField] private AudioClip[] ambientSounds;
    [Tooltip("Tiempo mínimo (en segundos) entre gruñidos.")]
    [SerializeField] private float minTimeBetweenSounds = 4.0f;
    [Tooltip("Tiempo máximo (en segundos) entre gruñidos.")]
    [SerializeField] private float maxTimeBetweenSounds = 8.0f;

    [Tooltip("Gruñidos aleatorios que el zombi emite al atacar.")]
    [SerializeField] private AudioClip[] attackSounds;

    [Header("Datos del zombi")]
    [SerializeField] private ZombieData zombieData;


    [Header("Ajustes de Combate")]
    [Tooltip("La velocidad a la que se moverá el zombi si le disparan en la pierna.")]
    [SerializeField] private float crippledSpeed = 1.5f;

    private CharacterController zombie;
    private Transform player;
    private Animator animator;
    private float currentHp;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isAttacking = false;
    private Vector3 verticalVelocity; // Para manejar la gravedad

    private WaveManager waveManager;
    private ScoreManager scoreManager;

    [Header("Configuración de Evasión")]
    [Tooltip("Máscara de la capa que bloquea la entrada al metro.")]
    public LayerMask metroEntranceMask;

    private float currentSpeed; // Velocidad actual (puede ser 'crippledSpeed')
    private bool isCrippled = false; // Flag para saber si le han disparado en la pierna

    void Start()
    {
        // --- Inicialización de componentes ---
        zombie = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // --- Búsqueda de Managers (Singletons) ---
        waveManager = FindAnyObjectByType<WaveManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();


        // Establece la velocidad inicial basada en el ScriptableObject
        if (zombieData != null)
        {
            currentSpeed = zombieData.speed;
        }

        // Inicia la corrutina para los gruñidos aleatorios
        StartCoroutine(AmbientSoundRoutine());
    }

    /// <summary>
    /// Aplica los datos base (vida, daño, etc.) del ScriptableObject.
    /// </summary>
    public void ApplyZombieData(ZombieData data)
    {
        zombieData = data;
        currentHp = data.maxHp;
    }

    /// <summary>
    /// Añade la vida extra calculada por el WaveManager.
    /// Es llamado por el ZombieSpawner justo después de ApplyZombieData.
    /// </summary>
    public void ApplyExtraHealth(float extraHealth)
    {
        currentHp += extraHealth;
    }


    void Update()
    {
        // Cláusula de guarda: si está muerto, atacando, o no hay jugador, no hace nada.
        if (isDead || player == null || isAttacking) return;

        // Comprueba la distancia al jugador para decidir si atacar o perseguir.
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= zombieData.attackRange)
        {
            StopAndAttack(); // Si está cerca, ataca
        }
        else
        {
            FollowPlayer(); // Si está lejos, persigue
        }
    }

    /// <summary>
    /// Lógica de movimiento (pathfinding) del zombi.
    /// </summary>
    private void FollowPlayer()
    {
        // Activa la animación de andar
        animator.SetBool("isWalking", true);

        // --- Manejo de Gravedad ---
        if (zombie.isGrounded)
            verticalVelocity.y = -2f; // Fuerza un poco hacia abajo si está en el suelo
        else
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime; // Aplica gravedad

        // --- Cálculo de Dirección ---
        // Dirección ideal: directo hacia el jugador (ignorando eje Y)
        Vector3 targetDir = (player.position - transform.position);
        targetDir.y = 0;
        targetDir.Normalize();

        // Lógica de evasión de obstáculos (Raycast avoidance)
        float rayDistance = 2.0f;
        int rayCount = 15; // Nº de rayos en un arco
        float maxAngle = 90f; // Amplitud del arco

        Vector3 bestDirection = Vector3.zero; // La dirección "segura" que elegirá
        float bestScore = float.MinValue;
        Vector3 origin = transform.position + Vector3.up * 0.5f; // Origen de los rayos

        // Lanza múltiples rayos en un arco frontal
        for (int i = 0; i < rayCount; i++)
        {
            float t = (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-maxAngle, maxAngle, t);
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;

            // Comprueba si el rayo choca con algo
            bool blocked = Physics.Raycast(origin, dir, rayDistance, ~0, QueryTriggerInteraction.Ignore);

            // Puntuación: qué tan alineada está esta dirección con el jugador
            float alignmentScore = Vector3.Dot(dir, targetDir);
            // Penalización si la dirección está bloqueada
            float avoidanceScore = blocked ? -8.0f : 1.0f;

            // Penalización extra si choca con la capa "metroEntranceMask"
            float metroPenalty = 0f;
            if (Physics.Raycast(origin, dir, out hit, rayDistance, metroEntranceMask))
            {
                metroPenalty = -15.0f;
            }

            // Puntuación total de esta dirección
            float score = (alignmentScore * 1.0f) + (avoidanceScore * 3.0f) + metroPenalty;

            // Si esta dirección es mejor que la anterior, la guardamos
            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        // Si no se encontró una buena dirección, va directo al jugador
        if (bestDirection == Vector3.zero || bestScore < -5.0f)
            bestDirection = targetDir;

        // Lógica de evasión de otros zombis
        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (var col in nearby)
        {
            if (col.CompareTag("Zombie") && col.gameObject != this.gameObject)
            {
                Vector3 away = transform.position - col.transform.position;
                away.y = 0;
                bestDirection += away.normalized * 1.0f; // Añade una fuerza de repulsión
            }
        }
        bestDirection.Normalize(); // Normaliza la dirección final

        // --- Aplicación del Movimiento ---
        Vector3 horizontalMovement = bestDirection * currentSpeed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity; // Combina movimiento y gravedad
        zombie.Move(finalMovement * Time.deltaTime);

        // Rotación suave hacia la dirección de movimiento
        if (horizontalMovement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    /// <summary>
    /// Detiene el movimiento y gestiona la lógica de ataque.
    /// </summary>
    private void StopAndAttack()
    {
        animator.SetBool("isWalking", false); // Detiene animación de andar

        // Sigue aplicando gravedad aunque esté quieto
        if (!zombie.isGrounded)
        {
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            zombie.Move(verticalVelocity * Time.deltaTime);
        }

        // Mira al jugador (solo en eje Y)
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // Comprueba si ha pasado el cooldown de ataque
        if (Time.time - lastAttackTime >= zombieData.attackCooldown)
        {
            StartCoroutine(AttackRoutine()); // Inicia la corrutina de ataque
        }
    }

    /// <summary>
    /// Corrutina que maneja la animación de ataque, el sonido y la aplicación del daño.
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        isAttacking = true; // Bloquea el Update()
        lastAttackTime = Time.time; // Resetea el cooldown

        animator.SetTrigger("Attack"); // Dispara la animación de ataque

        // --- Reproducir sonido de ataque aleatorio ---
        if (audioSource != null && attackSounds != null && attackSounds.Length > 0)
        {

            int index = Random.Range(0, attackSounds.Length);
            AudioClip clip = attackSounds[index];


            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        // Espera 0.9 segundos (punto de la animación donde impacta)
        yield return new WaitForSeconds(0.9f);

        // --- Aplicar Daño ---
        // Comprueba si el jugador sigue en rango después de 0.9s
        if (Vector3.Distance(transform.position, player.position) <= zombieData.attackRange)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(zombieData.damage);
        }

        // Espera el resto de la animación
        float animationTime = 2.3f;
        float waitAfterHitPoint = animationTime - 0.9f;
        yield return new WaitForSeconds(waitAfterHitPoint);

        // Espera el tiempo de cooldown restante (si lo hay)
        float remainingCooldown = zombieData.attackCooldown - animationTime;
        if (remainingCooldown > 0)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        isAttacking = false; // Desbloquea el Update()
    }

    /// <summary>
    /// Función pública para recibir daño (sobrecarga para daño genérico al cuerpo).
    /// </summary>
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, EHitboxType.Body);
    }


    /// <summary>
    /// Función principal de recibir daño, llamada por los ZombieHitbox.
    /// </summary>
    public void TakeDamage(float amount, EHitboxType partHit)
    {
        if (isDead) return; // No puede recibir daño si ya está muerto


        float finalDamage = amount;

        // Switch para aplicar multiplicadores o efectos según la parte golpeada
        switch (partHit)
        {

            case EHitboxType.Head: // Doble daño en la cabeza
                finalDamage *= 2.0f;
                Debug.Log($"¡Disparo a la cabeza! Daño total: {finalDamage}");
                break;


            case EHitboxType.Legs: // Ralentización en las piernas
                finalDamage = amount; // Daño normal

                if (!isCrippled) // Aplica la ralentización solo una vez
                {
                    isCrippled = true;

                    currentSpeed = crippledSpeed; // Reduce la velocidad
                    Debug.Log($"¡Pierna herida! Zombie ralentizado a {crippledSpeed}");
                }
                break;

            case EHitboxType.Body:
            default: // Daño normal
                finalDamage = amount;
                break;
        }

        currentHp -= finalDamage; // Aplica el daño

        if (currentHp <= 0)
        {
            Die(); // Muere si la vida es 0 o menos
        }
    }

    /// <summary>
    /// Gestiona la muerte del zombi.
    /// </summary>
    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die"); // Dispara la animación de muerte

        StopAllCoroutines(); // Detiene la corrutina de sonidos ambientales

        // Desactiva los colliders y el CharacterController para que no bloquee
        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.enabled = false;
        }
        zombie.enabled = false;

        // Notifica a los managers que el zombi ha muerto (para puntuación y conteo de oleada)
        if (scoreManager != null) scoreManager.ZombieKilled();
        if (waveManager != null) waveManager.ZombieDied();

        // Destruye el GameObject después de 2 segundos (para que termine la animación)
        Destroy(gameObject, 2f);
    }

    /// <summary>
    /// Getter público para la vida actual (usado por otros scripts si es necesario).
    /// </summary>
    public float GetHP()
    {
        return currentHp;
    }

    /// <summary>
    /// Corrutina que reproduce gruñidos aleatorios en intervalos aleatorios.
    /// </summary>
    private IEnumerator AmbientSoundRoutine()
    {

        while (!isDead) // Bucle infinito mientras el zombi esté vivo
        {

            // 1. Espera un tiempo aleatorio
            float waitTime = Random.Range(minTimeBetweenSounds, maxTimeBetweenSounds);
            yield return new WaitForSeconds(waitTime);


            // 2. Comprueba si tiene los componentes necesarios
            if (audioSource != null && ambientSounds != null && ambientSounds.Length > 0)
            {

                // 3. Elige un clip aleatorio
                int index = Random.Range(0, ambientSounds.Length);
                AudioClip clip = ambientSounds[index];

                // 4. Lo reproduce
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }
    }
}