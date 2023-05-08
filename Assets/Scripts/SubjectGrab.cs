using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class SubjectGrab : MonoBehaviour, IMixedRealityPointerHandler
{
    private Transform originalParent;
    private bool isGrabbable = false;
    
    private void Start()
    {
        originalParent = transform.parent;
    }

    public void Toggle()
    {
        isGrabbable = !isGrabbable;
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (!isGrabbable || !(eventData.Pointer is SpherePointer pointer)) return;
        Debug.Log($"GRABBED SUBJECT FROM {transform.localPosition:F8}, {transform.localScale:F8}, {transform.localRotation:F8}");
        transform.parent = pointer.transform;
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (!(eventData.Pointer is SpherePointer)) return;
        transform.SetParent(originalParent, true);
        Debug.Log($"PLACED SUBJECT AT {transform.localPosition:F8}, {transform.localScale:F8}, {transform.localRotation:F8}");
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }
}
