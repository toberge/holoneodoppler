﻿using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if WINDOWS_UWP
using Windows.Storage;
#endif

namespace DopplerSim
{
    struct Probe
    {
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

        public float MaxVelocity => simulator.MaxVelocity;
        public float MaxPRF => simulator.MaxPRF / DisplayedFrequencyFactor;
        public float MinPRF => simulator.MinPRF / DisplayedFrequencyFactor;
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

        public float ArterialVelocity
        {
            get => simulator.ArterialVelocity;
            set => simulator.ArterialVelocity = value;
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

#if WINDOWS_UWP
        private async Task ActuallySaveState(string filename, string probeData, byte[] spectrogram)
        {
            try
            {
                // TODO this didn't work:
                //var storageFolder = ApplicationData.Current.LocalFolder;
                var storageFolder = KnownFolders.CameraRoll;
                Debug.Log($"Writing to {storageFolder.Path}");
                var jsonFile =
                    await storageFolder.CreateFileAsync($"{filename}.json", CreationCollisionOption.ReplaceExisting);
                Debug.Log($"JSON file is {jsonFile.Path}");
                await FileIO.WriteTextAsync(jsonFile, probeData);
                Debug.Log($"Wrote probe state to {jsonFile.Path}");
                var pngFile =
                    await storageFolder.CreateFileAsync($"{filename}.png", CreationCollisionOption.ReplaceExisting);
                Debug.Log($"PNG file is {pngFile}");
                await FileIO.WriteBytesAsync(pngFile, spectrogram);
                Debug.Log($"Wrote spectrogram to {pngFile.Path}");
            }
            catch (Exception e)
            {
                Debug.LogWarning(e);
            }
        }
#endif

        public void SaveState(Transform probe)
        {
            var probeData = JsonUtility.ToJson(new Probe
                { position = probe.position, rotation = probe.rotation.eulerAngles });
            var spectrogram = simulator.SpectrogramToPNG();
            var filename = DateTime.Now.ToString("s");
            Debug.Log($"Storing state at {filename}");
            Debug.Log($"{probe.position:F6}, rotation: {probe.rotation.eulerAngles:F6}");
#if WINDOWS_UWP
            ActuallySaveState(filename, probeData, spectrogram);
#else
            var path = Path.Combine(Application.persistentDataPath, filename);
            File.WriteAllText($"{path}.json", probeData);
            Debug.Log($"Wrote probe state to {path}.json");
            File.WriteAllBytes($"{path}.png", spectrogram);
            Debug.Log($"Wrote spectrogram to {path}.png");
#endif
        }

        private void UpdateDisplayedValues()
        {
            // TODO split into X different text boxes and scrutinize necessity
            string angleColour = Angle >= 90 ? "blue" : "red";
            const string separator = "                      ";
            maxValues.text = $"PRF: {PulseRepetitionFrequency:F0} kHz{separator}" +
                             $"Beam-flow angle: <color={angleColour}>{Angle:F1}°</color>{separator}" +
                             $"Overlap: <color=yellow>{Overlap:F2}</color>";
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
            for (int i = 0; i < yLabels.Count; i++)
            {
                int y = i - 5 + (i >= 5 ? 1 : 0);
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
                // Debug.Log($"Spent {elapsedTime:F4} seconds generating slice");
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