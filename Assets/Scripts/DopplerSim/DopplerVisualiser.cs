using System.Threading.Tasks;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DopplerSim
{
    [RequireComponent(typeof(RawImage))]
    public class DopplerVisualiser : MonoBehaviour
    {
        public delegate void DopplerEvent();

        public DopplerEvent OnDopplerUpdate;

        [SerializeField] private RectTransform labelTemplateY;
        [SerializeField] private RectTransform tickTemplateY;
        [SerializeField] private RectTransform tickTemplateX;
        [SerializeField] private RectTransform xAxis;
        [SerializeField] private RectTransform loadingLine;

        // "Max PRF: 22\tMax Velocity: ??"
        [SerializeField] private Text maxValues;

        private const float DisplayedFrequencyFactor = 1000;

        public float MaxVelocity => simulator.MaxVelocity;
        public float MaxPRF => simulator.MaxPRF / DisplayedFrequencyFactor;
        public float MinPRF => simulator.MinPRF / DisplayedFrequencyFactor;
        public float MaxArterialVelocity = 3.0f * 37f; // TODO this used the old true-to-visualized conversion of * 37

        public float Angle
        {
            get => simulator.Angle;
            set => simulator.Angle = value;
        }

        public float ArterialVelocity
        {
            get => simulator.ArterialVelocity;
            set => simulator.ArterialVelocity = value;
        }

        public float PulseRepetitionFrequency
        {
            get => simulator.PulseRepetitionFrequency / DisplayedFrequencyFactor;
            set => simulator.PulseRepetitionFrequency = value * DisplayedFrequencyFactor;
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
            UpdateDisplayedValues();
        }

        void Start()
        {
            currentCoroutine = StartCoroutine(UpdateDopplerGraphRoutine());
        }

        private void UpdateDisplayedValues()
        {
            // TODO split into X different text boxes and scrutinize necessity
            string velocityColour = simulator.IsVelocityOverMax ? "red" : "green";
            const string separator = "                      ";
            maxValues.text = $"PRF: {PulseRepetitionFrequency:F0} kHz{separator}" +
                             $"Angle: {Angle:F1}°{separator}" +
                             $"Max Velocity: <color={velocityColour}>{MaxVelocity:F1}</color> cm/s";
        }

        private void CreateAxis()
        {
            const float gapY = 10f;
            const int timeStepX = 30;
            // Canvas should be outside the grid container
            // TODO perhaps move this component to the outermost point and assign RawImage as prop?
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
                // TODO adjust tick texts when PRF changes, since this affects Nyquist velocity
                labelY.GetComponent<Text>().text = (2 * tick * simulator.NyquistVelocity).ToString("N2");
            }

            // TODO make the X axis correct as well, when you know the ticks (if this is possible at all)
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
            UpdateDisplayedValues();
            OnDopplerUpdate?.Invoke();
        }

        private IEnumerator UpdateDopplerGraphRoutine()
        {
            loadingLine.gameObject.SetActive(true);
            Debug.Log("Overlap in doppler " + Overlap);
            while (true)
            {
                var startTime = Time.time;
                // Delegate generation to thread
                var task = Task.Factory.StartNew(simulator.GenerateNextSlice);
                yield return new WaitUntil(() => task.IsCompleted);
                Debug.Log($"Spent {Time.time - startTime:F2} seconds generating slice");
                simulator.AssignSlice(task.Result);
                loadingLine.anchoredPosition =
                    new Vector2(simulator.linePosition * rawImage.rectTransform.sizeDelta.x, 0);
                // TODO add WaitForSecondsRealtime with leftover time when you know time slice
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