using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(EnemyChaser))]
public class EnemyMeleeAttack : MonoBehaviour
{
    [Header("Ataque cuerpo a cuerpo")]
    public float damage = 12f;              // daño por golpe
    public float attackCooldown = 1.5f;     // tiempo entre golpes
    public float attackRange = 1.5f;        // distancia efectiva
    public float attackWindup = 0.2f;       // pequeño delay antes de aplicar daño
    public bool debugAttackRange = true;

    [Header("Feedback")]
    public GameObject attackVfx;            // opcional (impacto)
    public AudioClip attackSfx;             // opcional
    public AudioSource audioSource;

    EnemyChaser _chaser;
    Transform _player;
    float _nextAttackTime;
    NavMeshAgent _agent;
    PlayerHealth _playerHealth;

    void Awake()
    {
        _chaser = GetComponent<EnemyChaser>();
        _agent = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        if (!_chaser || !_chaser.enabled) return;
        _player = _chaser.player;
        if (!_player) return;

        float dist = Vector3.Distance(transform.position, _player.position);

        // ¿Está dentro del rango de ataque?
        if (dist <= attackRange && Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + attackCooldown;
            StartCoroutine(PerformAttack());
        }
    }

    System.Collections.IEnumerator PerformAttack()
    {
        yield return new WaitForSeconds(attackWindup);

        if (!_player) yield break;

        // Asegurarnos de seguir en rango
        float dist = Vector3.Distance(transform.position, _player.position);
        if (dist > attackRange * 1.2f) yield break;

        // Buscar componente de vida del jugador
        if (!_playerHealth)
            _playerHealth = _player.GetComponent<PlayerHealth>();

        if (_playerHealth)
        {
            _playerHealth.TakeDamage(damage);
        }

        if (attackVfx)
        {
            Instantiate(attackVfx, _player.position + Vector3.up * 1.2f, Quaternion.identity);
        }

        if (attackSfx && audioSource)
        {
            audioSource.PlayOneShot(attackSfx);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!debugAttackRange) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}