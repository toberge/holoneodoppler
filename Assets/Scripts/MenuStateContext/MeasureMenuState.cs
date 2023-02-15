using UnityEngine;

public class MeasureMenuState : MenuState
{
    [SerializeField] private Transform probeCoachPosition;
    [SerializeField] private RaycastAngle raycastAngle;
    public override MenuType GetMenuType() => MenuType.Measure;

    private void IntersectedForTheFirstTime()
    {
        Context.myAudioSource.PlayOneShot(Context.clipTrackingSuccess);
        Context.interactionHint.StopProbe();
        raycastAngle.OnIntersection -= IntersectedForTheFirstTime;
    }

    public override void Show()
    {
        gameObjectMenu.SetActive(true);
        Context.interactionHint.ShowProbe(probeCoachPosition);
        Context.slidersStateController.SetMeasureState();
        raycastAngle.OnIntersection += IntersectedForTheFirstTime;
    }

    public override void Hide()
    {
        raycastAngle.OnIntersection -= IntersectedForTheFirstTime;
        Context.slidersStateController.HideAll();
        gameObjectMenu.SetActive(false);
        Context.interactionHint.StopProbe();
    }

    private void OnDisable()
    {
        raycastAngle.OnIntersection -= IntersectedForTheFirstTime;
        Context.interactionHint.StopProbe();
    }
}