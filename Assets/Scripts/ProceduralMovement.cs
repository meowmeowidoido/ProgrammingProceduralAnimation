using UnityEngine;

public class ProceduralMovement : MonoBehaviour
{

    [Header("References")]
    public Transform leftFootTarget;
    public Transform rightFootTarget;
    public Transform leftFootHome;
    public Transform rightFootHome;
    public Transform hips;

    [Header("Walk Settings")]
    public float stepLength = 0.4f;
    public float stepHeight = 0.15f;
    public float stepSpeed = 2f;
    [Range(0f, 1f)] public float stepOffset = 0.5f; // Right foot phase offset
    public float hipBobHeight = 0.05f;
    public float hipBobSpeed = 2f;

    private Vector3 leftHomeStart;
    private Vector3 rightHomeStart;
    private Vector3 leftFootPos;
    private Vector3 rightFootPos;

    void Start()
    {
        leftHomeStart = leftFootHome.position;
        rightHomeStart = rightFootHome.position;

        leftFootPos = leftHomeStart;
        rightFootPos = rightHomeStart;
    }

    void Update()
    {
        // Walk cycle (sinusoidal)
        float time = Time.time * stepSpeed;

        float leftPhase = Mathf.Sin(time);
        float rightPhase = Mathf.Sin(time + Mathf.PI); // 180° out of phase

        // Calculate foot movement
        UpdateFoot(leftFootTarget, leftFootHome, leftPhase, ref leftFootPos);
        UpdateFoot(rightFootTarget, rightFootHome, rightPhase, ref rightFootPos);

        // Hip bob
        float hipOffset = Mathf.Sin(time * hipBobSpeed * 2f) * hipBobHeight;
        Vector3 hipsPos = hips.localPosition;
        hipsPos.y = hipOffset;
        hips.localPosition = hipsPos;
    }

    void UpdateFoot(Transform footTarget, Transform home, float phase, ref Vector3 currentPos)
    {
        // Forward/backward offset
        Vector3 forwardOffset = home.forward * phase * stepLength;

        // Vertical lift on swing phase
        float heightOffset = Mathf.Max(0f, Mathf.Sin(phase * Mathf.PI)) * stepHeight;

        // Combine offsets
        Vector3 desiredPos = home.position + forwardOffset + Vector3.up * heightOffset;

        // Smoothly move target
        currentPos = Vector3.Lerp(currentPos, desiredPos, Time.deltaTime * stepSpeed * 2f);

        // Apply to target
        footTarget.position = currentPos;
    }
}
