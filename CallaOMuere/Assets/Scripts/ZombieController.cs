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

    [Header("Configuración de Evasión")]
    [Tooltip("Máscara de la capa que bloquea la entrada al metro.")]
    public LayerMask metroEntranceMask;

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

        if (zombie.isGrounded)
            verticalVelocity.y = -2f;
        else
            verticalVelocity.y += Physics.gravity.y * Time.deltaTime;

        // Dirección objetivo hacia el jugador (Target Direction)
        Vector3 targetDir = (player.position - transform.position);
        targetDir.y = 0; // Ignoramos el eje Y para la persecución horizontal
        targetDir.Normalize();
        
        // Configuración del Raycasting
        float rayDistance = 2.0f; // Distancia para empezar a reaccionar (Ajustado para mejor evasión)
        int rayCount = 15;
        float maxAngle = 90f; 
        
        // Variables de Puntuación
        Vector3 bestDirection = Vector3.zero;
        float bestScore = float.MinValue; 
        Vector3 origin = transform.position + Vector3.up * 0.5f;

        for (int i = 0; i < rayCount; i++)
        {
            // Generar Rayos en un arco centrado en la dirección actual del zombi
            float t = (float)i / (rayCount - 1);
            float angle = Mathf.Lerp(-maxAngle, maxAngle, t);
            
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            RaycastHit hit;
            
            // Raycast principal para verificar BLOQUEO físico (Ignoramos Triggers)
            bool blocked = Physics.Raycast(origin, dir, rayDistance, ~0, QueryTriggerInteraction.Ignore);
            // Debug.DrawRay(origin, dir * rayDistance, blocked ? Color.red : Color.green); 

            // --- Sistema de Puntuación (Scoring) ---
            float alignmentScore = Vector3.Dot(dir, targetDir); 
            
            float avoidanceScore = blocked ? -8.0f : 1.0f; 
            
            float metroPenalty = 0f;
            // Raycast específico para golpear la capa de la zona prohibida (Debe ser un Trigger en esa capa)
            if (Physics.Raycast(origin, dir, out hit, rayDistance, metroEntranceMask))
            {
                // Castigo MUY ALTO para forzar al zombi a girar y buscar otra ruta.
                metroPenalty = -15.0f; 
            }

            // Puntuación combinada (El peso de 3.0f prioriza la evasión sobre la persecución)
            float score = (alignmentScore * 1.0f) + (avoidanceScore * 3.0f) + metroPenalty; 

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = dir;
            }
        }

        // Seguridad: Si todos los rayos están bloqueados (score es muy bajo), el zombi sigue hacia el jugador.
        if (bestDirection == Vector3.zero || bestScore < -5.0f) 
            bestDirection = targetDir;


        Collider[] nearby = Physics.OverlapSphere(transform.position, 1.0f); // Radio más amplio
        foreach (var col in nearby)
        {
            if (col.CompareTag("Zombie") && col.gameObject != this.gameObject)
            {
                Vector3 away = transform.position - col.transform.position;
                away.y = 0;
                // Mayor fuerza de separación para evitar amontonamiento en obstáculos
                bestDirection += away.normalized * 1.0f; 
            }
        }
        bestDirection.Normalize();

        Vector3 horizontalMovement = bestDirection * zombieData.speed;
        Vector3 finalMovement = horizontalMovement + verticalVelocity;
        zombie.Move(finalMovement * Time.deltaTime);

        // El zombi mira en la dirección calculada de movimiento (Steering)
        if (horizontalMovement.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(horizontalMovement);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 10f); 
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
