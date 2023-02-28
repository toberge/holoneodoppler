using System;
using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class Grabbable : MonoBehaviour, IMixedRealityPointerHandler
{
    [SerializeField] private Transform skull;
    
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
            transform.parent = null;
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
