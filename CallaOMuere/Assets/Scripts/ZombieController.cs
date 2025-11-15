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

        // Gravedad
        if (zombie.isGrounded)
            verticalVelocity.y = -2f;
        else
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;

        // Dirección objetivo hacia el jugador (Target Direction)
        Vector3 targetDir = (player.position - transform.position);
        targetDir.y = 0;
        targetDir.Normalize();

        // Steering y Evasión de Obstáculos
        
        // Configuración del Raycasting
        float rayDistance = 3.5f; // Distancia para empezar a reaccionar
        int rayCount = 15;
        float maxAngle = 90f; // Solo analizamos 90 grados a cada lado
        
        // Variables de Puntuación
        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.MinValue; // Usamos MinValue para un punto de partida seguro
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            // Generar Rayos en un arco centrado en la dirección actual del zombie
            float t = (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-maxAngle, maxAngle, t);
            
            // Crear la dirección del rayo relativa a la rotación del Zombi
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            // Verificar bloqueo: Ignoramos triggers (como PowerUps)
            bool blocked = Physics.Raycast(origin, dir, rayDistance, ~0, QueryTriggerInteraction.Ignore);
            // Debug.DrawRay(origin, dir * rayDistance, blocked ? Color.red : Color.green); // Descomentar para ver los rayos

            // Sistema de Puntuación (Scoring)
            // Puntuación base: Queremos ir hacia el jugador.
            float alignmentScore = Vector3.Dot(dir, targetDir); 

            // Puntuación de evasión: Es el factor más importante.
            float avoidanceScore = blocked ? -5.0f : 1.0f; // Puntuación negativa alta si está bloqueado
            
            // Puntuación combinada
            // Le damos más peso a la evasión que a la persecución.
            float score = (alignmentScore * 1.0f) + (avoidanceScore * 3.0f); 

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        // Seguridad: Si todos los rayos están bloqueados (bestScore sigue siendo bajo), usamos la dirección más alineada con el jugador
        if (bestDirection == Vector3.zero || bestScore < 0) 
            bestDirection = targetDir;


        // Separación entre zombies (Flocking)
        Collider[] nearby = Physics.OverlapSphere(transform.position, 0.8f); // Aumentamos un poco el radio
        foreach (var col in nearby)
        {
            if (col.CompareTag("Zombie") && col.gameObject != this.gameObject)
            {
                Vector3 away = transform.position - col.transform.position;
                away.y = 0;
                // Se normaliza la dirección y se le da un peso para influir en bestDirection
                bestDirection += away.normalized * 0.6f; // Aumentamos la fuerza de separación (0.6f)
            }
        }
        bestDirection.Normalize();

        Vector3 horizontalMovement = bestDirection * zombieData.speed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;
        zombie.Move(finalMovement * Time.deltaTime);

        if (horizontalMovement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); // Rotación más rápida
        }
    }

    private void StopAndAttack()
    {
        // Detiene la animación de caminar
        animator.SetBool("isWalking", false); 

        if (!zombie.isGrounded)
        {
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;
            zombie.Move(verticalVelocity * Time.deltaTime);
        }

        // Rotación: Asegura que el zombi mire al jugador mientras está parado o atacando.
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));

        // Si el cooldown ha terminado, inicia el ataque
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
        
        float animationTime = 1.2f; // <-- REEMPLAZA ESTE VALOR con la duración real de tu animación.
        float waitAfterHitPoint = animationTime - 0.9f; // Tiempo restante de la animación

        // Esperar el resto de la animación
        yield return new WaitForSeconds(waitAfterHitPoint);
        
        // Esperar el resto del cooldown si es mayor que la duración de la animación
        float remainingCooldown = zombieData.attackCooldown - animationTime;
        if (remainingCooldown > 0)
        {
            yield return new WaitForSeconds(remainingCooldown);
        }

        // El zombi está listo para volver a caminar y atacar
        isAttacking = false; // Desbloquea Update() y reanuda FollowPlayer()
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
