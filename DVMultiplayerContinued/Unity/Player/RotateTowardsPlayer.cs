using UnityEngine;

internal class RotateTowardsPlayer : MonoBehaviour
{
#pragma warning disable IDE0051 // Remove unused private members
    private Camera camera;
    private void Awake()
    {
        camera = PlayerManager.PlayerCamera;
    }

    private void Update()
    {
        transform.LookAt(transform.position + camera.transform.rotation * Vector3.forward, camera.transform.rotation * Vector3.up);
    }
}