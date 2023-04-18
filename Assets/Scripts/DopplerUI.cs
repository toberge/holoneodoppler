using DopplerSim;
using UnityEngine;
using UnityEngine.Serialization;


public class DopplerUI : MonoBehaviour
{
    [SerializeField] private SimpleSliderBehaviour prfSlider;

    [FormerlySerializedAs("depthSlider")] [SerializeField]
    private SimpleSliderBehaviour depthCenterSlider;

    [FormerlySerializedAs("bloodVelocitySlider")] [SerializeField]
    private SimpleSliderBehaviour depthRangeSlider;

    [FormerlySerializedAs("_dopplerVisualiser")] [SerializeField]
    private DopplerVisualiser dopplerVisualiser;

    [FormerlySerializedAs("_raycastAngle")] [SerializeField]
    private RaycastAngle raycastAngle;

    private DepthWindow depthWindow;

    // Start is called before the first frame update
    void Start()
    {
        if (dopplerVisualiser == null || raycastAngle == null || depthRangeSlider == null || prfSlider == null ||
            depthCenterSlider == null)
        {
            Debug.LogError("Values are not set up correctly on DopplerUI");
            enabled = false;
        }

        depthWindow = raycastAngle.GetComponent<DepthWindow>();
        UpdateMinMaxValues();

        depthRangeSlider.OnValueUpdate += DepthRangeSliderUpdate;
        prfSlider.OnValueUpdate += PRFSliderUpdate;
        depthCenterSlider.OnValueUpdate += DepthCenterSliderUpdate;
        raycastAngle.OnRaycastUpdate += AngleUpdate;
    }

    private void UpdateMinMaxValues()
    {
        prfSlider.MinValue = dopplerVisualiser.MinPRF;
        prfSlider.MaxValue = dopplerVisualiser.MaxPRF;
        prfSlider.CurrentValue = dopplerVisualiser.PulseRepetitionFrequency;
        depthCenterSlider.MinValue = depthWindow.MinDepth;
        depthCenterSlider.MaxValue = depthWindow.MaxDepth;
        depthCenterSlider.CurrentValue = depthWindow.DefaultDepth;
        depthRangeSlider.MinValue = depthWindow.MinWindowSize;
        depthRangeSlider.MaxValue = depthWindow.MaxWindowSize;
        depthRangeSlider.CurrentValue = depthWindow.DefaultWindowSize;
    }

    private void DepthRangeSliderUpdate()
    {
        depthWindow.WindowSize = depthRangeSlider.CurrentValue;
    }

    private void DepthCenterSliderUpdate()
    {
        dopplerVisualiser.SamplingDepth = depthCenterSlider.CurrentValue;
        depthWindow.Depth = depthCenterSlider.CurrentValue;
    }

    private void PRFSliderUpdate()
    {
        dopplerVisualiser.PulseRepetitionFrequency = prfSlider.CurrentValue;
    }

    private void AngleUpdate(float angle, float overlap)
    {
        dopplerVisualiser.Angle = angle;
        dopplerVisualiser.Overlap = overlap;
    }

    private void OnDestroy()
    {
        depthRangeSlider.OnValueUpdate -= DepthRangeSliderUpdate;
        prfSlider.OnValueUpdate -= PRFSliderUpdate;
        depthCenterSlider.OnValueUpdate -= DepthCenterSliderUpdate;
        raycastAngle.OnRaycastUpdate -= AngleUpdate;
    }
}