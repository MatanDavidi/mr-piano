using System;
using UnityEngine;
using UnityEngine.UI;
public class MenuController : MonoBehaviour
{
    #region Serialized fields
    public GameObject mainMenu;
    public GameObject songMenu;
    public GameObject instrumentSelectorMenu;
    public GameObject instrumentDetectorMenu;

    public GameObject singlePlayerLobbyMenu;

    public GameObject multiplayerMenu;
    public GameObject createRoomMenu;
    public GameObject joinRoomMenu;
    public GameObject multiplayerLobbyMenu;

    public Button multiplayerButton;

    [SerializeField]
    private GameObject manualPlaneDefinitionMenu;

    [SerializeField]
    private GameObject cvPlaneDefinitionMenu;
    [SerializeField]
    private GameObject singleplayerPauseMenu;
    [SerializeField]
    private GameObject multiplayerPauseMenu;

    [SerializeField]
    private PianoManager pianoManager;

    [SerializeField]
    private Button[] instrumentButtons;
    [SerializeField]
    private Button[] songSelectionButtons;
    [SerializeField]
    private NoteCubeManager noteCubeManager;

    [SerializeField]
    public Button multiplayerStart;
    [SerializeField]
    private Button resumeMultiplayerButton;
    #endregion

    #region Private fields
    private DefinedPlane definedPlane;
    #endregion

    public void Start()
    {
        multiplayerButton.interactable = ProjectConfig.Settings.enableMultiplayer;
        PianoManager.OnPlaneDefined += HandlePlaneDefined;
    }

    public void Update()
    {
        bool multiplayer = ProjectConfig.Settings.enableMultiplayer && ProjectConfig.Settings.useMultiplayer;
        if (!multiplayer && noteCubeManager.playing && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            noteCubeManager.Pause();
            ShowSingleplayerPauseMenu();
        }
    }

    #region Event handlers

    public void HideAllMenus()
    {
        mainMenu.SetActive(false);
        songMenu.SetActive(false);
        instrumentSelectorMenu.SetActive(false);
        singlePlayerLobbyMenu.SetActive(false);
        multiplayerMenu.SetActive(false);
        createRoomMenu.SetActive(false);
        joinRoomMenu.SetActive(false);
        instrumentDetectorMenu.SetActive(false);
        manualPlaneDefinitionMenu.SetActive(false);
        cvPlaneDefinitionMenu.SetActive(false);
        multiplayerLobbyMenu.SetActive(false);
        singleplayerPauseMenu.SetActive(false);
        multiplayerPauseMenu.SetActive(false);
    }
    public void ShowMultiplayerPauseMenu(bool masterPauser)
    {
        HideAllMenus();
        multiplayerPauseMenu.SetActive(true);
        resumeMultiplayerButton.interactable = masterPauser;
    }
    public void ShowSingleplayerPauseMenu()
    {
        HideAllMenus();
        singleplayerPauseMenu.SetActive(true);
    }
    public void ShowMultiplayerMenu()
    {
        HideAllMenus();
        multiplayerMenu.SetActive(true);
    }

    public void ShowInstrumentDetectorMenu()
    {
        HideAllMenus();
        instrumentDetectorMenu.SetActive(true);
    }

    public void ShowManualPlaneDefinitionMenu()
    {
        HideAllMenus();
        manualPlaneDefinitionMenu.SetActive(true);
    }
    public void ShowCVPlaneDefinitionMenu()
    {
        HideAllMenus();
        cvPlaneDefinitionMenu.SetActive(true);
    }

    public void ShowCreateRoomMenu()
    {
        HideAllMenus();
        createRoomMenu.SetActive(true);
    }

    public void ShowJoinRoomMenu()
    {
        HideAllMenus();
        joinRoomMenu.SetActive(true);
    }

    public void ShowInstrumentSelectorMenu()
    {
        HideAllMenus();
        instrumentSelectorMenu.SetActive(true);
    }

    public void ShowSongMenu()
    {
        HideAllMenus();
        songMenu.SetActive(true);
        foreach (Button button in songSelectionButtons)
        {
            button.interactable = ProjectConfig.Settings.master;
        }
    }

    public void ShowMainMenu()
    {
        HideAllMenus();
        mainMenu.SetActive(true);
    }

    public void ShowSinglePlayerLobbyMenu()
    {
        HideAllMenus();
        Debug.Log("Go to singleplayer lobby");
        singlePlayerLobbyMenu.SetActive(true);
    }

    // go to the required menu after the instrument has been selected (different for single vs multiplayer)
    public void postInstrumentSelectedMenu()
    {
        bool multiplayer = ProjectConfig.Settings.enableMultiplayer && ProjectConfig.Settings.useMultiplayer;
        if (multiplayer)
        {
            ShowMultiplayerMenu();
        }
        else
        {
            ShowSongMenu();
        }
    }

    // go to the menu you were at before you were in the song choice menu. This could be Create Room, Join Room or select instrument
    public void preSongChoiceMenu()
    {
        bool multiplayer = ProjectConfig.Settings.enableMultiplayer && ProjectConfig.Settings.useMultiplayer;
        if (multiplayer)
        {
            bool master = ProjectConfig.Settings.master;
            if (master)
            {
                ShowCreateRoomMenu();
            }
            else
            {
                ShowJoinRoomMenu();
            }
        }
        else
        {
            ShowInstrumentSelectorMenu();
        }
    }

    public void ShowMultiplayerLobbyMenu()
    {
        HideAllMenus();
        multiplayerLobbyMenu.SetActive(true);
    }

    // go to the menu after choosing a song
    public void postSongChoiceMenu()
    {
        bool multiplayer = ProjectConfig.Settings.enableMultiplayer && ProjectConfig.Settings.useMultiplayer;
        if (multiplayer)
        {
            ShowMultiplayerLobbyMenu();
        }
        else
        {
            ShowSinglePlayerLobbyMenu();
        }
    }

    private void HandlePlaneDefined(DefinedPlane plane)
    {
        definedPlane = plane;
        if (pianoManager != null)
        {
            pianoManager.Deactivate();
        }
        if (instrumentButtons != null)
        {
            foreach (Button instrumentButton in instrumentButtons)
            {
                if (instrumentButton != null)
                {
                    instrumentButton.interactable = true;
                }
            }
        }
        ShowMainMenu();
    }
    #endregion
}
