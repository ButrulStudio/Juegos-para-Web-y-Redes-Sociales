using UnityEngine;
using System.Collections;

public class ZombieController : MonoBehaviour
{
    [Header("Datos del zombi")]
    [SerializeField] private ZombieData zombieData;

    private CharacterController zombie;
    private Transform player;
    private float currentHp;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isAttacking = false;
    private Vector3 verticalVelocity;

    private WaveManager waveManager;
    private ScoreManager scoreManager;

    // --- Movimiento Aleatorio ---
    private Vector3 randomDirection;
    private bool isWalkingRandom = false;

    void Start()
    {
        zombie = GetComponent<CharacterController>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        waveManager = FindAnyObjectByType<WaveManager>();
        scoreManager = FindAnyObjectByType<ScoreManager>();

        ApplyZombieData(zombieData);

        StartCoroutine(RandomMovementRoutine());
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
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= zombieData.attackRange)
        {
            StopAndAttack();
        }
        else if (distance <= zombieData.detectionRange)
        {
            FollowPlayer();
        }
        else if (isWalkingRandom)
        {
            MoveRandomly();
        }
    }

    // --- Movimiento Aleatorio ---
    private IEnumerator RandomMovementRoutine()
    {
        while (!isDead)
        {
            isWalkingRandom = true;

            // Dirección aleatoria horizontal
            float angle = Random.Range(0f, 360f);
            randomDirection = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)).normalized;

            // Tiempo de caminata aleatoria
            float walkTime = Random.Range(2f, 5f);
            yield return new WaitForSeconds(walkTime);

            // Parar un tiempo aleatorio
            isWalkingRandom = false;
            float idleTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(idleTime);
        }
    }

    private void MoveRandomly()
    {
        if (isAttacking) return;

        ApplyGravity();

        Vector3 horizontalMovement = randomDirection * zombieData.speed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;
        zombie.Move(finalMovement * Time.deltaTime);

        if (horizontalMovement != Vector3.zero)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(horizontalMovement), 5f * Time.deltaTime);
    }

    private void FollowPlayer()
    {
        if (isAttacking) return;

        ApplyGravity();

        Vector3 dir = player.position - transform.position;
        dir.y = 0;
        dir = dir.normalized;

        Vector3 horizontalMovement = dir * zombieData.speed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;
        zombie.Move(finalMovement * Time.deltaTime);

        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    private void StopAndAttack()
    {
        ApplyGravity();

        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        if (Time.time - lastAttackTime >= zombieData.attackCooldown)
            StartCoroutine(AttackRoutine());
    }

    private void ApplyGravity()
    {
        if (zombie.isGrounded)
            verticalVelocity.y = -2f;
        else
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.3f);

        PlayerHealth ph = player.GetComponent<PlayerHealth>();
        if (ph != null) ph.TakeDamage(zombieData.damage);

        yield return new WaitForSeconds(zombieData.attackCooldown - 0.3f);
        isAttacking = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        currentHp -= amount;
        if (currentHp <= 0) Die();
    }

    private void Die()
    {
        isDead = true;

        CapsuleCollider capsule = GetComponent<CapsuleCollider>();
        if (capsule != null) capsule.enabled = false;

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
