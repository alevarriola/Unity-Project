using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour
{
    [SerializeField] public int ammoAmount = 15;

    // Para configurarlo por código desde el enemigo
    public void SetAmmoAmount(int amount)
    {
        ammoAmount = Mathf.Max(0, amount);
    }

    void Reset()
    {
        // Garantizar que el collider sea trigger
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Buscamos un GunController en el player o hijos
        var gun = other.GetComponentInChildren<GunController>();
        if (gun == null) return;

        if (ammoAmount > 0)
        {
            gun.AddReserveAmmo(ammoAmount);
        }

        Destroy(gameObject);
    }
}
