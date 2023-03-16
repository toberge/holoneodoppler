using DopplerSim;
using UnityEngine;
using UnityEngine.Serialization;


public class DopplerTestUI : MonoBehaviour
{
    [FormerlySerializedAs("_dopplerVisualiser")] [SerializeField]
    private DopplerVisualiser dopplerVisualiser;

    [FormerlySerializedAs("_raycastAngle")] [SerializeField]
    private RaycastAngle raycastAngle;

    private void Start()
    {
        if (dopplerVisualiser == null || raycastAngle == null)
        {
            Debug.LogError("Values are not set up correctly on DopplerUI");
            enabled = false;
        }

        raycastAngle.OnRaycastUpdate += AngleUpdate;
    }

    private void AngleUpdate(float angle, float overlap)
    {
        dopplerVisualiser.Angle = angle;
        dopplerVisualiser.Overlap = overlap;
    }

    private void OnDestroy()
    {
        raycastAngle.OnRaycastUpdate -= AngleUpdate;
    }
}