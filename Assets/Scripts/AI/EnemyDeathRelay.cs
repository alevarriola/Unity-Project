using UnityEngine;

public class EnemyDeathRelay : MonoBehaviour
{
    public System.Action OnDied;

    void OnDestroy()
    {
        OnDied?.Invoke();
    }
}
