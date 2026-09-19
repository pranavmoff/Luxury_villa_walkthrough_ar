using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panels")]
    public GameObject mainMenuPanel;
    public GameObject hudPanel;
    public GameObject inspectCardPanel;
    public GameObject overviewPanel;
    public GameObject controlsLegendPanel;

    [Header("HUD & Crosshair")]
    public GameObject crosshair;
    public GameObject interactionPromptBadge;
    public Text interactionPromptText;

    [Header("Apartment Info Card")]
    public Text cardApartmentIDText;
    public Text cardTitleText;
    public Text cardFloorTypeText;
    public Text cardPriceAreaText;
    public Text cardFeaturesText;

    [Header("Status & Buttons")]
    public Text timeOfDayLabelText;
    public Button returnInspectButton;
    public Button returnOverviewButton;
    public Button timeOfDayToggleButton;
    public Button overviewToggleButton;

    [Header("Main Menu Buttons")]
    public Button startTourBtn;
    public Button menuOverviewBtn;
    public Button unit1Btn;
    public Button unit2Btn;
    public Button unit3Btn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (returnInspectButton != null)
        {
            returnInspectButton.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayUIClick();
                if (GameManager.Instance != null) GameManager.Instance.ExitInspection();
            });
        }

        if (returnOverviewButton != null)
        {
            returnOverviewButton.onClick.AddListener(() => {
                if (SoundManager.Instance != null) SoundManager.Instance.PlayUIClick();
                if (GameManager.Instance != null) GameManager.Instance.ExitOverviewMode();
            });
        }

        if (timeOfDayToggleButton != null)
        {
            timeOfDayToggleButton.onClick.AddListener(() => {
                if (DayNightCycle.Instance != null) DayNightCycle.Instance.CycleNextTimeOfDay();
            });
        }

        if (overviewToggleButton != null)
        {
            overviewToggleButton.onClick.AddListener(() => {
                if (GameManager.Instance != null)
                {
                    if (GameManager.Instance.CurrentState == GameState.OverviewMode)
                        GameManager.Instance.ExitOverviewMode();
                    else
                        GameManager.Instance.EnterOverviewMode();
                }
            });
        }

        if (startTourBtn != null)
        {
            startTourBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.StartTour();
            });
        }

        if (menuOverviewBtn != null)
        {
            menuOverviewBtn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.EnterOverviewMode();
            });
        }

        if (unit1Btn != null)
        {
            unit1Btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.TeleportToApartment(0);
            });
        }

        if (unit2Btn != null)
        {
            unit2Btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.TeleportToApartment(1);
            });
        }

        if (unit3Btn != null)
        {
            unit3Btn.onClick.AddListener(() => {
                if (GameManager.Instance != null) GameManager.Instance.TeleportToApartment(2);
            });
        }
    }

    public void ShowMainMenu(bool show)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(show);
        if (hudPanel != null) hudPanel.SetActive(!show);
        if (inspectCardPanel != null) inspectCardPanel.SetActive(false);
        if (overviewPanel != null) overviewPanel.SetActive(false);
    }

    public void ShowHUD(bool show)
    {
        if (hudPanel != null) hudPanel.SetActive(show);
        if (crosshair != null) crosshair.SetActive(show);
    }

    public void ShowInteractionPrompt(bool show, string message)
    {
        if (interactionPromptBadge != null)
        {
            interactionPromptBadge.SetActive(show);
        }
        if (interactionPromptText != null)
        {
            interactionPromptText.text = message;
        }
    }

    public void ShowInspectCard(bool show, InspectableApartment apt = null)
    {
        if (inspectCardPanel != null)
        {
            inspectCardPanel.SetActive(show);
        }

        if (crosshair != null)
        {
            crosshair.SetActive(!show);
        }

        if (show && apt != null)
        {
            if (cardApartmentIDText != null) cardApartmentIDText.text = apt.apartmentID;
            if (cardTitleText != null) cardTitleText.text = apt.apartmentName;
            if (cardFloorTypeText != null) cardFloorTypeText.text = apt.floorNumber + " • " + apt.apartmentType;
            if (cardPriceAreaText != null) cardPriceAreaText.text = apt.areaSqFt + " | " + apt.priceOrRent;
            if (cardFeaturesText != null) cardFeaturesText.text = apt.features;
        }
    }

    public void ShowOverviewUI(bool show)
    {
        if (overviewPanel != null) overviewPanel.SetActive(show);
        if (hudPanel != null) hudPanel.SetActive(!show);
        if (crosshair != null) crosshair.SetActive(!show);
        if (inspectCardPanel != null) inspectCardPanel.SetActive(false);
    }

    public void UpdateTimeOfDayLabel(string tod)
    {
        if (timeOfDayLabelText != null)
        {
            timeOfDayLabelText.text = "Time: " + tod;
        }
    }
}