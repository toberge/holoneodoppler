using UnityEngine;

public class DebugMenuState: MenuState
{
    public override MenuType GetMenuType() => MenuType.Debug;
    
    public override void Show()
    {
        gameObjectMenu.SetActive(true);
    }

    public override void Hide()
    {
        gameObjectMenu.SetActive(false);
    }
}
