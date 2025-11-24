using UnityEngine;

public class PelotitaLifetime : MonoBehaviour
{
    public float lifetime = 3f;
    void OnEnable() { Destroy(gameObject, lifetime); }
}
