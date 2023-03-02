using UnityEngine;

public class GoalMenuState: MenuState
{
    public override MenuType GetMenuType() => MenuType.Goal;
    
    public override void Show()
    {
        gameObjectMenu.SetActive(true);
    }

    public override void Hide()
    {
        gameObjectMenu.SetActive(false);
    }
}
