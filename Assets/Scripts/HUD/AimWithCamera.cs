using UnityEngine;

public class AimWithCamera : MonoBehaviour
{
    public Transform cameraTarget; // el mismo del ThirdPersonController
    public bool onlyYaw = true;

    void LateUpdate()
    {
        if (!cameraTarget) return;
        var e = cameraTarget.rotation.eulerAngles;
        if (onlyYaw) transform.rotation = Quaternion.Euler(0f, e.y, 0f);
        else transform.rotation = Quaternion.Euler(e.x, e.y, 0f);

    }
}