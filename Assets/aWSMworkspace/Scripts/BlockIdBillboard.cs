using UnityEngine;

/// <summary>Keeps a transform facing the camera (for world-space block type labels).</summary>
public class BlockIdBillboard : MonoBehaviour
{
    public Camera TargetCamera;

    void LateUpdate()
    {
        if (TargetCamera == null)
            return;

        Vector3 toCam = TargetCamera.transform.position - transform.position;
        if (toCam.sqrMagnitude < 1e-6f)
            return;

        // TMP faces -Z; flip 180° on Y so text reads correctly toward the camera.
        transform.rotation = Quaternion.LookRotation(toCam, Vector3.up) * Quaternion.Euler(0f, 180f, 0f);
    }
}
