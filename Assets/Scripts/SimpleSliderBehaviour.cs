using System;
using UnityEngine;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class SimpleSliderBehaviour : MonoBehaviour
{
    public delegate void SliderEvent();

    public SliderEvent OnValueUpdate;
    [SerializeField] private Vector2 minMaxValue = Vector2.up;
    [SerializeField] private string floatAccuracy = "F0";

    [FormerlySerializedAs("_currentValue")] [SerializeField]
    private Text currentValueText;

    [FormerlySerializedAs("_minValue")] [SerializeField]
    private Text minValueText;

    [FormerlySerializedAs("_maxValue")] [SerializeField]
    private Text maxValueText;

    private PinchSlider pinchSlider;

    public float MaxValue
    {
        get => minMaxValue.y;
        set
        {
            if (Math.Abs(value - minMaxValue.y) < 0.01f)
                return;
            minMaxValue.y = value;
            maxValueText.text = value.ToString(floatAccuracy);
        }
    }

    public float MinValue
    {
        get => minMaxValue.x;
        set
        {
            if (Math.Abs(value - minMaxValue.x) < 0.01f)
                return;
            minMaxValue.x = value;
            minValueText.text = value.ToString(floatAccuracy);
        }
    }

    private float currentValue;

    /// <summary>
    /// Interpolated value between min and max
    /// </summary>
    public float CurrentValue
    {
        get => currentValue;

        // This sets the current value in turn due to the OnValueUpdated event.
        set => CurrentRawValue = Mathf.InverseLerp(MinValue, MaxValue, value);
    }

    /// <summary>
    /// Non-interpolated current slider values
    /// </summary>
    public float CurrentRawValue
    {
        get => pinchSlider.SliderValue;
        set => pinchSlider.SliderValue = value;
    }

    // Start is called before the first frame update
    void Awake()
    {
        Debug.Assert(currentValueText != null,
            "CurrentValue textMesh is not set up in SimpleSliderBehaviour on " + gameObject.name);
        Debug.Assert(minValueText != null,
            "MinValue textMesh is not set up in SimpleSliderBehaviour on " + gameObject.name);
        Debug.Assert(maxValueText != null,
            "MaxValue textMesh is not set up in SimpleSliderBehaviour on " + gameObject.name);

        pinchSlider = GetComponentInParent<PinchSlider>();
        if (pinchSlider == null)
        {
            throw new MissingComponentException($"Parent of {gameObject.name} is missing PinchSlider component");
        }

        ChangeMinMaxValueText(minMaxValue.x, minMaxValue.y);
        pinchSlider.OnValueUpdated.AddListener(OnSliderChange);
        pinchSlider.OnInteractionEnded.AddListener(OnInteractionEnded);
    }

    private void OnDestroy()
    {
        pinchSlider.OnValueUpdated.RemoveListener(OnSliderChange);
        pinchSlider.OnInteractionEnded.RemoveListener(OnInteractionEnded);
    }

    private void OnSliderChange(SliderEventData data)
    {
        float newValue = Mathf.Lerp(
            minMaxValue.x, minMaxValue.y, data.NewValue);
        currentValue = newValue;
        currentValueText.text = $"{newValue.ToString(floatAccuracy)}";
    }

    private void OnInteractionEnded(SliderEventData data)
    {
        OnValueUpdate?.Invoke();
    }

    private void ChangeMinMaxValueText(float minValue, float maxValue)
    {
        minValueText.text = minValue.ToString(floatAccuracy);
        maxValueText.text = maxValue.ToString(floatAccuracy);
    }
}