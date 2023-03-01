using UnityEditor;

namespace DopplerSim
{
    [CustomEditor(typeof(DopplerVisualiser))]
    public class DopplerVisualiserInspector : Editor
    {
        private DopplerVisualiser _dopplerVisualiser;
        private float _arterialVelocity = 10f;
        private float _pulseRepetitionFrequency = 13e3f;
        private float _angle = 45f;
        private float _samplingDepth = 0.5F;
        private float _minPrf = 7f;
        private float _maxPrf = 22f;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _dopplerVisualiser = (DopplerVisualiser)target;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Arterial Velocity");
            _arterialVelocity = EditorGUILayout.Slider(_arterialVelocity, 0, _dopplerVisualiser.MaxArterialVelocity);
            EditorGUILayout.LabelField("Pulse Repetition Frequency");
            _pulseRepetitionFrequency = EditorGUILayout.Slider(_pulseRepetitionFrequency, _minPrf, _maxPrf);
            EditorGUILayout.LabelField("Angle");
            _angle = EditorGUILayout.Slider(_angle, 0, 180);
            EditorGUILayout.LabelField("Sampling Depth");
            _samplingDepth = EditorGUILayout.Slider(_samplingDepth, 0.05f, 1f);


            if (EditorGUI.EndChangeCheck())
            {
                _dopplerVisualiser.ArterialVelocity = _arterialVelocity;
                _dopplerVisualiser.PulseRepetitionFrequency = _pulseRepetitionFrequency;
                _dopplerVisualiser.Angle = _angle;
                _dopplerVisualiser.SamplingDepth = _samplingDepth;

                _maxPrf = _dopplerVisualiser.MaxPRF;
                _minPrf = _dopplerVisualiser.MinPRF;
                _dopplerVisualiser.UpdateDoppler();
            }
        }
    }
}