using UnityEngine;

public class AngleDisplay : MonoBehaviour
{
    [SerializeField] private GameObject angleTextObject;

    [SerializeField] private RaycastAngle raycastAngle;

    void Start()
    {
        raycastAngle.OnRaycastUpdate += OnAngleUpdate;
    }

    private void OnDestroy()
    {
        raycastAngle.OnRaycastUpdate -= OnAngleUpdate;
    }

    void OnAngleUpdate(float angle, float overlap)
    {
        SampleUtil.AssignStringToTextComponent(angleTextObject ? angleTextObject : gameObject,
            $"Angle:\n{angle:F0}");
    }
}