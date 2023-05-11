using UnityEngine;

public class MeasureMenuState : MenuState
{
    public override MenuType GetMenuType() => MenuType.Measure;

    public override void Show()
    {
        gameObjectMenu.SetActive(true);
    }

    public override void Hide()
    {
        gameObjectMenu.SetActive(false);
    }
}