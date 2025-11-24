using UnityEngine;

public class PickupVisual : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float bobAmplitude = 0.1f;
    [SerializeField] private float bobFrequency = 2f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // Rotación constante
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);

        // Movimiento vertical tipo "flotar"
        float newY = _startPos.y + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        Vector3 pos = transform.position;
        pos.y = newY;
        transform.position = pos;
    }
}
