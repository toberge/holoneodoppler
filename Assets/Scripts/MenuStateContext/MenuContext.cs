using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities.Solvers;
using UnityEngine;

public enum MenuType
{
    None = 0,
    Welcome = 1,
    Reset = 2,
    Pinch = 3,
    Pin = 4,
    TutorialFinished = 5,
    Goal = 6,
    BloodFlow = 7,
    Window = 8,
    PRF = 9,
    Tracking = 10,
    Measure = 11,
    BLE = 12,
    Debug = 13,
}

// Follows the State pattern: https://refactoring.guru/design-patterns/state/csharp/example

public class MenuContext : MonoBehaviour
{
    private MenuType previousType = MenuType.None;
    private MenuType currentType = MenuType.None;

    private readonly Dictionary<MenuType, MenuState> menus = new Dictionary<MenuType, MenuState>();

    [SerializeField] private PressableButton prevButton;
    [SerializeField] public PressableButton nextButton;
    [SerializeField] public Interactable pinButton;
    [SerializeField] public PressableButton resetButton;
    [SerializeField] public MenuButtons menuButtons;
    [SerializeField] public InteractionsCoachHelper interactionHint;
    [SerializeField] public Orbital spectrogram;
    [SerializeField] public GameObject dialogPrefab;
    [SerializeField] public AudioClip clipTrackingSuccess;

    private FollowMeToggle followMeToggle;
    private RadialView radialView;
    public AudioSource myAudioSource;

    private const int LastRealMenu = (int)MenuType.Measure;

    private void Awake()
    {
        followMeToggle = GetComponent<FollowMeToggle>();
        radialView = GetComponent<RadialView>();
        myAudioSource = GetComponent<AudioSource>();
        resetButton.ButtonReleased.AddListener(ResetPosition);
        Debug.Assert(followMeToggle != null, "Could not find FollowMeToggle component on " + followMeToggle.name);
    }

    private void Start()
    {
        foreach (var menu in GetComponents<MenuState>())
        {
            menus.Add(menu.GetMenuType(), menu);
            menu.SetContext(this);
        }

        Debug.Log(menus.Values.Count + " menus are added");

        SetState(MenuType.Welcome); // Start with the Welcome Menu
        nextButton.ButtonPressed.AddListener(NextButtonPressed);
        prevButton.ButtonPressed.AddListener(PreviousButtonPressed);
    }

    public void SetState(MenuType newType)
    {
        if (!CheckAllowedState(newType)) return;

        previousType = currentType;
        currentType = newType;

        SetPreviousNextButtonsActivation();

        ChangeMenuVisibility(previousType, false);
        ChangeMenuVisibility(currentType, true);

        menuButtons.OnStateChange(currentType);

        //OnStateChange?.Invoke(_currentType);
    }


    private void ChangeMenuVisibility(MenuType menu, bool visible)
    {
        if (menu == MenuType.None)
        {
            DeactivateAllMenus(); // To make sure nothing is showing
            return;
        }

        if (!menus.ContainsKey(menu))
            throw new ArgumentException("There is not GameObject in menus dictionary for " + menu + " menu.");

        if (visible)
            menus[menu].Show();
        else
            menus[menu].Hide();
    }

    public void PinTheMenu()
    {
        followMeToggle.SetFollowMeBehavior(false);
        pinButton.IsToggled = true;
    }

    public void StartTutorial()
    {
        SetState((MenuType)((int)MenuType.Welcome + 1));
    }

    public void ShowTrackingMenu()
    {
        SetState(MenuType.Tracking);
    }

    public void ShowGoalMenu()
    {
        SetState(MenuType.Goal);
    }

    public void ShowMeasureMenu()
    {
        SetState(MenuType.Measure);
    }

    public void ShowBLEMenu()
    {
        SetState(MenuType.BLE);
    }

    public void ShowDebugMenu()
    {
        SetState(MenuType.Debug);
    }


    private bool CheckAllowedState(MenuType newType)
    {
        if (currentType == newType)
        {
            Debug.LogWarning("Tried to change to the state: " + newType);
            return false;
        }

        if (newType != MenuType.None && !menus.ContainsKey(newType))
        {
            Debug.LogWarning("Do not have this type of menu in the list: " + newType);
            return false;
        }

        // later can only change in a certain order
        return true;
    }

    private void NextButtonPressed()
    {
        int current = (int)currentType;
        int next = current + 1;
        if (next > LastRealMenu)
        {
            Debug.LogWarning($"No more menus (at {currentType})");
        }
        else
        {
            SetState((MenuType)next);
        }
    }

    private void PreviousButtonPressed()
    {
        int current = (int)currentType;
        int next = current - 1;
        if (next <= 0)
        {
            Debug.LogWarning($"No more menus (at {currentType})");
        }
        else
        {
            SetState((MenuType)next);
        }
    }

    private void SetPreviousNextButtonsActivation()
    {
        int current = (int)currentType;
        if (!(current < LastRealMenu))
        {
            nextButton.gameObject.SetActive(false);
        }
        else if (!nextButton.gameObject.activeSelf)
        {
            nextButton.gameObject.SetActive(true);
        }

        if (!(current > 1 && current <= LastRealMenu))
        {
            prevButton.gameObject.SetActive(false);
        }
        else if (!prevButton.gameObject.activeSelf)
        {
            prevButton.gameObject.SetActive(true);
        }
    }

    public void ResetPosition()
    {
        followMeToggle.SetFollowMeBehavior(true);
        spectrogram.enabled = true; // Enabling orbiting Script there that makes it snap back into place 
        pinButton.IsToggled = false;
        StartCoroutine(ResetMenuPosition());
    }

    private IEnumerator ResetMenuPosition()
    {
        float maxViewDegrees = radialView.MaxViewDegrees;
        radialView.MaxViewDegrees = 0;
        yield return new WaitForSecondsRealtime(2);
        radialView.MaxViewDegrees = maxViewDegrees;
    }

    public void ExitApplication()
    {
        Dialog myDialog = Dialog.Open(dialogPrefab, DialogButtonType.Yes | DialogButtonType.No, "Exiting Application",
            "Are you sure you want to close the application?", true);
        Debug.Log("My dialog: " + myDialog);
        if (myDialog != null)
        {
            ChangeVisibilityOfChildren(false);
            myDialog.OnClosed += OnClosedDialogEvent;
        }
    }

    private void ChangeVisibilityOfChildren(bool active)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    private void OnClosedDialogEvent(DialogResult obj)
    {
        if (obj.Result == DialogButtonType.Yes)
        {
            // save any game data here
#if UNITY_EDITOR
            // Application.Quit() does not work in the editor so
            // UnityEditor.EditorApplication.isPlaying need to be set to false to end the game
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
        else
        {
            ChangeVisibilityOfChildren(true);
        }
    }

    private void OnDestroy()
    {
        nextButton.ButtonPressed.RemoveListener(NextButtonPressed);
        prevButton.ButtonPressed.RemoveListener(PreviousButtonPressed);
    }

    private void DeactivateAllMenus()
    {
        foreach (MenuState menu in menus.Values)
        {
            menu.Hide();
        }
    }
}