using UnityEngine;
using System.Collections;

public class ZombieController : MonoBehaviour
{
    [Header("Datos del zombi")]
    [SerializeField] private ZombieData zombieData;

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

    void Start()
    {
        zombie = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player").transform;

        waveManager = FindAnyObjectByType<WaveManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();

        ApplyZombieData(zombieData);
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
        // Si está muerto, atacando, o no hay jugador, no hacer nada.
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

        // --- Gravedad ---
        if (zombie.isGrounded)
            verticalVelocity.y = -2f;
        else
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;

        // Dirección objetivo hacia el jugador
        Vector3 targetDir = (player.position - transform.position);
        targetDir.y = 0;
        targetDir.Normalize();

        // --- Steering avanzado con análisis de múltiples direcciones ---
        float rayDistance = 1.2f;
        float angleRange = 120f;
        int rayCount = 15;

        Vector3 bestDirection = Vector3.zero;
        float bestScore = -9999f;

        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            // generar el ángulo del rayo
            float t = (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-angleRange / 2, angleRange / 2, t);

            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            // hacemos raycast
            bool blocked = Physics.Raycast(origin, dir, rayDistance, ~0, QueryTriggerInteraction.Ignore);

            // calcular la alineación con el jugador (preferimos ángulos cercanos a targetDir)
            float alignment = Vector3.Dot(dir, targetDir);

            // puntuación de esta dirección
            float score = alignment - (blocked ? 2.0f : 0f);

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        // Seguridad: si todo fallara, sigue hacia el jugador
        if (bestDirection == Vector3.zero)
            bestDirection = targetDir;

        // --- Separación entre zombies ---
        Collider[] nearby = Physics.OverlapSphere(transform.position, 0.6f);
        foreach (var col in nearby)
        {
            if (col.CompareTag("Zombie") && col.gameObject != this.gameObject)
            {
                Vector3 away = transform.position - col.transform.position;
                away.y = 0;
                bestDirection += away.normalized * 0.4f;
            }
        }
        bestDirection.Normalize();

        // --- Movimiento final ---
        Vector3 horizontalMovement = bestDirection * zombieData.speed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;
        zombie.Move(finalMovement * Time.deltaTime);

        // --- Rotación suave ---
        Vector3 lookDir = new Vector3(player.position.x, transform.position.y, player.position.z) - transform.position;
        if (lookDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDir);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
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

        // Esperar el resto del cooldown
        yield return new WaitForSeconds(zombieData.attackCooldown - 0.9f);

        isAttacking = false; // Desbloquea Update()
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHp -= amount;
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
