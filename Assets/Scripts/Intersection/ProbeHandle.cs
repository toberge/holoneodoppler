using Microsoft.MixedReality.Toolkit.Input;
using UnityEngine;

public class ProbeHandle : MonoBehaviour, IMixedRealityPointerHandler
{
    [SerializeField] private Transform centerOfSkull;
    [SerializeField] private Transform probe;
    private Transform originalParent;
    private LayerMask skullMask;
    private bool grabbed = false;
    private bool useFixedRotation = true;

    public delegate void GrabEvent();

    public GrabEvent OnGrab;

    private void Start()
    {
        skullMask = LayerMask.GetMask("SkullSnap");
        originalParent = transform.parent;
        probe.rotation = Quaternion.LookRotation(-DirectionToCenter());
    }

    public void ToggleFixedRotation()
    {
        useFixedRotation = !useFixedRotation;
    }

    private Vector3 DirectionToCenter()
    {
        return (centerOfSkull.position - transform.position).normalized;
    }

    public void OnPointerDown(MixedRealityPointerEventData eventData)
    {
        if (!(eventData.Pointer is SpherePointer pointer)) return;
        OnGrab?.Invoke();
        grabbed = true;
        // Prepare for moving the probe
        transform.rotation = probe.rotation;
        // Reparent to pointer
        transform.parent = pointer.transform;
    }

    public void OnPointerUp(MixedRealityPointerEventData eventData)
    {
        if (!(eventData.Pointer is SpherePointer)) return;
        grabbed = false;
        // Set parent without affecting world position
        transform.SetParent(originalParent, true);
        // Then reset to probe's position
        transform.position = probe.position - .02f * DirectionToCenter();
    }

    private void FixedUpdate()
    {
        var direction = DirectionToCenter();
        // Probe should snap to skull. Cast ray some distance away from the skull to avoid problems when the user moves the probe inside the head.
        var didHit = Physics.Raycast(transform.position - direction, direction, out var hit, 10000f, skullMask);
        Debug.Assert(didHit, $"Somehow did not hit skull from {transform.position} to {centerOfSkull.position}");
        probe.position = hit.point - direction * 0.005f;
        if (useFixedRotation)
        {
            // Probe should always look to the center of the skull!
            probe.rotation = Quaternion.LookRotation(-direction);
        }
        else if (grabbed)
        {
            probe.rotation = transform.rotation;
        }
    }

    public void OnPointerClicked(MixedRealityPointerEventData eventData)
    {
    }

    public void OnPointerDragged(MixedRealityPointerEventData eventData)
    {
    }
}