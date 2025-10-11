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
    private Vector3 verticalVelocity; // 👈 NUEVA VARIABLE PARA LA GRAVEDAD

    private WaveManager waveManager;
    private ScoreManager scoreManager;

    void Start()
    {
        zombie = GetComponent<CharacterController>();
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
        if (isDead || player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance <= zombieData.attackRange) StopAndAttack();
        else FollowPlayer();
    }

    private void FollowPlayer()
    {
        if (isAttacking) return;

        // --- Lógica de Gravedad (Evitar Flotación) ---
        // Comprueba si está en el suelo usando la propiedad 'isGrounded' del CharacterController
        if (zombie.isGrounded)
        {
            // Fija una pequeña velocidad negativa para mantenerlo pegado al suelo.
            verticalVelocity.y = -2f;
        }
        else
        {
            // Aplica la gravedad (aceleración constante)
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
        }
        // ---------------------------------------------

        // 1. Obtener la dirección horizontal hacia el jugador
        Vector3 dir = player.position - transform.position;
        dir.y = 0; // Eliminar movimiento vertical intencional
        dir = dir.normalized;

        // 2. Combinar el movimiento horizontal con la velocidad vertical
        Vector3 horizontalMovement = dir * zombieData.speed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;

        // 3. Aplicar el movimiento
        zombie.Move(finalMovement * Time.deltaTime);

        // 4. Rotar para mirar al jugador
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    private void StopAndAttack()
    {
        // La gravedad debe seguir aplicándose incluso al detenerse.
        if (!zombie.isGrounded)
        {
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            zombie.Move(verticalVelocity * Time.deltaTime);
        }

        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        if (Time.time - lastAttackTime >= zombieData.attackCooldown)
            StartCoroutine(AttackRoutine());
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