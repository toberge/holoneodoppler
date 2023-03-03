using System;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class ProbeHandle : MonoBehaviour, IMixedRealityPointerHandler
{
    [SerializeField] private Transform skull;
    private Transform originalParent;

    private void Start()
    {
        originalParent = transform.parent;
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer is SpherePointer pointer)
        {
            transform.parent = pointer.transform;
        }
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer is SpherePointer)
        {
            // Set parent without affecting world position
            transform.SetParent(originalParent, true);
        }
    }

    private void FixedUpdate()
    {
        // Always look to the center of the skull!
        transform.rotation = Quaternion.LookRotation(transform.position - skull.position);
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData) {}
    public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
}
