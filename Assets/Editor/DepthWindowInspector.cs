using UnityEditor;
using UnityEngine;

namespace DopplerSim
{
    [CustomEditor(typeof(DepthWindow))]
    public class DepthWindowInspector : Editor
    {
        private DepthWindow _depthWindow;
        private float _depthDebug = -1;
        private float _windowSize = -1;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            _depthWindow = (DepthWindow)target;

            if (_depthDebug < 0)
            {
                _depthDebug = _depthWindow.DefaultDepth;
                _windowSize = _depthWindow.DefaultWindowSize;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Depth");
            _depthDebug = EditorGUILayout.Slider(_depthDebug, _depthWindow.MinDepth, _depthWindow.MaxDepth);

            EditorGUILayout.LabelField("Window Size");
            _windowSize = EditorGUILayout.Slider(_windowSize, _depthWindow.MinWindowSize, _depthWindow.MaxWindowSize);

            if (EditorGUI.EndChangeCheck())
            {
                _depthWindow.Depth = _depthDebug / 100f;
                _depthWindow.WindowSize = _windowSize / 100f;
            }
        }
    }
}