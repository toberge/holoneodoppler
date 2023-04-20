using System.Collections;
using UnityEngine;

enum UltrasoundColourState
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
    
    private Renderer mRenderer;
    private const string NameId = "_EmissiveColor";
    private static readonly int EmissiveColor = Shader.PropertyToID(NameId);
    private Coroutine currentCoroutine;

    private UltrasoundColourState currentColorState = UltrasoundColourState.Neutral;

    void Start()
    {
        mRenderer = GetComponent<Renderer>();
        mRenderer.material.SetColor(EmissiveColor, neutral);

        raycastAngle.OnRaycastUpdate += OnRaycastUpdate;
    }

    private UltrasoundColourState OnNoIntersection()
    {
        StartChangingColour(neutral);
        return UltrasoundColourState.Neutral;
    }

    private UltrasoundColourState OnCorrectAngleIntersect()
    {
        StartChangingColour(correct);
        return UltrasoundColourState.Correct;
    }

    private UltrasoundColourState OnCloseAngleIntersect()
    {
        StartChangingColour(close);
        return UltrasoundColourState.Close;
    }

    private void OnRaycastUpdate(float angle, float overlap)
    {
        var correctAngle = angle < 30 || angle > 150;
        if (overlap == 0 && currentColorState != UltrasoundColourState.Neutral)
        {
            currentColorState = OnNoIntersection();
        }
        else if ((int)currentColorState != (correctAngle ? 1 : 0))
        {
            currentColorState = correctAngle ? OnCorrectAngleIntersect() : OnCloseAngleIntersect();
        }
    }

    private void Update()
    {
        if (currentColorState != UltrasoundColourState.Neutral)
        {
            mRenderer.material.mainTextureOffset += (new Vector2(Time.deltaTime, Time.deltaTime) * textureSpeed);
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
        Color currentColour = mRenderer.material.GetColor(EmissiveColor);
        float timer = 0;
        while (timer < colourChangeSpeed)
        {
            timer += Time.deltaTime;
            mRenderer.material.SetColor(EmissiveColor, Color.Lerp(currentColour, to, timer / colourChangeSpeed));
            yield return null;
        }
        currentCoroutine = null;
    }
}
