using MenuStateContext;
using Microsoft.MixedReality.Toolkit.UI;
using UnityEngine;

public class MenuButtons : MonoBehaviour
{
    [SerializeField] private Interactable tutorialButton;
    [SerializeField] private Interactable knowledgeBaseButton;

    [SerializeField] private Interactable[] mainButtons;

    private bool unlockedTutorial;

    private void Start()
    {
        tutorialButton.gameObject.SetActive(false);
        knowledgeBaseButton.gameObject.SetActive(false);
        foreach (var button in mainButtons)
        {
            var type = button.GetComponent<MenuButton>().menu;
            if (type != MenuType.Debug && type != MenuType.BLE)
            {
                button.gameObject.SetActive(false);
            }
        }
    }

    public void EnableAll()
    {
        knowledgeBaseButton.gameObject.SetActive(true);
        foreach (var button in mainButtons)
        {
            button.gameObject.SetActive(true);
        }
    }

    public void OnStateChange(MenuType menuState)
    {
        tutorialButton.IsToggled = false;
        knowledgeBaseButton.IsToggled = false;
        foreach (var button in mainButtons)
        {
            button.IsToggled = false;
        }

        if (!unlockedTutorial && menuState >= MenuType.TutorialFinished)
        {
            UnlockTutorial();
        }

        if (menuState >= MenuType.Reset && menuState <= MenuType.TutorialFinished && unlockedTutorial)
        {
            tutorialButton.gameObject.SetActive(true);
            tutorialButton.IsToggled = true;
        }
        else if (menuState >= MenuType.Goal && menuState <= MenuType.PRF)
        {
            knowledgeBaseButton.gameObject.SetActive(true);
            knowledgeBaseButton.IsToggled = true;
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