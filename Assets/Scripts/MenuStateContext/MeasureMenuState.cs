using UnityEngine;

public class MeasureMenuState : MenuState
{
    public override MenuType GetMenuType() => MenuType.Measure;

    public override void Show()
    {
        gameObjectMenu.SetActive(true);
        Context.slidersStateController.SetMeasureState();
    }

    public override void Hide()
    {
        Context.slidersStateController.HideAll();
        gameObjectMenu.SetActive(false);
    }
}