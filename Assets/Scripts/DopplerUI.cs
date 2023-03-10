using DopplerSim;
using UnityEngine;


public class DopplerUI : MonoBehaviour
{
    [SerializeField] private SimpleSliderBehaviour bloodVelocitySlider;
    [SerializeField] private SimpleSliderBehaviour prfSlider;
    [SerializeField] private SimpleSliderBehaviour depthSlider;

    [SerializeField] private DopplerVisualiser _dopplerVisualiser;
    [SerializeField] RaycastAngle _raycastAngle;

    private DepthWindow _depthWindow;

    // Start is called before the first frame update
    void Start()
    {
        if (_dopplerVisualiser == null || _raycastAngle == null || bloodVelocitySlider == null || prfSlider == null ||
            depthSlider == null)
        {
            Debug.LogError("Values are not set up correctly on DopplerUI");
            enabled = false;
        }

        _depthWindow = _raycastAngle.GetComponent<DepthWindow>();

        UpdateMinMaxValues();

        bloodVelocitySlider.OnValueUpdate += BloodVelocitySliderUpdate;
        prfSlider.OnValueUpdate += PRFSliderUpdate;
        depthSlider.OnValueUpdate += SamplingDepthSliderUpdate;
        _raycastAngle.OnRaycastUpdate += AngleUpdate;
    }

    private void UpdateMinMaxValues()
    {
        prfSlider.MinValue = _dopplerVisualiser.MinPRF;
        prfSlider.MaxValue = _dopplerVisualiser.MaxPRF;
        prfSlider.CurrentValue = _dopplerVisualiser.PulseRepetitionFrequency;
    }

    private void BloodVelocitySliderUpdate()
    {
        _dopplerVisualiser.ArterialVelocity = bloodVelocitySlider.CurrentValue;
        _dopplerVisualiser.UpdateDoppler();
    }

    private void SamplingDepthSliderUpdate()
    {
        // Depth is weird and needs to be 0.05 and 1.0 where around av_depth / 7.0D + 0.0125D + 0.05D
        // If av_depth is 3.0, so optimal depth is around 0.49107142857 is the optimum
        _dopplerVisualiser.SamplingDepth = Mathf.Clamp(depthSlider.CurrentRawValue, 0.05f, 1.0f);
        _dopplerVisualiser.Overlap = _depthWindow.Overlap;
        Debug.Log($"sample slider updated. Depth: {_depthWindow.DepthDebug} raw: {depthSlider.CurrentRawValue}");

        _dopplerVisualiser.UpdateDoppler();
    }

    private void PRFSliderUpdate()
    {
        _dopplerVisualiser.PulseRepetitionFrequency = prfSlider.CurrentValue;
        _dopplerVisualiser.UpdateDoppler();
    }

    private void AngleUpdate(float angle, float overlap)
    {
        _dopplerVisualiser.Angle = angle;
        _dopplerVisualiser.Overlap = overlap;
        _dopplerVisualiser.UpdateDoppler();
    }

    private void OnDestroy()
    {
        bloodVelocitySlider.OnValueUpdate -= BloodVelocitySliderUpdate;
        prfSlider.OnValueUpdate -= PRFSliderUpdate;
        depthSlider.OnValueUpdate -= SamplingDepthSliderUpdate;
        _raycastAngle.OnRaycastUpdate -= AngleUpdate;
    }
}