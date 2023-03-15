using DopplerSim;
using UnityEngine;


public class DopplerTestUI : MonoBehaviour
{
    [SerializeField] private DopplerVisualiser _dopplerVisualiser;
    [SerializeField] private RaycastAngle _raycastAngle;

    private void Start()
    {
        if (_dopplerVisualiser == null || _raycastAngle == null)
        {
            Debug.LogError("Values are not set up correctly on DopplerUI");
            enabled = false;
        }

        _raycastAngle.OnRaycastUpdate += AngleUpdate;
    }

    private void AngleUpdate(float angle, float overlap)
    {
        _dopplerVisualiser.Angle = angle;
        _dopplerVisualiser.Overlap = overlap;
    }

    private void OnDestroy()
    {
        _raycastAngle.OnRaycastUpdate -= AngleUpdate;
    }
}
