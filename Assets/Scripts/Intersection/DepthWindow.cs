using UnityEngine;

public class DepthWindow : MonoBehaviour
{
    [SerializeField] private bool isShown = true;

    [SerializeField] private Transform top;
    public Vector3 Top => top.position;
    [SerializeField] private Transform bottom;
    public Vector3 Bottom => bottom.position;

    [SerializeField] private float minDepth = 0;
    [SerializeField] private float defaultDepth = 0.03f;
    [SerializeField] private float maxDepth = 0.05f;

    [SerializeField] private float minRange = 0.01f;
    [SerializeField] private float defaultRange = 0.04f;
    [SerializeField] private float maxRange = 0.06f;

    private const float ToDisplayedCm = 100;
    private const float FromDisplayedCm = 1f / 100f;

    public float MinDepth => minDepth * ToDisplayedCm;
    public float DefaultDepth => defaultDepth * ToDisplayedCm;
    public float MaxDepth => maxDepth * ToDisplayedCm;

    public float MinWindowSize => minRange * ToDisplayedCm;
    public float DefaultWindowSize => defaultRange * ToDisplayedCm;
    public float MaxWindowSize => maxRange * ToDisplayedCm;

    private float windowSize = 0.0056f * 2;

    public float WindowSize
    {
        get => windowSize * ToDisplayedCm;
        set
        {
            windowSize = value * FromDisplayedCm;
            UpdateWindowSize();
        }
    }

    private float depth = 0f;

    public float Depth
    {
        get => depth * ToDisplayedCm;
        set
        {
            depth = value * FromDisplayedCm;
            UpdateDepth();
        }
    }

    private Transform window;
    private Vector3 startDepth;

    private void Start()
    {
        window = bottom.parent;
        startDepth = window.localPosition;
        depth = defaultDepth;
        UpdateDepth();
        windowSize = defaultRange;
        UpdateWindowSize();
        OnEnable();
    }

    private void UpdateWindowSize()
    {
        top.localPosition = new Vector3(top.localPosition.x, top.localPosition.y, -windowSize / 2);
        bottom.localPosition = new Vector3(bottom.localPosition.x, bottom.localPosition.y, windowSize / 2);
    }

    private void UpdateDepth()
    {
        window.localPosition = startDepth + new Vector3(0, 0, depth);
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
        // For a gradual overlap, you could raycast from bottom.position and up to find the bottom of the hit artery.
        // In this case, we only need a boolean overlap.
        return IsInsideWindow(hit) ? 1 : 0;
    }
}