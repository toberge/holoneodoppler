using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vuforia;
using Image = UnityEngine.UI.Image;
using Text = UnityEngine.UI.Text;

public class TrackingMenuState : MenuState
{
    [SerializeField] private Image astronautCheckmark;
    [SerializeField] private Image droneCheckmark;

    [SerializeField] private Text statusText;

    public delegate void OnVuforiaStateChange();

    public OnVuforiaStateChange stateChange;
    private const string ACTIVE_TARGETS_TITLE = "<b>Status: </b>";
    string mTargetStatusInfo;
    string mVuMarkTrackableStateInfo;

    readonly Dictionary<string, string> mTargetsStatus = new Dictionary<string, string>();

    private Coroutine _simulationRoutine;

    void Start()
    {
        gameObjectMenu.SetActive(false);
    }

    private void OnEnable()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
    }

    void OnVuforiaStarted()
    {
        UpdateText();
    }

    void UpdateText()
    {
        UpdateInfo();

        var completeInfo = ACTIVE_TARGETS_TITLE;

        if (mTargetStatusInfo.Length > 0)
            completeInfo += $"\n{mTargetStatusInfo}";

        statusText.text = completeInfo;
    }

    void UpdateInfo()
    {
        mTargetStatusInfo = GetTargetsStatusInfo();
    }

    /// <summary>
    /// Public method to be called by an EventHandler's Lost/Found Events
    /// </summary>
    /// <param name="observerBehaviour"></param>
    public void TargetStatusChanged(ObserverBehaviour observerBehaviour)
    {
        var status = GetStatusString(observerBehaviour.TargetStatus);
        var targetName = observerBehaviour.TargetName;
        if (observerBehaviour.TargetStatus.Status == Status.TRACKED)
        {
            if (!astronautCheckmark.enabled && targetName.Contains("Astronaut"))
            {
                ImageTracked(astronautCheckmark);
            }
            else if (!droneCheckmark.enabled && targetName.Contains("Drone"))
            {
                ImageTracked(droneCheckmark);
            }
        }

        if (mTargetsStatus.ContainsKey(targetName))
            mTargetsStatus[targetName] = status;
        else
            mTargetsStatus.Add(targetName, status);

        UpdateText();
    }

    string GetStatusString(TargetStatus targetStatus)
    {
        return $"{targetStatus.Status} -- {targetStatus.StatusInfo}";
    }

    string GetTargetsStatusInfo()
    {
        var targetsAsMultiLineString = "";

        foreach (var targetStatus in mTargetsStatus)
            targetsAsMultiLineString += "\n" + targetStatus.Key + ": " + targetStatus.Value;

        return targetsAsMultiLineString;
    }

    private void Reset()
    {
        astronautCheckmark.enabled = false;
        droneCheckmark.enabled = false;
        statusText.text = "No images are tracked yet. Try to slowly get them closer/further to/from your eyes.";
    }

    public override MenuType GetMenuType() => MenuType.Tracking;

    public override void Show()
    {
        if (gameObjectMenu.activeSelf)
        {
            Debug.LogWarning(GetMenuType() +
                             " was already the visibility set to: true. Are you sure you were supposed to change it?");
        }

        Reset();

        gameObjectMenu.SetActive(true);
        Context.nextButton.gameObject.SetActive(false);

        // Stop both hands in case some of them are still showing
        Context.interactionHint.StopHand();
        Context.interactionHint.StopHand(false);
    }

    public override void Hide()
    {
        gameObjectMenu.SetActive(false);
        if (_simulationRoutine != null)
        {
            StopCoroutine(_simulationRoutine);
            _simulationRoutine = null;
        }
    }

    public void ImageTracked(Image im)
    {
        im.enabled = true;
        Context.myAudioSource.PlayOneShot(Context.clipTrackingSuccess);
        if (IsTrackingFinished())
        {
            statusText.text = "Both images are tracked. Remember to keep the images in view for continuous tracking.";
            Context.nextButton.gameObject.SetActive(true);
        }
    }

    public bool IsTrackingFinished()
    {
        if (SceneManager.GetActiveScene().name == "HoloUmoja")
        {
            return astronautCheckmark.enabled && droneCheckmark.enabled;
        }

        return droneCheckmark.enabled;
    }

    private void OnDisable()
    {
        if (_simulationRoutine != null)
        {
            StopCoroutine(_simulationRoutine);
            _simulationRoutine = null;
        }

        VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
    }
}