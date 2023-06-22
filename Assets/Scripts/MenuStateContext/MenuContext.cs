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
    Tracking = 6,
    BLE = 7,
    Measure = 8,
    Estimate = 9,
}

// Attempting to use State pattern: https://refactoring.guru/design-patterns/state/csharp/example

public class MenuContext : MonoBehaviour
{
    // public delegate void MenuController(MenuType newType);
    // public MenuController OnStateChange;
    private MenuType _previousType = MenuType.None;
    private MenuType _currentType = MenuType.None;

    private readonly Dictionary<MenuType, MenuState> _menus = new Dictionary<MenuType, MenuState>();

    [SerializeField] private PressableButton prevButton;
    [SerializeField] public PressableButton nextButton;
    [SerializeField] public Interactable pinButton;
    [SerializeField] public PressableButton resetButton;
    [SerializeField] public MenuButtons menuButtons;
    [SerializeField] public InteractionsCoachHelper interactionHint;
    [SerializeField] public Orbital spectrogram;
    [SerializeField] public GameObject dialogPrefab;
    [SerializeField] public SlidersStateController slidersStateController;
    [SerializeField] public AudioClip clipTrackingSuccess;
    [SerializeField] public AudioClip clipVelocitySuccess;

    private FollowMeToggle _followMeToggle;
    private RadialView _radialView;
    public AudioSource myAudioSource;

    private void Awake()
    {
        _followMeToggle = GetComponent<FollowMeToggle>();
        _radialView = GetComponent<RadialView>();
        myAudioSource = GetComponent<AudioSource>();
        resetButton.ButtonReleased.AddListener(ResetPosition);
        Debug.Assert(_followMeToggle != null, "Could not find FollowMeToggle component on " + _followMeToggle.name);
    }

    void Start()
    {
        foreach (var menu in GetComponents<MenuState>())
        {
            _menus.Add(menu.GetMenuType(), menu);
            menu.SetContext(this);
        }

        Debug.Log(_menus.Values.Count + " menus are added");

        SetState(MenuType.Welcome); // Start with the Welcome Menu
        nextButton.ButtonPressed.AddListener(NextButtonPressed);
        prevButton.ButtonPressed.AddListener(PreviousButtonPressed);

    }

    public MenuType GetPreviousState() => _previousType;
    public MenuType GetCurrentState() => _currentType;

    public void SetState(MenuType newType)
    {
        if (!CheckAllowedState(newType)) return;

        _previousType = _currentType;
        _currentType = newType;

        ChangeMenuVisibility(_previousType, false);
        ChangeMenuVisibility(_currentType, true);

        SetPreviousNextButtonsActivation();
        menuButtons.OnStateChange(_currentType);

        //OnStateChange?.Invoke(_currentType);
    }


    private void ChangeMenuVisibility(MenuType menu, bool visible)
    {
        if (menu == MenuType.None)
        {
            DeactivateAllMenus(); // To make sure nothing is showing
            return;
        }
        if (!_menus.ContainsKey(menu)) throw new ArgumentException("There is not GameObject in menus dictionary for " + menu + " menu.");

        if (visible)
            _menus[menu].Show();
        else
            _menus[menu].Hide();
    }

    public void PinTheMenu()
    {
        _followMeToggle.SetFollowMeBehavior(false);
        pinButton.IsToggled = true;
    }

    public void StartTutorial()
    {
        SetState((MenuType)(((int)MenuType.Welcome) + 1));
    }

    public void ShowTrackingMenu()
    {
        SetState(MenuType.Tracking);
    }

    public void ShowBLEMenu()
    {
        SetState(MenuType.BLE);
    }

    public void ShowMeasureMenu()
    {
        SetState(MenuType.Measure);
    }

    public void ShowEstimateMenu()
    {
        SetState(MenuType.Estimate);
    }

    private bool CheckAllowedState(MenuType newType)
    {
        if (_currentType == newType)
        {
            Debug.LogWarning("Tried to change to the state: " + newType);
            return false;
        }

        if (newType != MenuType.None && !_menus.ContainsKey(newType))
        {
            Debug.LogWarning("Do not have this type of menu in the list: " + newType);
            return false;
        }
        // later can only change in a certain order
        return true;
    }

    private void NextButtonPressed()
    {
        Debug.Log("hi is this working=?");
        int current = (int)_currentType;
        int next = current + 1;
        if (next > _menus.Count)
        {
            Debug.Log("No more menus");
        }
        else
        {
            SetState((MenuType)next);
        }
    }

    private void PreviousButtonPressed()
    {
        int current = (int)_currentType;
        int next = current - 1;
        if (next <= 0)
        {
            Debug.Log("No more menus");
        }
        else
        {
            SetState((MenuType)next);
        }
    }

    private void SetPreviousNextButtonsActivation()
    {
        if ((int)_currentType + 1 > _menus.Count)
        {
            nextButton.gameObject.SetActive(false);
        }
        else if (!nextButton.gameObject.activeSelf)
        {
            nextButton.gameObject.SetActive(true);
        }

        if ((int)_currentType - 1 <= 0)
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
        _followMeToggle.SetFollowMeBehavior(true);
        spectrogram.enabled = true; // Enabling orbiting Script there that makes it snap back into place 
        pinButton.IsToggled = false;
        StartCoroutine(ResetMenuPosition());
    }

    private IEnumerator ResetMenuPosition()
    {
        float maxViewDegrees = _radialView.MaxViewDegrees;
        _radialView.MaxViewDegrees = 0;
        yield return new WaitForSecondsRealtime(2);
        _radialView.MaxViewDegrees = maxViewDegrees;

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
        foreach (MenuState menu in _menus.Values)
        {
            menu.Hide();
        }
    }

}
