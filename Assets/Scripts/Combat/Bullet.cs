using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 50f;
    public float damage = 10f;
    public float lifetime = 3f;
    public GameObject hitVfxPrefab;

    Vector3 _direction;

    public void Launch(Vector3 dir)
    {
        _direction = dir.normalized;
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        transform.position += _direction * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Evitar golpear al mismo jugador
        if (other.CompareTag("Player")) return;

        // VFX
        if (hitVfxPrefab)
        {
            var vfx = Instantiate(hitVfxPrefab, transform.position, Quaternion.LookRotation(_direction));
            Destroy(vfx, 2f);
        }

        // Daño
        if (other.TryGetComponent<IDamageable>(out var dmg))
        {
            dmg.TakeDamage(damage, transform.position, -_direction);
        }

        Destroy(gameObject);
    }
}