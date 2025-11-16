using UnityEngine;
using System.Collections;

public class ZombieController : MonoBehaviour
{
    [Header("Datos del zombi")]
    [SerializeField] private ZombieData zombieData;

    // ¡NUEVA VARIABLE!
    [Header("Ajustes de Combate")]
    [Tooltip("La velocidad a la que se moverá el zombi si le disparan en la pierna.")]
    [SerializeField] private float crippledSpeed = 1.5f; // Puedes cambiar este valor en el Inspector

    private CharacterController zombie;
    private Transform player;
    private Animator animator;
    private float currentHp;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isAttacking = false;
    private Vector3 verticalVelocity;

    private WaveManager waveManager;
    private ScoreManager scoreManager;

    [Header("Configuración de Evasión")]
    [Tooltip("Máscara de la capa que bloquea la entrada al metro.")]
    public LayerMask metroEntranceMask;

    private float currentSpeed;
    private bool isCrippled = false;

    void Start()
    {
        zombie = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        waveManager = FindAnyObjectByType<WaveManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();

        ApplyZombieData(zombieData);

        if (zombieData != null)
        {
            currentSpeed = zombieData.speed;
        }
    }

    public void ApplyZombieData(ZombieData data)
    {
        zombieData = data;
        currentHp = data.maxHp;
    }

    public void ApplyHealthMultiplier(float multiplier)
    {
        currentHp *= multiplier;
    }

    void Update()
    {
        if (isDead || player == null || isAttacking) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= zombieData.attackRange)
        {
            StopAndAttack();
        }
        else
        {
            FollowPlayer();
        }
    }

    private void FollowPlayer()
    {
        animator.SetBool("isWalking", true);

        if (zombie.isGrounded)
            verticalVelocity.y = -2f;
        else
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;

        Vector3 targetDir = (player.position - transform.position);
        targetDir.y = 0;
        targetDir.Normalize();

        float rayDistance = 2.0f;
        int rayCount = 15;
        float maxAngle = 90f;

        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.MinValue;
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            float t = (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-maxAngle, maxAngle, t);

            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;

            bool blocked = Physics.Raycast(origin, dir, rayDistance, ~0, QueryTriggerInteraction.Ignore);

            float alignmentScore = Vector3.Dot(dir, targetDir);
            float avoidanceScore = blocked ? -8.0f : 1.0f;

            float metroPenalty = 0f;
            if (Physics.Raycast(origin, dir, out hit, rayDistance, metroEntranceMask))
            {
                metroPenalty = -15.0f;
            }

            float score = (alignmentScore * 1.0f) + (avoidanceScore * 3.0f) + metroPenalty;

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        if (bestDirection == Vector3.zero || bestScore < -5.0f)
            bestDirection = targetDir;


        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.0f);
        foreach (var col in nearby)
        {
            if (col.CompareTag("Zombie") && col.gameObject != this.gameObject)
            {
                Vector3 away = transform.position - col.transform.position;
                away.y = 0;
                bestDirection += away.normalized * 1.0f;
            }
        }
        bestDirection.Normalize();

        // Usa la variable 'currentSpeed'
        Vector3 horizontalMovement = bestDirection * currentSpeed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;
        zombie.Move(finalMovement * Time.deltaTime);

        if (horizontalMovement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f);
        }
    }

    private void StopAndAttack()
    {
        animator.SetBool("isWalking", false);

        if (!zombie.isGrounded)
        {
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            zombie.Move(verticalVelocity * Time.deltaTime);
        }

        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

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

        yield return new WaitForSeconds(0.9f);

        if (Vector3.Distance(transform.position, player.position) <= zombieData.attackRange)
        {
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(zombieData.damage);
        }

        float animationTime = 2.3f;
        float waitAfterHitPoint = animationTime - 0.9f;

        yield return new WaitForSeconds(waitAfterHitPoint);

        float remainingCooldown = zombieData.attackCooldown - animationTime;
        if (remainingCooldown > 0)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        isAttacking = false;
    }

    public void TakeDamage(float amount)
    {
        TakeDamage(amount, EHitboxType.Body);
    }

    // --- ¡FUNCIÓN DE DAÑO MODIFICADA! ---
    public void TakeDamage(float amount, EHitboxType partHit)
    {
        if (isDead) return;

        // 'amount' ya es el daño del arma (con multiplicadores de power-up si los hay)
        float finalDamage = amount;

        switch (partHit)
        {
            // ¡LÓGICA DE DAÑO X2!
            case EHitboxType.Head:
                finalDamage *= 2.0f; // Duplica el daño recibido
                Debug.Log($"¡Disparo a la cabeza! Daño total: {finalDamage}");
                break;

            // ¡LÓGICA DE RALENTIZACIÓN!
            case EHitboxType.Legs:
                finalDamage = amount; // Daño normal

                if (!isCrippled)
                {
                    isCrippled = true;
                    // Asigna la velocidad pública que definiste en el Inspector
                    currentSpeed = crippledSpeed;
                    Debug.Log($"¡Pierna herida! Zombie ralentizado a {crippledSpeed}");
                }
                break;

            case EHitboxType.Body:
            default:
                finalDamage = amount; // Daño normal
                break;
        }

        currentHp -= finalDamage;

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.enabled = false;
        }

        zombie.enabled = false;

        if (scoreManager != null) scoreManager.ZombieKilled();
        if (waveManager != null) waveManager.ZombieDied();

        Destroy(gameObject, 2f);
    }

    public float GetHP()
    {
        return currentHp;
    }
}