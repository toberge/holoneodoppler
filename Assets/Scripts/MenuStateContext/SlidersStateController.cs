using UnityEngine;
using UnityEngine.Serialization;

public class SlidersStateController : MonoBehaviour
{
    [SerializeField]
    private SimpleSliderBehaviour prfSlider;
    [FormerlySerializedAs("depthSlider")] [SerializeField]
    private SimpleSliderBehaviour depthCenterSlider;
    [FormerlySerializedAs("bloodVelocitySlider")] [SerializeField]
    private SimpleSliderBehaviour depthRangeSlider;
    
    void Start()
    {
        ChangeVisibilityAll(false);
    }
    
    private void ChangeVisibilityAll(bool active)
    {
        depthRangeSlider.gameObject.SetActive(active);
        prfSlider.gameObject.SetActive(active);
        depthCenterSlider.gameObject.SetActive(active);
    }

    public void HideAll()
    {
        ChangeVisibilityAll(false);
    }

    public void SetMeasureState()
    {
        prfSlider.gameObject.SetActive(true);
        depthCenterSlider.gameObject.SetActive(true);
        depthRangeSlider.gameObject.SetActive(true);
    }
}
