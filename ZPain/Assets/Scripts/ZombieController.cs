using UnityEngine;
using UnityEngine.AI; // IMPORTANTE: Necesario para usar NavMeshAgent
using System.Collections;

// Esto asegura que si pones el script, Unity te añade el NavMeshAgent automáticamente
[RequireComponent(typeof(NavMeshAgent))]
public class ZombieController : MonoBehaviour
{
    [Header("Sonidos de Ambiente")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip[] ambientSounds;
    [SerializeField] private float minTimeBetweenSounds = 4.0f;
    [SerializeField] private float maxTimeBetweenSounds = 8.0f;

    [Header("Sonidos de Ataque")]
    [SerializeField] private AudioClip[] attackSounds;

    [Header("Datos del zombi")]
    [SerializeField] private ZombieData zombieData;

    [Header("Ajustes de Combate")]
    [Tooltip("La velocidad a la que se moverá el zombi si le disparan en la pierna.")]
    [SerializeField] private float crippledSpeed = 1.5f;

    // --- COMPONENTES ---
    private NavMeshAgent agent; // Sustituye al CharacterController
    private Transform player;
    private Animator animator;
    private CapsuleCollider physicalCollider; // Para recibir balas

    // --- ESTADO ---
    private float currentHp;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isCrippled = false;

    // --- MANAGERS ---
    private WaveManager waveManager;
    private ScoreManager scoreManager;

    void Start()
    {
        // 1. Inicializar Componentes
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        physicalCollider = GetComponent<CapsuleCollider>();

        // 2. Buscar al Jugador
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogError("¡No se encuentra al Player! Asegúrate de que tiene el Tag 'Player'.");
        }

        // 3. Buscar Managers
        waveManager = FindAnyObjectByType<WaveManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();

        // 4. Configurar el Agente con los datos del ScriptableObject
        if (zombieData != null)
        {
            agent.speed = zombieData.speed;
            currentHp = zombieData.maxHp;

            // Configuración clave para que pare justo delante del jugador
            agent.stoppingDistance = zombieData.attackRange - 0.2f;
        }

        StartCoroutine(AmbientSoundRoutine());
    }

    // Método llamado por el Spawner para configurar stats
    public void ApplyZombieData(ZombieData data)
    {
        zombieData = data;
        currentHp = data.maxHp;
        if (agent != null)
        {
            agent.speed = data.speed;
            agent.stoppingDistance = data.attackRange - 0.2f;
        }
    }

    public void ApplyExtraHealth(float extraHealth)
    {
        currentHp += extraHealth;
    }

    void Update()
    {
        // Si está muerto, atacando o no hay jugador, no calculamos movimiento
        if (isDead || player == null) return;

        // Si está atacando, aseguramos que el agente esté quieto y salimos
        if (isAttacking)
        {
            if (!agent.isStopped) agent.isStopped = true;
            return;
        }

        // Calculamos distancia real física
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= zombieData.attackRange)
        {
            StopAndAttack();
        }
        else
        {
            ChasePlayer();
        }
    }

    private void ChasePlayer()
    {
        // Reactivamos el agente si estaba parado
        if (agent.isStopped) agent.isStopped = false;

        // --- LA LÍNEA MÁGICA: El NavMesh calcula todo ---
        agent.SetDestination(player.position);

        // Sincronizar animación con la velocidad real del agente
        // velocity.magnitude nos dice si realmente se está moviendo (por si se atasca)
        bool isMoving = agent.velocity.magnitude > 0.1f;
        animator.SetBool("isWalking", isMoving);
    }

    private void StopAndAttack()
    {
        // 1. Frenar en seco
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetBool("isWalking", false);

        // 2. Rotación Manual: 
        // El NavMesh no rota bien cuando está parado (isStopped), así que lo rotamos nosotros
        // para que mire al jugador mientras le pega.
        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0; // Ignorar altura para no inclinar al zombie
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        // 3. Comprobar Cooldown y Atacar
        if (Time.time - lastAttackTime >= zombieData.attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true; // Bloquea el movimiento en Update
        lastAttackTime = Time.time;

        animator.SetTrigger("Attack");

        // Sonido de ataque
        if (audioSource != null && attackSounds != null && attackSounds.Length > 0)
        {
            audioSource.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)]);
        }

        // Esperar al momento del impacto (ajusta este 0.9f a tu animación exacta)
        yield return new WaitForSeconds(0.9f);

        // Verificar si el jugador sigue cerca y vivo
        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            // Damos un pequeño margen extra (0.5f) por si el jugador se movió un poco hacia atrás
            if (distance <= zombieData.attackRange + 0.5f)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(zombieData.damage);
            }
        }

        // Esperar a que termine la animación
        // Asumiendo que la animación dura unos 2.3 segundos en total
        float animationDuration = 2.3f;
        yield return new WaitForSeconds(animationDuration - 0.9f);

        isAttacking = false; // Desbloquea el Update para que vuelva a perseguir
    }

    public void TakeDamage(float amount, EHitboxType partHit)
    {
        if (isDead) return;

        float finalDamage = amount;

        switch (partHit)
        {
            case EHitboxType.Head:
                finalDamage *= 2.0f; // Doble daño
                break;

            case EHitboxType.Legs:
                // Lógica de lisiado usando NavMeshAgent
                if (!isCrippled)
                {
                    isCrippled = true;
                    agent.speed = crippledSpeed; // <-- Modificamos la velocidad del NavMesh
                }
                break;
        }

        currentHp -= finalDamage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    // Sobrecarga por si se llama sin hitbox específica
    public void TakeDamage(float amount)
    {
        TakeDamage(amount, EHitboxType.Body);
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        StopAllCoroutines();

        Collider[] allColliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in allColliders)
        {
            col.enabled = false;
        }

        if (physicalCollider != null) physicalCollider.enabled = false;
        if (agent != null) agent.enabled = false;

        if (scoreManager != null) scoreManager.ZombieKilled();
        if (waveManager != null) waveManager.ZombieDied();

        
        PlayerShooting ps = null;
        if (player != null)
        {
            ps = player.GetComponent<PlayerShooting>();
        }

        if (ps == null)
        {
            ps = FindAnyObjectByType<PlayerShooting>();
        }

        if (ps != null)
        {
            ps.RegisterZombieKill();
        }
        else
        {
            Debug.LogError("[ERROR CRÍTICO] El Zombi murió pero NO ENCONTRÓ el script 'PlayerShooting' en la escena.");
        }

        Destroy(gameObject, 2.8f);
    }

    private IEnumerator AmbientSoundRoutine()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(Random.Range(minTimeBetweenSounds, maxTimeBetweenSounds));

            if (audioSource != null && ambientSounds != null && ambientSounds.Length > 0)
            {
                audioSource.PlayOneShot(ambientSounds[Random.Range(0, ambientSounds.Length)]);
            }
        }
    }

    public float GetHP()
    {
        return currentHp;
    }
}