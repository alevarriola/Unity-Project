using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Prefab del enemigo (debe tener NavMeshAgent/EnemyChaser/IDamageable).")]
    public GameObject enemyPrefab;

    [Tooltip("Ground que define el área de spawn (usaremos sus bounds). Puede ser MeshRenderer o Collider.")]
    public Renderer groundRenderer;     // Asigná el MeshRenderer del ground
    public Collider groundCollider;     // O asigná el Collider del ground (si no hay Renderer)
    public Transform player;            // Player (para distancia mínima)

    [Header("Spawn Config")]
    public float spawnInterval = 10f;   // cada cuántos segundos intenta spawnear
    public int maxActive = 3;           // máximo enemigos activos en escena
    public float minDistanceToPlayer = 6f;   // no spawnear muy cerca del jugador
    public float minSeparationBetweenEnemies = 3f; // separación mínima entre enemigos
    public float navmeshSnapRadius = 2.5f;   // radio para buscar NavMesh cerca del punto elegido
    public int areaMask = NavMesh.AllAreas;  // qué áreas del NavMesh admite

    [Header("Altura / Raycast")]
    public float raycastHeightAboveGround = 2f; // desde dónde raycastear hacia abajo al terreno
    public LayerMask groundMask = ~0;           // para raycast al suelo (opcional)

    // Internos
    readonly List<GameObject> _active = new List<GameObject>();
    public System.Action<GameObject> OnSpawned;

    Bounds _groundBounds;
    bool _running;

    void OnEnable()
    {
        // Resolver bounds del ground al habilitar
        _groundBounds = GetGroundBounds();
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    void OnDisable()
    {
        StopSpawner();
    }

    public void StartSpawner()
    {
        if (_running) return;
        _running = true;
        StartCoroutine(SpawnerLoop());
    }

    public void StopSpawner()
    {
        if (!_running) return;
        _running = false;
        StopAllCoroutines();
    }

    IEnumerator SpawnerLoop()
    {
        var wait = new WaitForSeconds(spawnInterval);
        while (_running)
        {
            PruneList();

            if (_active.Count < maxActive && enemyPrefab && _groundBounds.size.sqrMagnitude > 0.0001f)
            {
                TrySpawnOne();
            }

            yield return wait;
        }
    }

    void TrySpawnOne()
    {
        const int MAX_TRIES = 20;
        for (int i = 0; i < MAX_TRIES; i++)
        {
            if (TryGetRandomSpawnPoint(out Vector3 spawnPos))
            {
                var go = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
                _active.Add(go);

                // pasar Player al chaser
                var chaser = go.GetComponent<EnemyChaser>() ?? go.GetComponentInChildren<EnemyChaser>();
                if (chaser && player) chaser.SetPlayer(player);

                // avisar al mundo que nació un enemigo
                OnSpawned?.Invoke(go);
                return;
            }
        }
    }

    bool TryGetRandomSpawnPoint(out Vector3 spawnPos)
    {
        spawnPos = Vector3.zero;

        // 1) Punto aleatorio dentro de los bounds del ground
        Vector3 p = RandomPointInGroundBounds();

        // 2) Ajustar Y con un raycast hacia abajo (opcional, ayuda con terrenos no planos)
        Vector3 rayOrigin = new Vector3(p.x, _groundBounds.max.y + raycastHeightAboveGround, p.z);
        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, _groundBounds.size.y + raycastHeightAboveGround * 2f, groundMask, QueryTriggerInteraction.Ignore))
        {
            p = hit.point + Vector3.up * 0.05f; // un pelín sobre el suelo
        }
        else
        {
            // Si no hay suelo para este punto, descartamos
            return false;
        }

        // 3) Snap al NavMesh cercano
        if (!NavMesh.SamplePosition(p, out NavMeshHit navHit, navmeshSnapRadius, areaMask))
        {
            return false;
        }

        Vector3 candidate = navHit.position;

        // 4) Reglas de distancia mínima
        if (player && Vector3.Distance(candidate, player.position) < minDistanceToPlayer)
            return false;

        // 5) Separación mínima con otros enemigos
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var go = _active[i];
            if (go == null) continue;
            if (Vector3.Distance(candidate, go.transform.position) < minSeparationBetweenEnemies)
                return false;
        }

        spawnPos = candidate;
        return true;
    }

    Vector3 RandomPointInGroundBounds()
    {
        Bounds b = _groundBounds;
        float x = Random.Range(b.min.x, b.max.x);
        float z = Random.Range(b.min.z, b.max.z);
        // Y provisional: a la mitad de la altura
        float y = b.center.y + 0.5f;
        return new Vector3(x, y, z);
    }

    Bounds GetGroundBounds()
    {
        if (groundRenderer != null) return groundRenderer.bounds;
        if (groundCollider != null) return groundCollider.bounds;

        // Si no asignaste nada, intentamos encontrar un Renderer en este objeto
        var r = GetComponentInChildren<Renderer>();
        if (r) return r.bounds;

        var c = GetComponentInChildren<Collider>();
        if (c) return c.bounds;

        Debug.LogWarning("EnemySpawner: No se encontró Renderer/Collider para calcular el área. Asigna 'groundRenderer' o 'groundCollider'.");
        return new Bounds(transform.position, Vector3.zero);
    }

    void PruneList()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            if (_active[i] == null) _active.RemoveAt(i);
        }
    }

    // === Gizmos para depuración ===
    public Color boundsColor = new Color(0f, 1f, 0.5f, 0.15f);
    public Color outlineColor = new Color(0f, 1f, 0.5f, 0.9f);

    void OnDrawGizmosSelected()
    {
        Bounds b = Application.isPlaying ? _groundBounds : GetGroundBounds();
        if (b.size.sqrMagnitude < 0.0001f) return;

        Gizmos.color = boundsColor;
        Gizmos.DrawCube(b.center, b.size);

        Gizmos.color = outlineColor;
        Gizmos.DrawWireCube(b.center, b.size);
    }
}