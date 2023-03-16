using UnityEngine;

public class BloodFlowMenuState : MenuState
{
    public override MenuType GetMenuType() => MenuType.BloodFlow;

    public override void Show()
    {
        gameObjectMenu.SetActive(true);
    }

    public override void Hide()
    {
        gameObjectMenu.SetActive(false);
    }
}