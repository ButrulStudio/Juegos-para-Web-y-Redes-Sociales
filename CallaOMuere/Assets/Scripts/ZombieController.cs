using UnityEngine;
using System.Collections;

public class ZombieController : MonoBehaviour
{
    [SerializeField] private CharacterController zombie;
    [SerializeField] private Transform player;
    [SerializeField] private float speed = 3.0f;
    [SerializeField] private float maxHp = 100f; // La vida base
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRange = 2.0f;
    [SerializeField] private float attackCooldown = 1.5f;

    private float currentHp;
    private float lastAttackTime = 0f;
    private bool isDead = false;
    private bool isAttacking = false;

    // Referencias a otros Managers
    private WaveManager waveManager;
    private ScoreManager scoreManager; // <--- Referencia al ScoreManager

    void Start()
    {
        currentHp = maxHp;
        if (zombie == null) zombie = GetComponent<CharacterController>();

        // Buscar el jugador
        if (player == null) player = GameObject.FindGameObjectWithTag("Player").transform;

        // BUSCAR MANAGERS AL INICIO
        waveManager = FindObjectOfType<WaveManager>();
        scoreManager = FindObjectOfType<ScoreManager>();

        // VERIFICACIÓN DE DEBUG
        if (scoreManager == null)
        {
            Debug.LogError("ERROR GRAVE: ScoreManager NO encontrado en la escena. ¡Los puntos NO se sumarán!");
        }
    }

    void Update()
    {
        if (isDead) return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= attackRange)
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
        if (isAttacking) return;

        Vector3 direction = (player.position - transform.position).normalized;
        zombie.Move(direction * speed * Time.deltaTime);
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    private void StopAndAttack()
    {
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            StartCoroutine(AttackRoutine());
        }
    }

    private IEnumerator AttackRoutine()
    {
        isAttacking = true;
        lastAttackTime = Time.time;

        yield return new WaitForSeconds(0.3f);

        // Aplicar daño al jugador
        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth != null)
        {
            playerHealth.TakeDamage((int)damage);
        }

        yield return new WaitForSeconds(attackCooldown - 0.3f);

        isAttacking = false;
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHp -= amount;
        if (currentHp <= 0f)
        {
            Die();
        }
    }

    public void ApplyHealthMultiplier(float multiplier)
    {
        maxHp = maxHp * multiplier;
        currentHp = maxHp;
    }

    private void Die()
    {
        isDead = true;

        // 1. NOTIFICAR AL SCORE MANAGER Y DAR PUNTOS
        if (scoreManager != null)
        {
            scoreManager.ZombieKilled(); // <--- LLAMADA DE PUNTOS
        }

        // 2. NOTIFICAR AL WAVE MANAGER
        if (waveManager != null)
        {
            waveManager.ZombieDied();
        }

        zombie.enabled = false;
        Destroy(gameObject, 2f);
    }

    public float GetHP()
    {
        return currentHp;
    }
}