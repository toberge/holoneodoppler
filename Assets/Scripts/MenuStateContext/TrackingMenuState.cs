using System.Collections.Generic;
using System.Linq;
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

    private const string ActiveTargetsTitle = "<b>Status: </b>";

    private readonly Dictionary<string, string> targetStatuses = new Dictionary<string, string>();

    private Coroutine simulationRoutine;

    private void Start()
    {
        gameObjectMenu.SetActive(false);
    }

    private void OnEnable()
    {
        VuforiaApplication.Instance.OnVuforiaStarted += OnVuforiaStarted;
    }

    private void OnVuforiaStarted()
    {
        UpdateText();
    }

    private void UpdateText()
    {
        var targetsStatusInfo = GetTargetsStatusInfo();
        var completeInfo = ActiveTargetsTitle;

        if (targetsStatusInfo.Length > 0)
            completeInfo += $"\n{targetsStatusInfo}";

        statusText.text = completeInfo;
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

        if (targetStatuses.ContainsKey(targetName))
            targetStatuses[targetName] = status;
        else
            targetStatuses.Add(targetName, status);

        UpdateText();
    }

    private string GetStatusString(TargetStatus targetStatus)
    {
        return $"{targetStatus.Status} -- {targetStatus.StatusInfo}";
    }

    private string GetTargetsStatusInfo()
    {
        return targetStatuses.Aggregate("", (current, targetStatus) => current + $"\n{targetStatus.Key}: {targetStatus.Value}");
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
        if (simulationRoutine != null)
        {
            StopCoroutine(simulationRoutine);
            simulationRoutine = null;
        }
    }

    private void ImageTracked(Image im)
    {
        im.enabled = true;
        Context.myAudioSource.PlayOneShot(Context.clipTrackingSuccess);
        if (IsTrackingFinished())
        {
            statusText.text = "Both images are tracked. Remember to keep the images in view for continuous tracking.";
            Context.nextButton.gameObject.SetActive(true);
        }
    }

    private bool IsTrackingFinished()
    {
        if (SceneManager.GetActiveScene().name == "HoloUmoja")
        {
            return astronautCheckmark.enabled && droneCheckmark.enabled;
        }

        return droneCheckmark.enabled;
    }

    private void OnDisable()
    {
        if (simulationRoutine != null)
        {
            StopCoroutine(simulationRoutine);
            simulationRoutine = null;
        }

        VuforiaApplication.Instance.OnVuforiaStarted -= OnVuforiaStarted;
    }
}