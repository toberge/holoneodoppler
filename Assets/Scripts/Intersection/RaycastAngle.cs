using System.Linq;
using UnityEngine;

public class RaycastAngle : MonoBehaviour
{
    public delegate void RaycastEvent(float angle, float overlap);

    public RaycastEvent OnRaycastUpdate;

    public delegate void StatusEvent();

    public StatusEvent OnNoIntersection;
    public StatusEvent OnIntersection;

    private DepthWindow depthWindow;

    private float currentAngle;
    private int previousAngle;
    private float previousOverlap;

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

    private void HandleNoIntersection()
    {
        if (notifiedAboutNoIntersection)
        {
            return;
        }

        OnNoIntersection?.Invoke();
        OnRaycastUpdate?.Invoke(90, 0);
        notifiedAboutNoIntersection = true;
        currentAngle = -1000;
    }

    private void FixedUpdate()
    {
        var origin = transform.position;
        // Does the ray intersect any object in the artery or skull layer?
        if (!Physics.Raycast(origin, transform.forward, out var hit, Mathf.Infinity, layerMask))
        {
            Debug.DrawRay(origin, transform.forward * 1000, Color.white);
            HandleNoIntersection();
            return;
        }

        // Abort if we hit the skull!
        if (hit.transform.gameObject.layer == skullLayer)
        {
            Debug.DrawRay(origin, transform.forward * hit.distance, Color.red);
            HandleNoIntersection();
            return;
        }

        notifiedAboutNoIntersection = false;
        OnIntersection?.Invoke();

        Debug.DrawRay(origin, transform.forward * hit.distance, Color.yellow);

        // Raycast from the top to the bottom of the depth window to check if we hit anything inside it
        var hits = Physics.RaycastAll(depthWindow.Top, transform.forward, depthWindow.WindowSize / 100f, layerMask);
        if (hits.Length == 0)
        {
            Debug.DrawRay(depthWindow.Top, transform.forward * 1000, Color.white);
            HandleNoIntersection();
            return;
        }

        if (hits.First().transform.gameObject.layer == skullLayer)
        {
            Debug.DrawRay(origin, depthWindow.Top * hit.distance, Color.red);
            HandleNoIntersection();
            return;
        }

        // Otherwise, handle the hit!
        var overlap = depthWindow.CalculateOverlap(hits.First().point);

        previousAngle = Mathf.RoundToInt(currentAngle);
        // Find angle between ray and blood flow; average of all angles in the intersection.
        currentAngle = hits.Select(h => Vector3.Angle(transform.forward, h.transform.forward)).Average();

        if (Mathf.Abs(currentAngle - previousAngle) > AngleAccuracy ||
            Mathf.Abs(overlap - previousOverlap) > OverlapAccuracy)
        {
            Debug.Log(
                $"Angle changed from {previousAngle:F1} to {currentAngle:F1}, overlap from {previousOverlap:F1} to {overlap:F1}.");
            OnRaycastUpdate?.Invoke(currentAngle, overlap);
            previousOverlap = overlap;
        }
    }
}