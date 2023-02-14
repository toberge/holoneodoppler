using System;
using System.Threading.Tasks;
using System.Collections;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DopplerSim
{
    [RequireComponent(typeof(RawImage))]
    public class DopplerVisualiser : MonoBehaviour
    {
        public const float ConvertFromTrueToVisualised = 37f;
        public const float ConvertFromVisualisedToTrue = 1 / ConvertFromTrueToVisualised;

        public delegate void OnDopplerVisualiser();

        public OnDopplerVisualiser OnDopplerUpdate;

        public bool ShowMaxValues = true;

        [SerializeField] private RectTransform labelTemplateY;
        [SerializeField] private RectTransform tickTemplateY;
        [SerializeField] private RectTransform tickTemplateX;
        [SerializeField] private RectTransform xAxis;
        [SerializeField] private RectTransform loadingLine;

        // "Max PRF: 22\tMax Velocity: ??"
        [SerializeField] private Text maxValues;

        public float MaxVelocity => simulator.MaxVelocity * ConvertFromTrueToVisualised;
        public float MaxPRF => simulator.MaxPRF;
        public float MaxArterialVelocity = 3.0f * ConvertFromTrueToVisualised;

        public float Angle
        {
            get => simulator.Angle;
            set => simulator.Angle = value;
        }

        public float ArterialVelocity
        {
            get => simulator.ArterialVelocity * ConvertFromTrueToVisualised;
            set => simulator.ArterialVelocity = value * ConvertFromVisualisedToTrue;
        }

        public float PulseRepetitionFrequency
        {
            get => simulator.PulseRepetitionFrequency;
            set => simulator.PulseRepetitionFrequency = value;
        }

        public float SamplingDepth
        {
            get => simulator.SamplingDepth;
            set => simulator.SamplingDepth = value;
        }

        public float Overlap
        {
            get => simulator.Overlap;
            set => simulator.Overlap = value;
        }

        private RawImage rawImage;
        private DopplerSimulator simulator;

        private Coroutine currentCoroutine;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            simulator = new DopplerSimulator();
            rawImage.texture = simulator.CreatePlot();
            rawImage.SetNativeSize();
            loadingLine.gameObject.SetActive(false);
            CreateAxis();
            UpdateMaxValues();
        }

        void Start()
        {
            currentCoroutine = StartCoroutine(UpdateDopplerGraphRoutine());
        }

        private void UpdateMaxValues()
        {
            if (ShowMaxValues)
            {
                string velocityColour = simulator.IsVelocityOverMax ? "red" : "green";
                var roundedMaxVelocity = Mathf.Round(MaxVelocity * 10) / 10;
                maxValues.text = $"Max PRF: {Mathf.RoundToInt(MaxPRF)} kHz                      " +
                                 $"Max Velocity: <color={velocityColour}>{roundedMaxVelocity}</color> cm/s";
            }
            else
            {
                maxValues.text = "";
            }
        }

        private void CreateAxis()
        {
            const float gapY = 10f;
            const int velocityStepY = 30;
            const int timeStepX = velocityStepY;
            // Canvas should be outside the grid container
            Transform parent = transform.parent.parent;

            for (int tick = -5; tick <= 5; tick++)
            {
                RectTransform tickY = Instantiate(tickTemplateY, parent);
                tickY.anchoredPosition =
                    new Vector2(tickTemplateY.anchoredPosition.x, gapY * tick + xAxis.anchoredPosition.y);
                tickY.gameObject.SetActive(true);

                if (tick == 0)
                    continue;
                RectTransform labelY = Instantiate(labelTemplateY, parent);
                labelY.anchoredPosition = new Vector2(labelTemplateY.anchoredPosition.x,
                    gapY * tick + xAxis.anchoredPosition.y);
                labelY.gameObject.SetActive(true);
                // Velocity value in cm/s (Nyquist velocity is in m/s I think)
                labelY.GetComponent<Text>().text = (2 * tick * DopplerSimulator.NyquistVelocity).ToString("N2");
            }

            // TODO make the X axis correct as well, when you know the ticks
            for (int tick = 1; tick < 7; tick++)
            {
                RectTransform tickX = Instantiate(tickTemplateX, parent);
                tickX.anchoredPosition = new Vector2(tickTemplateX.anchoredPosition.x - timeStepX * tick,
                    tickTemplateX.anchoredPosition.y);
                tickX.gameObject.SetActive(true);
            }
        }

        public void UpdateDoppler()
        {
            UpdateMaxValues();
            OnDopplerUpdate?.Invoke();
        }

        private IEnumerator UpdateDopplerGraphRoutine()
        {
            loadingLine.gameObject.SetActive(true);
            Debug.Log("Overlap in doppler " + Overlap);
            while (true)
            {
                // Delegate generation to thread (TODO safety)
                var task = Task.Factory.StartNew(simulator.GenerateNextSlice);
                yield return new WaitUntil(() => task.IsCompleted);
                simulator.AssignSlice(task.Result);
                loadingLine.anchoredPosition = new Vector2(simulator.linePosition , 0);
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }

        private void OnDisable()
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
            }
        }

        private void OnEnable()
        {
            if (currentCoroutine == null)
            {
                currentCoroutine = StartCoroutine(UpdateDopplerGraphRoutine());
            }
        }
    }
}