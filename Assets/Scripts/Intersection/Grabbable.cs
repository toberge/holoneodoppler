using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class Grabbable : MonoBehaviour, IMixedRealityPointerHandler
{
    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer is SpherePointer)
        {
            Debug.Log($"Grab start from {eventData.Pointer.PointerName}");
            transform.parent = ((SpherePointer)(eventData.Pointer)).transform;
        }
        if (eventData.Pointer is PokePointer)
        {
            Debug.Log($"Touch start from {eventData.Pointer.PointerName}");
        }
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        transform.parent = null;
    }
    
    public void OnPointerClicked(MixedRealityPointerEventData eventData) {}
    public void OnPointerDragged(MixedRealityPointerEventData eventData) {}
}
