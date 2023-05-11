public class WindowMenuState : MenuState
{
    public override MenuType GetMenuType() => MenuType.Window;

    public override void Show()
    {
        gameObjectMenu.SetActive(true);
    }

    public override void Hide()
    {
        gameObjectMenu.SetActive(false);
    }
}
