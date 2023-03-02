using MenuStateContext;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] private Interactable tutorialButton;

    [SerializeField] private Interactable[] mainButtons;

    private bool unlockedTutorial;

    private void Start()
    {
        tutorialButton.gameObject.SetActive(false);
        foreach (var button in mainButtons)
        {
            var type = button.GetComponent<MenuButton>().menu;
            if (type != MenuType.Debug && type != MenuType.BLE)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    public void OnStateChange(MenuType menuState)
    {
        tutorialButton.IsToggled = false;
        foreach (var button in mainButtons)
        {
            button.IsToggled = false;
        }

        int state = (int)menuState;

        if (!unlockedTutorial && menuState >= MenuType.TutorialFinished)
        {
            UnlockTutorial();
        }

        if (state >= 2 && state <= 5 && unlockedTutorial)
        {
            tutorialButton.gameObject.SetActive(true);
            tutorialButton.IsToggled = true;
        }
        else
        {
            foreach (var button in mainButtons)
            {
                if (button.GetComponent<MenuButton>().menu == menuState)
                {
                    button.gameObject.SetActive(true);
                    button.IsToggled = true;
                }
            }
        }
    }

    private void UnlockTutorial()
    {
        unlockedTutorial = true;
        tutorialButton.gameObject.SetActive(true);
    }
}