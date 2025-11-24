using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChaser : MonoBehaviour
{
    [Header("Player")]
    public Transform player;           // Asignado en runtime por el Spawner

    [Header("Persecución")]
    public float detectRange = 25f;    // rango para empezar a perseguir
    public float giveUpRange = 35f;    // si el player sale más lejos, deja de perseguir
    public float repathInterval = 0.15f;
    public float faceTargetSpeed = 10f; // suaviza giro al final
    public bool requireLineOfSight = false;
    public LayerMask losMask = ~0;     // qué capas bloquean la visión

    [Header("Movimiento")]
    [Tooltip("Velocidad base del agente NavMesh (puede cambiarse en tiempo real).")]
    public float moveSpeed = 3.5f;     // velocidad configurable
    [Tooltip("Aceleración al moverse hacia el jugador.")]
    public float acceleration = 8f;

    NavMeshAgent _agent;
    float _nextRepathTime;
    bool _hasLOS;

    void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;
        _agent.acceleration = acceleration;
    }

    void OnEnable()
    {
        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        // Asegura que la velocidad se aplique al activarse (por si se ajusta en prefab)
        if (_agent)
        {
            _agent.speed = moveSpeed;
            _agent.acceleration = acceleration;
        }
    }

    // Permite asignar el Player desde el Spawner
    public void SetPlayer(Transform p)
    {
        player = p;
    }

    // Permite cambiar velocidad dinámicamente desde otros scripts
    public void SetMoveSpeed(float newSpeed)
    {
        moveSpeed = newSpeed;
        if (_agent) _agent.speed = newSpeed;
    }

    void Update()
    {
        if (!player || !_agent) return;

        // Actualiza la velocidad si cambió en el Inspector
        if (_agent.speed != moveSpeed)
            _agent.speed = moveSpeed;

        float dist = Vector3.Distance(transform.position, player.position);

        // Línea de visión (opcional)
        _hasLOS = true;
        if (requireLineOfSight)
        {
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 dir = (player.position + Vector3.up * 1.2f) - origin;
            _hasLOS = !Physics.Raycast(origin, dir.normalized, dir.magnitude, losMask, QueryTriggerInteraction.Ignore);
        }

        // Lógica de persecución
        if ((dist <= detectRange && _hasLOS) || (_agent.hasPath && dist <= giveUpRange))
        {
            // repath cada X segs
            if (Time.time >= _nextRepathTime)
            {
                _agent.SetDestination(player.position);
                _nextRepathTime = Time.time + repathInterval;
            }

            // Si está cerca, mirar al jugador suavemente
            if (dist <= _agent.stoppingDistance + 0.2f)
            {
                Vector3 lookDir = (player.position - transform.position);
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.0001f)
                {
                    Quaternion target = Quaternion.LookRotation(lookDir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, target, faceTargetSpeed * Time.deltaTime);
                }
            }
        }
        else
        {
            // Parar si se aleja demasiado
            if (!_agent.isStopped)
                _agent.ResetPath();
        }
    }
}