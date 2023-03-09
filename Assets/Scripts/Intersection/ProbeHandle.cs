using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class ProbeHandle : MonoBehaviour, IMixedRealityPointerHandler
{
    [SerializeField] private Transform skull;
    [SerializeField] private Transform probe;
    private Transform originalParent;
    private LayerMask skullMask;

    public delegate void GrabEvent();
    public GrabEvent OnGrab;

    private void Start()
    {
        skullMask = LayerMask.GetMask("SkullSnap");
        originalParent = transform.parent;
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (eventData.Pointer is SpherePointer pointer)
        {
            OnGrab?.Invoke();
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
        var direction = (skull.position - transform.position).normalized;
        // Probe should snap to skull
        var didHit = Physics.Raycast(transform.position, direction, out RaycastHit hit, 10000f, skullMask);
        Debug.Assert(didHit, $"Somehow did not hit skull from {transform.position} to {skull.position}");
        probe.position = hit.point - direction * 0.005f;
        // Probe should always look to the center of the skull!
        probe.rotation = Quaternion.LookRotation(-direction);
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }
}