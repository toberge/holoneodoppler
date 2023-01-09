using UnityEngine;

public class RaycastAngle : MonoBehaviour
{
    public delegate void OnRaycastAngle(int newAngle, float overlap);
    public OnRaycastAngle valueUpdate;
    [SerializeField] private UltrasoundVisualiser visualiser;
    [SerializeField] private GameObject angleTextObject;
    public float currentAngle { get; private set; }
    private int previousAngle;
    private float previousOverlap = Mathf.NegativeInfinity;
    private float overlapAccuracy = 0.1f;

    private bool _notifiedAboutNoIntersection = false;

    private DepthWindow depthWindow;
    private int layerMask;
    private int skullLayer;

    private void Start()
    {
        depthWindow = GetComponent<DepthWindow>();
        layerMask = LayerMask.GetMask(new string[] { "Artery", "Skull" });
        skullLayer = LayerMask.NameToLayer("Skull");
    }

    private void OnNoIntersect(bool drawRay = true)
    {
        if (drawRay)
        {
            Debug.DrawRay(transform.position, transform.forward * 1000, Color.white);
        }
        //SampleUtil.AssignStringToTextComponent(AngleTextObject ? AngleTextObject : gameObject, "Angle: ?");
        if (!_notifiedAboutNoIntersection)
        {
            visualiser.OnNoIntersect();
            _notifiedAboutNoIntersection = true;
            currentAngle = -1000;
        }
    }

    void FixedUpdate()
    {
        Vector3 topHit = Vector3.negativeInfinity, bottomHit = Vector3.negativeInfinity;

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            // Abort if we hit the skull
            if (hit.transform.gameObject.layer == skullLayer)
            {
                Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.red);
                OnNoIntersect(false);
                return;
            }

            topHit = hit.point;
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.yellow);
            // TODO it seems wrong to use transform.position here, check with Maria
            if (Physics.Raycast(transform.position + transform.forward, -transform.forward, out hit, Mathf.Infinity, layerMask))
            {
                bottomHit = hit.point;
                // Debug.DrawRay(bottomHit, transform.TransformDirection(Vector3.right), Color.green);
            }
            float overlap = depthWindow.CalculateOverlap(topHit, bottomHit);

            previousAngle = Mathf.RoundToInt(currentAngle);
            // Find angle between ray and blood flow.
            currentAngle = Vector3.Angle(transform.forward, hit.transform.forward);

            int currentAngleRounded = Mathf.RoundToInt(currentAngle);
            if (currentAngleRounded != previousAngle)
            {
                SampleUtil.AssignStringToTextComponent(angleTextObject ? angleTextObject : gameObject, "Angle:\n" + currentAngleRounded);
                valueUpdate?.Invoke(currentAngleRounded, overlap);
                Debug.Log("Notifying different overlap because of angle: " + overlap);
                previousOverlap = overlap;
                // If the probe is closely aligned to (or away from) the blood flow:
                visualiser.OnIntersecting(currentAngleRounded < 30 || currentAngleRounded > 150);
                _notifiedAboutNoIntersection = false;
            }
            else if (Mathf.Abs(overlap - previousOverlap) > overlapAccuracy)
            {
                Debug.Log("Notifying different overlap: " + overlap + " prev: " + previousOverlap);
                valueUpdate?.Invoke(currentAngleRounded, overlap);
                previousOverlap = overlap;
            }
            //Debug.Log("Did Hit " + hit.transform.name + ", angle:  " + angle + " cos: " + cosAngle + "acos: " + Mathf.Acos(cosAngle) * Mathf.Rad2Deg);

            // Hit back:
            //Debug.DrawRay(topHit, transform.TransformDirection(Vector3.right), Color.magenta);
        }
        else
        {
            OnNoIntersect();
        }

    }


}
