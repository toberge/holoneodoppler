using UnityEngine;

public class DepthWindow : MonoBehaviour
{
    [SerializeField] private bool isShown = true;

    [SerializeField] private Transform top;
    [SerializeField] private Transform bottom;

    [SerializeField] private float minDepth = 0;
    [SerializeField] private float defaultDepth = 0.03f;
    [SerializeField] private float maxDepth = 0.05f;

    [SerializeField] private float minRange = 0.01f;
    [SerializeField] private float defaultRange = 0.04f;
    [SerializeField] private float maxRange = 0.06f;

    private const float ToDisplayedCm = 100;

    public float MinDepth => minDepth * ToDisplayedCm;
    public float DefaultDepth => defaultDepth * ToDisplayedCm;
    public float MaxDepth => maxDepth * ToDisplayedCm;

    public float MinWindowSize => minRange * ToDisplayedCm;
    public float DefaultWindowSize => defaultRange * ToDisplayedCm;
    public float MaxWindowSize => maxRange * ToDisplayedCm;

    public float WindowSize
    {
        get => windowSize;
        set
        {
            top.localPosition = new Vector3(top.localPosition.x, top.localPosition.y, -value / 2);
            bottom.localPosition = new Vector3(bottom.localPosition.x, bottom.localPosition.y, value / 2);
            windowSize = value;
        }
    }

    public float Depth
    {
        get => depth;
        set
        {
            window.localPosition = startDepth + new Vector3(0, 0, value);
            depth = value;
        }
    }

    private float windowSize = 0.0056f * 2;

    private Transform window;
    private float depth = 0f;
    private Vector3 startDepth;

    void Start()
    {
        window = bottom.parent;
        startDepth = window.localPosition;
        Depth = defaultDepth;
        WindowSize = defaultRange;
        OnEnable();
    }

    private void OnEnable()
    {
        if (!isShown)
        {
            top.parent.gameObject.SetActive(false);
        }
    }

    private bool IsInsideWindow(Vector3 point)
    {
        // Inside if distances to both endpoints are within size
        return Vector3.Distance(point, top.position) <= windowSize &&
               Vector3.Distance(point, bottom.position) <= windowSize;
    }

    public float CalculateOverlap(Vector3 hit)
    {
        // TODO raycast from bottom.position and up to find bottomhit (if necessary)
        //      (and abort early if not top inside)
        return IsInsideWindow(hit) ? 1 : 0;
    }
}