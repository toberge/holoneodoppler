using UnityEngine;

public class RaycastAngle : MonoBehaviour
{
    public delegate void RaycastEvent(float angle, float overlap);

    public RaycastEvent OnRaycastUpdate;

    public delegate void StatusEvent();

    public StatusEvent OnNoIntersection;
    public StatusEvent OnIntersection;

    [SerializeField] private UltrasoundVisualiser visualiser;

    private DepthWindow depthWindow;

    private float currentAngle;
    private int previousAngle;
    private float previousOverlap = Mathf.NegativeInfinity;

    private const float AngleAccuracy = 0.5f;
    private const float OverlapAccuracy = 0.1f;

    private bool notifiedAboutNoIntersection = false;

    private int layerMask;
    private int skullLayer;

    private void Start()
    {
        depthWindow = GetComponent<DepthWindow>();
        layerMask = LayerMask.GetMask("Artery", "Skull");
        skullLayer = LayerMask.NameToLayer("Skull");
    }

    private void HandleNoIntersection(bool drawRay = true)
    {
        if (drawRay)
        {
            Debug.DrawRay(transform.position, transform.forward * 1000, Color.white);
        }

        if (!notifiedAboutNoIntersection)
        {
            OnNoIntersection?.Invoke();
            OnRaycastUpdate?.Invoke(90, 0);
            visualiser.OnNoIntersection();
            notifiedAboutNoIntersection = true;
            currentAngle = -1000;
        }
    }

    void FixedUpdate()
    {
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.forward, out var hit, Mathf.Infinity, layerMask))
        {
            // Abort if we hit the skull!
            if (hit.transform.gameObject.layer == skullLayer)
            {
                Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
                HandleNoIntersection(false);
                return;
            }

            notifiedAboutNoIntersection = false;
            OnIntersection?.Invoke();

            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.yellow);

            var topHit = hit.point;
            var bottomHit = Vector3.negativeInfinity;
            if (Physics.Raycast(transform.position + transform.forward, -transform.forward, out hit, Mathf.Infinity,
                    layerMask))
            {
                bottomHit = hit.point;
                Debug.DrawRay(bottomHit, topHit - bottomHit, Color.green);
            }

            float overlap = depthWindow.CalculateOverlap(topHit, bottomHit);

            previousAngle = Mathf.RoundToInt(currentAngle);
            // Find angle between ray and blood flow.
            currentAngle = Vector3.Angle(transform.forward, hit.transform.forward);

            if (Mathf.Abs(currentAngle - previousAngle) > AngleAccuracy ||
                Mathf.Abs(overlap - previousOverlap) > OverlapAccuracy)
            {
                Debug.Log(
                    $"Angle changed from {previousAngle:F1} to {currentAngle:F1}, overlap from {previousOverlap:F1} to {overlap:F1}.");
                OnRaycastUpdate?.Invoke(currentAngle, overlap);
                // If the probe is closely aligned to (or away from) the blood flow:
                visualiser.OnIntersection(currentAngle < 30 || currentAngle > 150);
                previousOverlap = overlap;
            }
        }
        else
        {
            HandleNoIntersection();
        }
    }
}