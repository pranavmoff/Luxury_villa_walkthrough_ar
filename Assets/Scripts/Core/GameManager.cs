using UnityEngine;
using System.Collections.Generic;

public enum GameState
{
    MainMenu,
    FreeRoam,
    InspectMode,
    OverviewMode
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Controllers")]
    public PlayerController playerController;
    public CameraController cameraController;

    [Header("Apartments in Complex")]
    public List<InspectableApartment> apartments = new List<InspectableApartment>();

    [Header("Spawn Points")]
    public Transform groundSpawnPoint;
    public Transform overviewCameraAnchor;

    public GameState CurrentState { get; private set; } = GameState.MainMenu;
    private InspectableApartment currentInspectedApartment = null;

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
        // Start with Main Menu + camera at overview for dramatic building reveal
        SetState(GameState.MainMenu);
        if (cameraController != null)
            cameraController.SnapToOverview();
    }

    private void Update()
    {
        // Global Keyboard shortcuts using InputBridge
        if (InputBridge.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.InspectMode)
            {
                ExitInspection();
            }
            else if (CurrentState == GameState.OverviewMode)
            {
                ExitOverviewMode();
            }
            else if (CurrentState == GameState.FreeRoam)
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                    cameraController.LockCursor(false);
                else
                    cameraController.LockCursor(true);
            }
        }

        // Quick Overview toggle with 'O'
        if (InputBridge.GetKeyDown(KeyCode.O))
        {
            if (CurrentState == GameState.OverviewMode)
                ExitOverviewMode();
            else if (CurrentState == GameState.FreeRoam)
                EnterOverviewMode();
        }

        // Day/Night cycle toggle with 'T'
        if (InputBridge.GetKeyDown(KeyCode.T))
        {
            if (DayNightCycle.Instance != null) DayNightCycle.Instance.CycleNextTimeOfDay();
        }

        // Quick inspect shortcuts: 1, 2, 3
        if (CurrentState == GameState.FreeRoam || CurrentState == GameState.InspectMode)
        {
            if (InputBridge.GetKeyDown(KeyCode.Alpha1) && apartments.Count > 0) InspectApartment(apartments[0]);
            if (InputBridge.GetKeyDown(KeyCode.Alpha2) && apartments.Count > 1) InspectApartment(apartments[1]);
            if (InputBridge.GetKeyDown(KeyCode.Alpha3) && apartments.Count > 2) InspectApartment(apartments[2]);
        }

        // Press Enter on main menu to start
        if (CurrentState == GameState.MainMenu)
        {
            if (InputBridge.GetKeyDown(KeyCode.Return)) StartTour();
        }
    }

    public void SetState(GameState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                if (playerController != null) playerController.canMove = false;
                if (cameraController != null) cameraController.LockCursor(false);
                if (UIManager.Instance != null) UIManager.Instance.ShowMainMenu(true);
                break;

            case GameState.FreeRoam:
                if (playerController != null) playerController.canMove = true;
                if (cameraController != null) cameraController.LockCursor(true);
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowMainMenu(false);
                    UIManager.Instance.ShowHUD(true);
                    UIManager.Instance.ShowInspectCard(false);
                    UIManager.Instance.ShowOverviewUI(false);
                }
                break;

            case GameState.InspectMode:
                if (playerController != null) playerController.canMove = false;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowHUD(false);
                    UIManager.Instance.ShowInspectCard(true, currentInspectedApartment);
                    UIManager.Instance.ShowOverviewUI(false);
                }
                break;

            case GameState.OverviewMode:
                if (playerController != null) playerController.canMove = false;
                if (cameraController != null) cameraController.EnterOverview();
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowOverviewUI(true);
                }
                break;
        }
    }

    public void StartTour()
    {
        if (SoundManager.Instance != null) SoundManager.Instance.PlayUIClick();
        if (groundSpawnPoint != null && playerController != null)
            playerController.SetPositionAndRotation(groundSpawnPoint.position, Quaternion.identity);
        SetState(GameState.FreeRoam);
    }

    public void InspectApartment(InspectableApartment apartment)
    {
        if (apartment == null) return;
        currentInspectedApartment = apartment;
        SetState(GameState.InspectMode);

        if (cameraController != null)
        {
            cameraController.StartInspect(apartment);
        }

        apartment.SetInteriorLightsActive(true);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayInspectEnter();
        }
    }

    public void ExitInspection()
    {
        if (currentInspectedApartment != null)
        {
            currentInspectedApartment.SetHovered(false);
            currentInspectedApartment = null;
        }

        if (cameraController != null)
        {
            cameraController.ExitInspect();
        }

        SetState(GameState.FreeRoam);

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayInspectExit();
        }
    }

    public void EnterOverviewMode()
    {
        SetState(GameState.OverviewMode);
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayInspectEnter();
        }
    }

    public void ExitOverviewMode()
    {
        if (cameraController != null)
        {
            cameraController.ExitOverview();
        }
        SetState(GameState.FreeRoam);
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayInspectExit();
        }
    }

    public void TeleportToApartment(int index)
    {
        if (index >= 0 && index < apartments.Count)
        {
            var apt = apartments[index];
            if (apt.playerSpawnPoint != null && playerController != null)
            {
                playerController.SetPositionAndRotation(apt.playerSpawnPoint.position, apt.playerSpawnPoint.rotation);
            }
            InspectApartment(apt);
        }
    }

    public void TeleportToGround()
    {
        if (groundSpawnPoint != null && playerController != null)
        {
            playerController.SetPositionAndRotation(groundSpawnPoint.position, groundSpawnPoint.rotation);
        }
        if (CurrentState != GameState.FreeRoam)
        {
            ExitInspection();
        }
    }
}