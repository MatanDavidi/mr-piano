using UnityEngine;
using UnityEngine.UI;
public class MenuController : MonoBehaviour
{
    public GameObject mainMenu;
    public GameObject songMenu;
    public GameObject instrumentSelectorMenu;
    public GameObject instrumentDetectorMenu;

    public GameObject singlePlayerLobbyMenu;

    public GameObject multiplayerMenu;
    public GameObject createRoomMenu;
    public GameObject joinRoomMenu;

    public Button multiplayerButton;

    [SerializeField]
    private GameObject manualPlaneDefinitionMenu;

    public void Start()
    {
        multiplayerButton.interactable = ProjectConfig.Settings.enableMultiplayer;
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

    public void HideInstrumentDetectorMenu()
    {
        HideAllMenus();
        manualPlaneDefinitionMenu.SetActive(true);
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
        // TODO: Depending on whether you're the master or not, you have to disable certain things here.
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

    // go to the menu after choosing a song
    public void postSongChoiceMenu()
    {
        bool multiplayer = ProjectConfig.Settings.enableMultiplayer && ProjectConfig.Settings.useMultiplayer;
        if (multiplayer)
        {
            Debug.LogError("Multiplayer UI not yet implemented");
        }
        else
        {
            ShowSinglePlayerLobbyMenu();
        }
    }
    #endregion
}
