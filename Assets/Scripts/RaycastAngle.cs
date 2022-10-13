using UnityEngine;

public class RaycastAngle : MonoBehaviour
{
    public delegate void OnRaycastAngle(int newAngle, float overlap);
    public OnRaycastAngle valueUpdate;
    [SerializeField] private UltrasoundVisualiser visualiser;
    [SerializeField] private GameObject AngleTextObject;
    public float CurrentAngle { get; private set; }
    private int previousAngle;
    private float previousOverlap = Mathf.NegativeInfinity;
    private float overlapAccuracy = 0.1f;

    private bool _notifiedAboutNoIntersection = false;

    private DepthWindow depthWindow;
    private int layerMask;

    private void Start()
    {
        depthWindow = GetComponent<DepthWindow>();
        layerMask = LayerMask.GetMask(new string[] { "Artery" });
    }

    void FixedUpdate()
    {
        Vector3 topHit = Vector3.negativeInfinity, bottomHit = Vector3.negativeInfinity;

        RaycastHit hit;
        // Does the ray intersect any objects excluding the player layer
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, layerMask))
        {
            topHit = hit.point;
            Debug.DrawRay(transform.position, transform.forward * hit.distance, Color.yellow);
            if (Physics.Raycast(transform.position + transform.forward, -transform.forward, out hit, Mathf.Infinity, layerMask))
            {

                bottomHit = hit.point;
                // Debug.DrawRay(bottomHit, transform.TransformDirection(Vector3.right), Color.green);
            }
            float overlap = depthWindow.CalculateOverlap(topHit, bottomHit);

            // Find angle between ray and blood flow.
            // TODO make use of this angle!
            //      or switch to angle = rawAngle > 90 ? 90 - (rawAngle - 90) : rawAngle
            //      to present these angles as negative (though it really is just negative doppler shift we're looking at)
            float rawAngle = Vector3.Angle(transform.forward, hit.transform.forward);
            previousAngle = Mathf.RoundToInt(CurrentAngle);
            // Transform angle from [0, 45, 90, 135, 180] to [0, 45, 90, 45, 0].
            CurrentAngle = (Mathf.Acos(Mathf.Abs(Mathf.Cos(rawAngle * Mathf.Deg2Rad))) * Mathf.Rad2Deg);

            int currentAngleRounded = Mathf.RoundToInt(CurrentAngle);
            if (currentAngleRounded != previousAngle)
            {
                SampleUtil.AssignStringToTextComponent(AngleTextObject ? AngleTextObject : gameObject, "Angle:\n" + currentAngleRounded + "\n" + Mathf.RoundToInt(rawAngle));
                valueUpdate?.Invoke(currentAngleRounded, overlap);
                Debug.Log("Notifying different overlap because of angle: " + overlap);
                previousOverlap = overlap;
                visualiser.OnIntersecting(-30 < currentAngleRounded && currentAngleRounded < 30);
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
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.forward) * 1000, Color.white);
            //SampleUtil.AssignStringToTextComponent(AngleTextObject ? AngleTextObject : gameObject, "Angle: ?");
            if (!_notifiedAboutNoIntersection)
            {
                visualiser.OnNoIntersect();
                _notifiedAboutNoIntersection = true;
                //Debug.Log("Notified " + _notifiedAboutNoIntersection);
                CurrentAngle = -1000;
            }
        }

    }


}
