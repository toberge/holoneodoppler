using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace DopplerSim
{
    internal struct Probe
    {
        public float angle;
        public float overlap;
        public Vector3 position;
        public Vector3 rotation;
    }

    [RequireComponent(typeof(RawImage))]
    public class DopplerVisualiser : MonoBehaviour
    {
        [SerializeField] private RectTransform labelTemplateY;
        [SerializeField] private RectTransform tickTemplateY;
        [SerializeField] private RectTransform tickTemplateX;
        [SerializeField] private RectTransform xAxis;
        [SerializeField] private RectTransform loadingLine;

        // "Max PRF: 22\tMax Velocity: ??"
        [SerializeField] private Text maxValues;

        private List<Text> yLabels = new List<Text>();

        private const float TimePerTimeSlice = 0.1f;
        private const float DisplayedFrequencyFactor = 1000;

        public float MaxPRF => simulator.MaxPRF / DisplayedFrequencyFactor;
        public float MinPRF => simulator.MinPRF / DisplayedFrequencyFactor;
        public float DefaultPRF => (float)DopplerSimulator.DefaultPulseRepetitionFrequency / DisplayedFrequencyFactor;
        public float MaxArterialVelocity = 3.0f * 37f; // TODO this used the old true-to-visualized conversion of * 37

        public float Angle
        {
            get => simulator.Angle;
            set
            {
                simulator.Angle = value;
                UpdateDisplayedValues();
            }
        }

        public float PulseRepetitionFrequency
        {
            get => simulator.PulseRepetitionFrequency / DisplayedFrequencyFactor;
            set
            {
                simulator.PulseRepetitionFrequency = value * DisplayedFrequencyFactor;
                UpdateAxes();
                UpdateDisplayedValues();
            }
        }

        public float SamplingDepth
        {
            get => simulator.SamplingDepth;
            set => simulator.SamplingDepth = value;
        }

        public float Overlap
        {
            get => simulator.Overlap;
            set
            {
                simulator.Overlap = value;
                UpdateDisplayedValues();
            }
        }

        private RawImage rawImage;
        private readonly DopplerSimulator simulator = new DopplerSimulator();

        private Coroutine currentCoroutine;

        private void Awake()
        {
            rawImage = GetComponent<RawImage>();
            rawImage.texture = simulator.CreatePlot();
            rawImage.SetNativeSize();
            CreateAxes();
            UpdateDisplayedValues();
        }

        private void Start()
        {
            currentCoroutine = StartCoroutine(UpdateDopplerGraphRoutine());
        }

        public void SaveState(Transform probe)
        {
            var probeData = JsonUtility.ToJson(new Probe
                { angle = Angle, overlap = Overlap, position = probe.localPosition, rotation = probe.localRotation.eulerAngles });
            var spectrogram = simulator.SpectrogramToPNG();
            var filename = DateTime.Now.ToString("yyyyy-MM-dd_HH.mm.ss.fff");

            Debug.Log($"Storing state at {filename}");

            // Write JSON
            var writer = new StreamWriter($"{Application.persistentDataPath}/{filename}.json", true);
            writer.Write(probeData);
            writer.Close();

            // Write PNG
            File.WriteAllBytes($"{Application.persistentDataPath}/{filename}.png", spectrogram);
        }

        private void UpdateDisplayedValues()
        {
            var angleColour = Angle >= 90 ? "blue" : "orange";
            var overlapColour = Overlap > 0 ? "green" : "red";
            var overlapText = Overlap > 0 ? "Yes" : "No";
            const string separator = "                ";
            maxValues.text = $"PRF: {PulseRepetitionFrequency:F0} kHz{separator}" +
                             $"Beam-flow angle: <color={angleColour}>{Angle:F1}°</color>{separator}" +
                             $"Depth window overlap: <color={overlapColour}>{overlapText}</color>";
        }

        private void CreateAxes()
        {
            const float gapY = 10f;
            const int timeStepX = 30;
            // Canvas should be outside the grid container
            // TODO perhaps move this component to the outermost point and assign RawImage as prop?
            Transform parent = transform.parent.parent;

            for (int y = -5; y <= 5; y++)
            {
                RectTransform tickY = Instantiate(tickTemplateY, parent);
                tickY.anchoredPosition =
                    new Vector2(tickTemplateY.anchoredPosition.x, gapY * y + xAxis.anchoredPosition.y);
                tickY.gameObject.SetActive(true);

                if (y == 0)
                    continue;
                RectTransform labelY = Instantiate(labelTemplateY, parent);
                labelY.anchoredPosition = new Vector2(labelTemplateY.anchoredPosition.x,
                    gapY * y + xAxis.anchoredPosition.y);
                labelY.gameObject.SetActive(true);
                // Velocity value in cm/s (Nyquist velocity is in m/s I think)
                var text = labelY.GetComponent<Text>();
                text.text = (2 * y * simulator.NyquistVelocity).ToString("N2");
                yLabels.Add(text);
            }

            // TODO make the X axis correct as well, when you know the ticks (if this is possible at all)
            for (int x = 1; x < 7; x++)
            {
                RectTransform tickX = Instantiate(tickTemplateX, parent);
                tickX.anchoredPosition = new Vector2(tickTemplateX.anchoredPosition.x - timeStepX * x,
                    tickTemplateX.anchoredPosition.y);
                tickX.gameObject.SetActive(true);
            }
        }

        private void UpdateAxes()
        {
            for (var i = 0; i < yLabels.Count; i++)
            {
                var y = i - 5 + (i >= 5 ? 1 : 0);
                yLabels[i].text = (2 * y * simulator.NyquistVelocity).ToString("N2");
            }
        }

        private IEnumerator UpdateDopplerGraphRoutine()
        {
            while (true)
            {
                var startTime = Time.time;

                // Delegate generation to thread
                var task = Task.Factory.StartNew(simulator.GenerateNextSlice);
                yield return new WaitUntil(() => task.IsCompleted);
                simulator.AssignSlice(task.Result);
                loadingLine.anchoredPosition =
                    new Vector2(simulator.linePosition * rawImage.rectTransform.sizeDelta.x, 0);

                var elapsedTime = Time.time - startTime;
                // Wait out the remaining time slice
                yield return new WaitForSecondsRealtime(Mathf.Abs(TimePerTimeSlice - elapsedTime));
            }
        }

        private void OnDisable()
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
                currentCoroutine = null;
                loadingLine.gameObject.SetActive(false);
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