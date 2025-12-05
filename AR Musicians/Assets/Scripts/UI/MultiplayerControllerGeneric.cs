using UnityEngine;
using TMPro;
using Meta.XR;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
using Meta.XR.MRUtilityKit; // Added for EnvironmentRaycastManager

public class MultiplayerControllerGeneric : MonoBehaviourPunCallbacks
{
    private TouchScreenKeyboard overlayKeyboard;
    public TMP_InputField createRoomText;
    public TMP_InputField joinRoomText;
    private bool createRoomTyping = false;
    private bool joinRoomTyping = false;

    private bool simulator;
    private List<string> cachedRoomNames = new List<string>();
    private bool master = true;
    private bool readyLocal = false;
    private bool multiplayer = false;

    [Header("Generic References")]
    [SerializeField] private MenuControllerGeneric menuController;
    [SerializeField] private SongsUIHandlerGeneric songsUIHandler;
    [SerializeField] private RhythmGameManager rhythmGameManager; // Changed from NoteCubeManager
    [SerializeField] private CountdownUIGeneric countDownUI;      // Changed from CountdownUI
    [SerializeField] private Toggle readyToggle;

    private int localQueuePos = 0;
    private Dictionary<Player, bool> playersReady;
    private bool masterPauser = false;

    void Start()
    {
        // EnvironmentRaycastManager check
        simulator = !EnvironmentRaycastManager.IsSupported;
        PhotonNetwork.ConnectUsingSettings();

        // Ensure SongsUIHandlerGeneric events are linked
        SongsUIHandlerGeneric.OnNewPosition += OnNewPositionLocal;
        SongsUIHandlerGeneric.OnSelectSong += OnSelectSongLocal;
    }

    private void Update()
    {
        if (!simulator && overlayKeyboard != null && overlayKeyboard.active)
        {
            if (createRoomTyping && createRoomText.text != overlayKeyboard.text)
            {
                createRoomText.text = overlayKeyboard.text;
            }
            else if (joinRoomTyping && joinRoomText.text != overlayKeyboard.text)
            {
                joinRoomText.text = overlayKeyboard.text;
            }
        }
        multiplayer = ProjectConfig.Settings.enableMultiplayer && ProjectConfig.Settings.useMultiplayer;
        if (!masterPauser && multiplayer && rhythmGameManager.playing && OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            OnPauseLocal();
        }
    }

    #region Pause
    public void OnQuitLocal()
    {
        if (master)
        {
            photonView.RPC("OnQuitNetwork", RpcTarget.Others);
        }
    }
    [PunRPC]
    private void OnQuitNetwork()
    {
        rhythmGameManager.Quit();
    }
    // Locally pressed pause button
    private void OnPauseLocal()
    {
        masterPauser = true;
        photonView.RPC("OnPauseNetwork", RpcTarget.All);
    }
    [PunRPC]
    // Someone in the lobby pressed the pause button
    private void OnPauseNetwork()
    {
        if (rhythmGameManager.quit)
            return;
        rhythmGameManager.Pause();
        menuController.ShowMultiplayerPauseMenu(masterPauser || master);
        masterPauser = false;
    }
    public void OnResumeLocal()
    {
        photonView.RPC("OnResumeNetwork", RpcTarget.All);
    }
    [PunRPC]
    private void OnResumeNetwork()
    {
        if (rhythmGameManager.quit)
            return;
        menuController.HideAllMenus();
        countDownUI.ResumeCountdown();
    }
    #endregion

    #region Ready Logic

    public void OnToggleLocal()
    {
        readyLocal = readyToggle.isOn;
        if (master)
        {
            Debug.Log("Toggling master ready to: " + readyLocal);
            if (playersReady == null) playersReady = new Dictionary<Player, bool>();

            playersReady[PhotonNetwork.LocalPlayer] = readyLocal;
            EveryoneReadyCheck();
        }
        else
        {
            photonView.RPC("OnReadyToggleNetwork", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, readyLocal);
        }
    }

    [PunRPC]
    private void OnReadyToggleNetwork(Player player, bool newReady)
    {
        Debug.Log("Setting player " + player.ActorNumber + " to ready-state: " + newReady);
        if (playersReady == null) playersReady = new Dictionary<Player, bool>();

        playersReady[player] = newReady;
        EveryoneReadyCheck();
    }

    private void EveryoneReadyCheck()
    {
        if (playersReady == null) return;

        bool everyoneReady = true;
        foreach (var ready in playersReady.Values)
        {
            if (!ready) { everyoneReady = false; break; }
        }
        Debug.Log("Everyone is ready: " + everyoneReady);

        // Enable the start button if everyone is ready
        if (menuController.multiplayerStart != null)
            menuController.multiplayerStart.interactable = everyoneReady;
    }

    public void OnStartSongLocal()
    {
        if (master)
        {
            photonView.RPC("OnStartSongNetwork", RpcTarget.All);
        }
    }

    [PunRPC]
    private void OnStartSongNetwork()
    {
        menuController.HideAllMenus();
        countDownUI.StartCountdown();
    }

    #endregion

    #region SongSync

    // Locally chose new song (scrolled), so this would have to be the master
    private void OnNewPositionLocal(int newPosition)
    {
        if (master && multiplayer && PhotonNetwork.InRoom)
        {
            localQueuePos = newPosition;
            photonView.RPC("OnNextPositionNetwork", RpcTarget.Others, localQueuePos);
        }
    }

    // Locally selected song (clicked play/select), so this would have to be the master
    private void OnSelectSongLocal()
    {
        if (master && multiplayer && PhotonNetwork.InRoom)
        {
            photonView.RPC("OnSelectSongNetwork", RpcTarget.Others);
        }
    }

    [PunRPC]
    // Master selected the song, we need to do the same
    private void OnSelectSongNetwork()
    {
        // Using the Generic Manager to load the song
        StartCoroutine(rhythmGameManager.updateSongRoutine(songsUIHandler.SelectedSong));
        menuController.postSongChoiceMenu();
    }

    [PunRPC]
    // Receive new songData (scroll update)
    private void OnNextPositionNetwork(int newPosition)
    {
        localQueuePos = newPosition;
        songsUIHandler.SelectedSong = songsUIHandler.getSongData(localQueuePos);
    }

    [PunRPC]
    // Give the current queue position to the requesting player
    private void OnGiveQueuePosNetwork(Player player)
    {
        photonView.RPC("OnNextPositionNetwork", player, localQueuePos);
    }

    #endregion

    #region Connection

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        cachedRoomNames.Clear();
        cachedRoomNames.AddRange(roomList.Select(r => r.Name));
    }

    public bool RoomExists(string name)
    {
        return cachedRoomNames.Contains(name);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon Master Server.");
        PhotonNetwork.JoinLobby();
        ProjectConfig.Settings.enableMultiplayer = true;
        if (menuController.multiplayerButton != null)
            menuController.multiplayerButton.interactable = ProjectConfig.Settings.enableMultiplayer && menuController.instrumentDefined;
    }

    public void CreateRoom()
    {
        string roomName = createRoomText.text;
        if (RoomExists(roomName))
        {
            Debug.LogError("Room " + roomName + " already exists!");
            return;
        }
        PhotonNetwork.CreateRoom(roomName, new RoomOptions { MaxPlayers = 4 }, TypedLobby.Default);
        master = true;
        ProjectConfig.Settings.master = true;
        playersReady = new Dictionary<Player, bool>();
        menuController.ShowSongMenu();
    }

    public void JoinRoom()
    {
        string roomName = joinRoomText.text;
        if (!RoomExists(roomName))
        {
            Debug.LogError("Room " + roomName + " does not exist!");
            return;
        }
        PhotonNetwork.JoinRoom(roomName);
        master = false;
        ProjectConfig.Settings.master = false;
        menuController.ShowSongMenu();
    }

    [PunRPC]
    public void OnKick()
    {
        if (!master)
            PhotonNetwork.LeaveRoom();
        Debug.LogError("You were kicked.");
    }

    public void LeaveRoom()
    {
        if (!PhotonNetwork.InRoom) return;

        if (!master)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }

        // Master leaves -> Kick everyone else or Migrate Master (here we kick)
        foreach (var player in PhotonNetwork.PlayerListOthers)
        {
            photonView.RPC("OnKick", player);
        }
        playersReady = new Dictionary<Player, bool>();
        PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        // New Client joins -> Ask Master for current song
        photonView.RPC("OnGiveQueuePosNetwork", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        if (master)
        {
            if (playersReady == null) playersReady = new Dictionary<Player, bool>();
            playersReady[PhotonNetwork.LocalPlayer] = false;
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (master)
        {
            if (playersReady == null) playersReady = new Dictionary<Player, bool>();
            playersReady[newPlayer] = false;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (master && playersReady != null)
        {
            playersReady.Remove(otherPlayer);
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        menuController.preSongChoiceMenu();
        master = true;
        ProjectConfig.Settings.master = true;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected from Photon: " + cause);
    }

    #endregion

    #region UI Handlers

    public void OnMultiplayer()
    {
        ProjectConfig.Settings.useMultiplayer = true;
    }

    public void OnSingleplayer()
    {
        ProjectConfig.Settings.useMultiplayer = false;
        master = true;
        ProjectConfig.Settings.master = true;
    }

    public void OnSelectTextCreateRoom()
    {
        Debug.Log("OnSelect Create Room");
        if (!simulator)
            overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        createRoomTyping = true;
        joinRoomTyping = false;
    }

    public void OnSelectTextJoinRoom()
    {
        Debug.Log("OnSelect Join Room");
        if (!simulator)
            overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        joinRoomTyping = true;
        createRoomTyping = false;
    }

    public void OnDeselectTextCreateRoom()
    {
        createRoomTyping = false;
        if (simulator) createRoomText.text = "Room1";
        Debug.Log("Create Room Text: " + createRoomText.text);
    }

    public void OnDeselectTextJoinRoom()
    {
        joinRoomTyping = false;
        if (simulator) joinRoomText.text = "Room1"; // If you want identical behavior
        else if (string.IsNullOrEmpty(joinRoomText.text)) joinRoomText.text = "Room1"; // Fallback

        Debug.Log("Join Room Text: " + joinRoomText.text);
    }

    #endregion 
}