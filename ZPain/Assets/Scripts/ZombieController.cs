using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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
    private NavMeshAgent agent;
    private Transform player;
    private Animator animator;
    private CapsuleCollider physicalCollider;

    // --- ESTADO ---
    private float currentHp;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isAttacking = false;
    private bool isCrippled = false;

    // --- MANAGERS ---
    private WaveManager waveManager;
    private ScoreManager scoreManager;

    // --- VARIABLES PARA EFECTOS ---
    private float originalSpeed;
    private Coroutine slowCoroutine;

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

        // 4. Configurar el Agente
        if (zombieData != null)
        {
            agent.speed = zombieData.speed;
            currentHp = zombieData.maxHp;
            agent.stoppingDistance = zombieData.attackRange - 0.2f;
        }

        // Guardamos la velocidad original para poder restaurarla tras efectos de hielo
        if (agent != null) originalSpeed = agent.speed;

        StartCoroutine(AmbientSoundRoutine());
    }

    // --- MÉTODOS DE CONFIGURACIÓN ---
    public void ApplyZombieData(ZombieData data)
    {
        zombieData = data;
        currentHp = data.maxHp;
        if (agent != null)
        {
            agent.speed = data.speed;
            originalSpeed = data.speed; 
            agent.stoppingDistance = data.attackRange - 0.2f;
        }
    }

    public void ApplyExtraHealth(float extraHealth)
    {
        currentHp += extraHealth;
    }

    // --- MÉTODOS DE EFECTOS ESPECIALES ---

    // 1. RALENTIZAR (HIELO)
    public void ApplySlow(float percentage, float duration)
    {
        if (isDead) return;

        // Si ya está ralentizado, reiniciamos el contador
        if (slowCoroutine != null) StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(SlowRoutine(percentage, duration));
    }

    private IEnumerator SlowRoutine(float percentage, float duration)
    {
        // Aplicar lentitud
        agent.speed = originalSpeed * percentage;

        yield return new WaitForSeconds(duration);

        // Restaurar velocidad 
        if (!isCrippled)
        {
            agent.speed = originalSpeed;
        }
        else
        {
            agent.speed = crippledSpeed;
        }
        slowCoroutine = null;
    }

    // 2. EMPUJE (KNOCKBACK)
    public void ApplyKnockback(Vector3 direction, float force)
    {
        if (isDead || agent == null) return;

        // Calculamos posición destino
        Vector3 targetPos = transform.position + (direction * force);

        // Movemos el agente en el NavMesh
        agent.Warp(targetPos);

        // Breve aturdimiento
        if (!isAttacking)
        {
            agent.velocity = Vector3.zero;
            agent.isStopped = true;
            Invoke("ResumeAgent", 0.5f);
        }
    }

    private void ResumeAgent()
    {
        if (!isDead && agent != null) agent.isStopped = false;
    }

    // --- BUCLE PRINCIPAL ---
    void Update()
    {
        if (isDead || player == null) return;

        if (isAttacking)
        {
            if (!agent.isStopped) agent.isStopped = true;
            return;
        }

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
        if (agent.isStopped) agent.isStopped = false;
        agent.SetDestination(player.position);

        bool isMoving = agent.velocity.magnitude > 0.1f;
        animator.SetBool("isWalking", isMoving);
    }

    private void StopAndAttack()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;
        animator.SetBool("isWalking", false);

        Vector3 directionToPlayer = (player.position - transform.position).normalized;
        directionToPlayer.y = 0;
        if (directionToPlayer != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(directionToPlayer);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }

        if (Time.time - lastAttackTime >= zombieData.attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        animator.SetTrigger("Attack");

        if (audioSource != null && attackSounds != null && attackSounds.Length > 0)
        {
            audioSource.PlayOneShot(attackSounds[Random.Range(0, attackSounds.Length)]);
        }

        yield return new WaitForSeconds(0.9f);

        if (player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= zombieData.attackRange + 0.5f)
            {
                PlayerHealth ph = player.GetComponent<PlayerHealth>();
                if (ph != null) ph.TakeDamage(zombieData.damage);
            }
        }

        yield return new WaitForSeconds(1.4f); 
        isAttacking = false;
    }

    // --- SISTEMA DE DAÑO ---
    public void TakeDamage(float amount, EHitboxType partHit)
    {
        if (isDead) return;

        float finalDamage = amount;

        switch (partHit)
        {
            case EHitboxType.Head:
                finalDamage *= 2.0f;
                break;

            case EHitboxType.Legs:
                if (!isCrippled)
                {
                    isCrippled = true;
                    agent.speed = crippledSpeed;
                    originalSpeed = crippledSpeed; 
                }
                break;
        }

        currentHp -= finalDamage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

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
        if (player != null) ps = player.GetComponent<PlayerShooting>();
        if (ps == null) ps = FindAnyObjectByType<PlayerShooting>();
        if (ps != null) ps.RegisterZombieKill();

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