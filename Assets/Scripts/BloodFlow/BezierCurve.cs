using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public Transform points;
    public Transform segments;
    public GameObject template;

    [SerializeField] private float scale = 1.0f;

    [SerializeField] private float segmentsPerUnit = 400f;

    [SerializeField] private bool reverseDirection = true;

    private const float GizmosDelta = 0.1f;
    private int controlPointsCount = -1;

    void Start()
    {
        controlPointsCount = points.childCount;
        // Use distance between start and end points as estimate of arc length,
        // and determine the resolution of the instantiation from there.
        // This estimate should work since the veins we model are mostly straight.
        float delta =
            1f / (segmentsPerUnit * Vector3.Distance(points.GetChild(0).position, points.GetChild(3).position));
        // Instantiate blood flow cylinder hitboxes
        InstantiatePoints(points.GetChild(0).position, points.GetChild(1).position, points.GetChild(2).position,
            points.GetChild(3).position, delta);
        for (int i = 3; i < controlPointsCount - 2; i += 2)
        {
            Vector3 p0 = points.GetChild(i).position;
            // Force the first control point in subsequent curves to be a mirror of previous p2.
            Vector3 prevP2 = points.GetChild(i - 1).position;
            Vector3 p1 = prevP2 + (p0 - prevP2) * 2;
            Vector3 p3 = points.GetChild(i + 2).position;
            delta = 1f / (segmentsPerUnit * Vector3.Distance(p0, p3));
            InstantiatePoints(p0, p1, points.GetChild(i + 1).position, p3, delta);
        }
    }

    void InstantiatePoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float delta)
    {
        for (float t = delta / 2.0f; t < 1.0f; t += delta)
        {
            float nt = 1 - t;
            Vector3 point = (nt * nt * nt * p0) + (3 * nt * nt * t * p1) + (3 * nt * t * t * p2) + (t * t * t * p3);
            Vector3 direction = ((3 * nt * nt * (p1 - p0)) + (6 * nt * t * (p2 - p1)) + (3 * t * t * (p3 - p2)))
                .normalized;
            if (reverseDirection)
            {
                direction = -direction;
            }
            // Use value as position and first derivative as direction.
            var instance = Instantiate(template, point,
                Quaternion.FromToRotation(template.transform.forward, direction), segments);
            instance.transform.localScale *= scale;
        }
    }

    void OnDrawGizmos()
    {
        // Ugly thing to make sure we don't mess up here :))))
        controlPointsCount = points.childCount;

        float radius = transform.parent ? 0.005f * transform.parent.localScale.x : 0.005f;

        Gizmos.color = Color.blue;
        DrawCurve(points.GetChild(0).position, points.GetChild(1).position, points.GetChild(2).position,
            points.GetChild(3).position);
        for (int i = 3; i < controlPointsCount - 2; i += 2)
        {
            Vector3 p0 = points.GetChild(i).position;
            // Force the first control point in subsequent curves to be a mirror of previous p2.
            Vector3 prevP2 = points.GetChild(i - 1).position;
            Vector3 p1 = prevP2 + (p0 - prevP2) * 2;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(p1, radius);
            Gizmos.color = Color.blue;
            DrawCurve(p0, p1, points.GetChild(i + 1).position, points.GetChild(i + 2).position);
        }
    }

    void DrawCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 previousPoint = p0;
        for (float t = GizmosDelta; t < 1.0f; t += GizmosDelta)
        {
            float nt = 1 - t;
            Vector3 point = (nt * nt * nt * p0) + (3 * nt * nt * t * p1) + (3 * nt * t * t * p2) + (t * t * t * p3);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }

        Gizmos.DrawLine(previousPoint, p3);
    }
}