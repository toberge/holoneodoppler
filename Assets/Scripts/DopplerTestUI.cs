using DopplerSim;
using DopplerSim.Tools;
using UnityEngine;
using Random = System.Random;


public class DopplerTestUI : MonoBehaviour
{
    [SerializeField] private DopplerVisualiser _dopplerVisualiser;
    [SerializeField] private RaycastAngle _raycastAngle;
    private DepthWindow _depthWindow;
    private Random rand;

    // TODO slider or sth for velocity? yeeesus

    void Start()
    {
        if (_dopplerVisualiser == null || _raycastAngle == null)
        {
            Debug.LogError("Values are not set up correctly on DopplerUI");
            this.enabled = false;
        }

        _depthWindow = _raycastAngle.GetComponent<DepthWindow>();

        rand = new Random();
        SetRandomBloodVelocityWithinRange();

        _raycastAngle.valueUpdate += AngleUpdate;
    }

    public void SetRandomBloodVelocityWithinRange()
    {
        var r = Mathf.Abs((float)rand.NextGaussian(mu: 0.35, sigma: 0.15));
        var lerpedRandomBloodVelocity = Mathf.Lerp(0, _dopplerVisualiser.MaxArterialVelocity, (float)r);
        Debug.Log($"r: {r}, lerped: {lerpedRandomBloodVelocity}");
        _dopplerVisualiser.ArterialVelocity = lerpedRandomBloodVelocity;
        _dopplerVisualiser.UpdateDoppler();
    }

    private void AngleUpdate(int newAngle, float overlap)
    {
        Debug.Log("Updating angle or overlap : " + overlap);
        _dopplerVisualiser.Angle = _raycastAngle.currentAngle;
        _dopplerVisualiser.Overlap = overlap;
        _dopplerVisualiser.UpdateDoppler();
    }

    private void OnDestroy()
    {
        _raycastAngle.valueUpdate -= AngleUpdate;
    }
}
