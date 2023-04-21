using System.Collections;
using UnityEngine;

internal enum UltrasoundColourState
{
    Close = 0,
    Correct = 1,
    Neutral = 2
}

public class UltrasoundVisualiser : MonoBehaviour
{
    [SerializeField] private Color correct;
    [SerializeField] private Color neutral;
    [SerializeField] private Color close;
    [SerializeField] private Vector2 textureSpeed = new Vector2(0.1f, 0.3f);
    [SerializeField] private float colourChangeSpeed = 0.2f;

    [SerializeField] private RaycastAngle raycastAngle;

    private Renderer meshRenderer;
    private const string NameId = "_EmissiveColor";
    private static readonly int EmissiveColor = Shader.PropertyToID(NameId);
    private Coroutine currentCoroutine;

    private UltrasoundColourState currentColorState = UltrasoundColourState.Neutral;

    void Start()
    {
        meshRenderer = GetComponent<Renderer>();
        meshRenderer.material.SetColor(EmissiveColor, neutral);

        raycastAngle.OnRaycastUpdate += OnRaycastUpdate;
    }

    private void OnNoIntersection()
    {
        StartChangingColour(neutral);
        currentColorState = UltrasoundColourState.Neutral;
    }

    private void OnCorrectAngleIntersect()
    {
        StartChangingColour(correct);
        currentColorState = UltrasoundColourState.Correct;
    }

    private void OnCloseAngleIntersect()
    {
        StartChangingColour(close);
        currentColorState = UltrasoundColourState.Close;
    }

    private void OnRaycastUpdate(float angle, float overlap)
    {
        var correctAngle = angle < 30 || angle > 150;
        if (overlap == 0 && currentColorState != UltrasoundColourState.Neutral)
        {
            OnNoIntersection();
        }
        else if ((int)currentColorState != (correctAngle ? 1 : 0))
        {
            if (correctAngle)
            {
                OnCorrectAngleIntersect();
            }
            else
            {
                OnCloseAngleIntersect();
            }
        }
    }

    private void Update()
    {
        if (currentColorState != UltrasoundColourState.Neutral)
        {
            meshRenderer.material.mainTextureOffset += (new Vector2(Time.deltaTime, Time.deltaTime) * textureSpeed);
        }
    }

    private void StartChangingColour(Color to)
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }

        currentCoroutine = StartCoroutine(ChangeColour(to));
    }

    private IEnumerator ChangeColour(Color to)
    {
        Color currentColour = meshRenderer.material.GetColor(EmissiveColor);
        float timer = 0;
        while (timer < colourChangeSpeed)
        {
            timer += Time.deltaTime;
            meshRenderer.material.SetColor(EmissiveColor, Color.Lerp(currentColour, to, timer / colourChangeSpeed));
            yield return null;
        }

        currentCoroutine = null;
    }
}