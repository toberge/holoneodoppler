using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArterySegment : MonoBehaviour
{
    private CapsuleCollider capsule;

    void Start()
    {
        capsule = GetComponent<CapsuleCollider>();
    }

    void OnDrawGizmos()
    {
        //Gizmos.DrawMesh(capsule.transform.position, capsule.transform.rotation);
    }
}
