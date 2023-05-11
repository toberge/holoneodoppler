using UnityEngine;
using UnityEngine.SceneManagement;

public class MeasureMenuState : MenuState
{
    [SerializeField] private Transform probeCoachPosition;
    [SerializeField] private Transform probe;
    [SerializeField] private ProbeHandle probeHandle;
    [SerializeField] private RaycastAngle raycastAngle;
    
    public override MenuType GetMenuType() => MenuType.Measure;

    private void IntersectedForTheFirstTime()
    {
        Context.myAudioSource.PlayOneShot(Context.clipTrackingSuccess);
        Context.interactionHint.StopProbe();
        raycastAngle.OnIntersection -= IntersectedForTheFirstTime;
    }

    private void OnFirstGrab()
    {
        Context.interactionHint.StopHand();
    }

    public override void Show()
    {
        gameObjectMenu.SetActive(true);
        raycastAngle.OnIntersection += IntersectedForTheFirstTime;
        if (SceneManager.GetActiveScene().name == "HoloUmoja")
        {
            // Show old probe.
            Context.interactionHint.ShowProbe(probeCoachPosition);
        }
        else
        {
            // Show hand grabbing probe that disappears when the probe is grabbed.
            Context.interactionHint.ShowHand(probe.position + new Vector3(0, .03f, .3f), "Move", true, probe.parent);
            probeHandle.OnGrab += OnFirstGrab;
        }
    }

    public override void Hide()
    {
        raycastAngle.OnIntersection -= IntersectedForTheFirstTime;
        gameObjectMenu.SetActive(false);
        Context.interactionHint.StopProbe();
        Context.interactionHint.StopHand();
    }
    
    private void OnDisable()
    {
        raycastAngle.OnIntersection -= IntersectedForTheFirstTime;
        Context.interactionHint.StopProbe();
        Context.interactionHint.StopHand();
    }
}