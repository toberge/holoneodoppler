using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public Transform points;
    public Transform segments;
    public GameObject template;
    private float delta = 0.01f;
    private float gizmosDelta = 0.1f;
    private int controlPointsCount = -1;

    void Start()
    {
        controlPointsCount = points.childCount;
        // Instantiate blood flow cylinder hitboxes
        InstantiatePoints(points.GetChild(0).position, points.GetChild(1).position, points.GetChild(2).position, points.GetChild(3).position);
        for (int i = 3; i < controlPointsCount - 2; i += 2)
        {
            Vector3 p0 = points.GetChild(i).position;
            // Force the first control point in subsequent curves to be a mirror of previous p2.
            Vector3 prevP2 = points.GetChild(i - 1).position;
            Vector3 p1 = prevP2 + (p0 - prevP2) * 2;
            InstantiatePoints(p0, p1, points.GetChild(i + 1).position, points.GetChild(i + 2).position);
        }
    }

    void InstantiatePoints(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        for (float t = delta / 2.0f; t < 1.0f; t += delta)
        {
            float nt = 1 - t;
            Vector3 point = (nt * nt * nt * p0) + (3 * nt * nt * t * p1) + (3 * nt * t * t * p2) + (t * t * t * p3);
            Vector3 direction = ((3 * nt * nt * (p1 - p0)) + (6 * nt * t * (p2 - p1)) + (3 * t * t * (p3 - p2))).normalized;
            // Use value as position and first derivative as direction.
            Instantiate(template, point, Quaternion.FromToRotation(template.transform.forward, direction), segments);
        }
    }

    void OnDrawGizmos()
    {
        // Ugly thing to make sure we don't mess up here :))))
        controlPointsCount = points.childCount;

        Gizmos.color = Color.blue;
        DrawCurve(points.GetChild(0).position, points.GetChild(1).position, points.GetChild(2).position, points.GetChild(3).position);
        for (int i = 3; i < controlPointsCount - 2; i += 2)
        {
            Vector3 p0 = points.GetChild(i).position;
            // Force the first control point in subsequent curves to be a mirror of previous p2.
            Vector3 prevP2 = points.GetChild(i - 1).position;
            Vector3 p1 = prevP2 + (p0 - prevP2) * 2;
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(p1, 0.01f);
            Gizmos.color = Color.blue;
            DrawCurve(p0, p1, points.GetChild(i + 1).position, points.GetChild(i + 2).position);
        }
    }

    void DrawCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        Vector3 previousPoint = p0;
        for (float t = gizmosDelta; t < 1.0f; t += gizmosDelta)
        {
            float nt = 1 - t;
            Vector3 point = (nt * nt * nt * p0) + (3 * nt * nt * t * p1) + (3 * nt * t * t * p2) + (t * t * t * p3);
            Gizmos.DrawLine(previousPoint, point);
            previousPoint = point;
        }
        Gizmos.DrawLine(previousPoint, p3);
    }
}
