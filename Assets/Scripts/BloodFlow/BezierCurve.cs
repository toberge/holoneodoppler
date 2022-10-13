using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierCurve : MonoBehaviour
{
    public GameObject[] points;
    public GameObject template;
    private float delta = 0.01f;
    private float gizmosDelta = 0.1f;

    void Start()
    {
        // Instantiate blood flow cylinder hitboxes
        InstantiatePoints();
    }

    void InstantiatePoints()
    {
        Vector3 p0 = points[0].transform.position;
        Vector3 p1 = points[1].transform.position;
        Vector3 p2 = points[2].transform.position;
        Vector3 p3 = points[3].transform.position;
        for (float t = delta / 2.0f; t < 1.0f; t += delta)
        {
            float nt = 1 - t;
            Vector3 point = (nt * nt * nt * p0) + (3 * nt * nt * t * p1) + (3 * nt * t * t * p2) + (t * t * t * p3);
            Vector3 direction = ((3 * nt * nt * (p1 - p0)) + (6 * nt * t * (p2 - p1)) + (3 * t * t * (p3 - p2))).normalized;
            Debug.Log("B(" + t + ") = " + point + ", B'(t) = " + direction);
            // Use value as position and first derivative as direction.
            Instantiate(template, point, Quaternion.FromToRotation(template.transform.forward, direction));
        }
    }

    void OnDrawGizmos()
    {
        Vector3 p0 = points[0].transform.position;
        Vector3 p1 = points[1].transform.position;
        Vector3 p2 = points[2].transform.position;
        Vector3 p3 = points[3].transform.position;
        Gizmos.color = Color.blue;
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
