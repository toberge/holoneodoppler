using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BezierPoint : MonoBehaviour
{
    public Color color = Color.white;

    private void OnDrawGizmos()
    {
        Gizmos.color = color;
        float radius = transform.parent?.parent?.parent ? 0.005f * transform.parent.parent.parent.localScale.x : 0.005f;
        Gizmos.DrawSphere(transform.position, radius);
    }
}
