using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class SubjectGrab : MonoBehaviour, IMixedRealityPointerHandler
{
    private Transform originalParent;
    private bool isGrabbable = false;
    
    private NearInteractionGrabbable grabbable;
    private BoxCollider collider;
    
    private void Start()
    {
        originalParent = transform.parent;
        grabbable = GetComponent<NearInteractionGrabbable>();
        grabbable.enabled = isGrabbable;
        collider = GetComponent<BoxCollider>();
        collider.enabled = isGrabbable;
    }

    public void Toggle()
    {
        isGrabbable = !isGrabbable;
        grabbable.enabled = isGrabbable;
        collider.enabled = isGrabbable;
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (!isGrabbable || !(eventData.Pointer is SpherePointer pointer)) return;
        Debug.Log($"GRABBED SUBJECT FROM Vector3{transform.localPosition:F8}, Vector3{transform.localScale:F8}, Quaternion{transform.localRotation:F8}");
        transform.parent = pointer.transform;
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (!(eventData.Pointer is SpherePointer)) return;
        transform.SetParent(originalParent, true);
        Debug.Log($"PLACED SUBJECT AT Vector3{transform.localPosition:F8}, Vector3{transform.localScale:F8}, Quaternion{transform.localRotation:F8}");
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }
}
