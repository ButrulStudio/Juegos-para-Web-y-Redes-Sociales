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
        Vector3 dir = (player.position - transform.position).normalized;
        zombie.Move(dir * zombieData.speed * Time.deltaTime);
        transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
    }

    private void StopAndAttack()
    {
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
        if (ph != null) ph.TakeDamage((int)zombieData.damage);

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
        if (scoreManager != null) scoreManager.ZombieKilled();
        if (waveManager != null) waveManager.ZombieDied();
        zombie.enabled = false;
        Destroy(gameObject, 2f);
    }

    public float GetHP()
    {
        return currentHp;
    }
}
