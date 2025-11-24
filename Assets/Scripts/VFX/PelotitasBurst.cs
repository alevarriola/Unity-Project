using UnityEngine;

public class PelotitasBurst : MonoBehaviour
{
    [Header("Pelotitas")]
    public GameObject pelotitaPrefab;
    public int count = 5;
    public float minSpeed = 1.5f;
    public float maxSpeed = 3.5f;
    public float upwardBias = 0.5f;     // empuje hacia arriba
    public float spread = 0.4f;         // aleatorio lateral
    public Color color = Color.yellow;  // si tu material soporta color

    [Header("Auto")]
    public float selfDestroy = 0.1f;    // este contenedor se destruye rápido

    void Start()
    {
        Spawn();
        Destroy(gameObject, selfDestroy);
    }

    public void Setup(Color c, int cnt = -1)
    {
        color = c;
        if (cnt > 0) count = cnt;
    }

    void Spawn()
    {
        if (!pelotitaPrefab) return;

        for (int i = 0; i < count; i++)
        {
            // Posici'n con leve jitter alrededor
            Vector3 pos = transform.position + new Vector3(
                Random.Range(-spread, spread),
                Random.Range(0f, spread * 0.5f),
                Random.Range(-spread, spread)
            );

            var go = Instantiate(pelotitaPrefab, pos, Random.rotation);
            var rb = go.GetComponent<Rigidbody>();
            if (rb)
            {
                Vector3 dir = Random.onUnitSphere;
                dir.y = Mathf.Abs(dir.y) + upwardBias;  // mas hacia arriba
                dir.Normalize();

                float spd = Random.Range(minSpeed, maxSpeed);
                rb.linearVelocity = dir * spd;
            }

            // Si tu material soporta color por MaterialPropertyBlock:
            var rend = go.GetComponent<Renderer>();
            if (rend)
            {
                var mpb = new MaterialPropertyBlock();
                rend.GetPropertyBlock(mpb);
                mpb.SetColor("_Color", color);
                mpb.SetColor("_EmissionColor", color * 0.5f);
                rend.SetPropertyBlock(mpb);
            }
        }
    }
}
