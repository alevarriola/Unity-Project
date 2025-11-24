using UnityEngine;

[RequireComponent(typeof(Collider))]
public class EnemyDummy : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    public float maxHealth = 50f;
    public GameObject hitVfxPrefab; // opcional
    float _hp;

    [Header("Drop de munición")]
    [Range(0f, 1f)]
    public float dropChance = 0.35f; // probabilidad de drop
    public int minAmmoDrop = 15;
    public int maxAmmoDrop = 20;
    public GameObject ammoPickupPrefab; // tu AmmoDrop.prefab

    void Awake() => _hp = maxHealth;

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        _hp -= amount;

        if (hitVfxPrefab)
        {
            var vfx = Instantiate(hitVfxPrefab, hitPoint, Quaternion.LookRotation(hitNormal));
            Destroy(vfx, 2f);
        }

        if (_hp <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        TryDropAmmo();
        Destroy(gameObject);
    }

    void TryDropAmmo()
    {
        // Tirar dado
        if (Random.value > dropChance) return;

        // Cantidad aleatoria
        int amount = Random.Range(minAmmoDrop, maxAmmoDrop + 1);

        // Instanciar el pickup
        var pickup = Instantiate(ammoPickupPrefab, transform.position, Quaternion.identity);

        // Pasarle la cantidad 
        var ammoPickup = pickup.GetComponent<AmmoPickup>();
        if (ammoPickup != null)
        {
            ammoPickup.ammoAmount = amount;
        }
    }
}