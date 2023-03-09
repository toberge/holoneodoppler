using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class ProbeGrab : MonoBehaviour, IMixedRealityPointerHandler
{
    [SerializeField] private ProbeHandle handle;

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        handle.OnPointerDown(eventData);
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        handle.OnPointerUp(eventData);
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }
}
