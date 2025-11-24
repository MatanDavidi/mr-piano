using UnityEngine;
using TMPro;
using Meta.XR;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class MultiplayerController : MonoBehaviourPunCallbacks
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

    [SerializeField]
    private MenuController menuController;
    [SerializeField]
    private SongsUIHandler songsUIHandler;
    [SerializeField]
    private NoteCubeManager noteCubeManager;
    [SerializeField]
    private CountdownUI countDownUI;
    [SerializeField]
    private Toggle readyToggle;
    private int localQueuePos = 0;

    private Dictionary<Player, bool> playersReady;
    void Start()
    {
        simulator = !EnvironmentRaycastManager.IsSupported;
        PhotonNetwork.ConnectUsingSettings();
        SongsUIHandler.OnNewPosition += OnNewPositionLocal;
        SongsUIHandler.OnSelectSong += OnSelectSongLocal;
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
    }
    #region Ready
    public void OnToggleLocal()
    {
        readyLocal = readyToggle.isOn;
        if (master)
        {
            Debug.Log("Toggling master ready to: " + readyLocal);
            Debug.Log(playersReady.Keys.Count);
            playersReady[PhotonNetwork.LocalPlayer] = readyLocal;
            everyoneReadyCheck();
        }
        else
        {
            photonView.RPC("OnReadyToggleNetwork", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer, readyLocal);
        }
    }
    [PunRPC]
    // Clients call this via RPC to tell the master whether they are ready or not.
    private void OnReadyToggleNetwork(Player player, bool newReady)
    {
        Debug.Log("Setting player " + player.ActorNumber + " to ready-state: " + newReady);
        playersReady[player] = newReady;
        everyoneReadyCheck();
    }

    private void everyoneReadyCheck()
    {
        bool everyoneReady = true;
        foreach (var ready in playersReady.Values)
        {
            Debug.Log(playersReady);
            if (!ready) { everyoneReady = false; break; }
        }
        Debug.Log("Everyone is ready: " + everyoneReady);
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
    // Locally chose new song, so this would have to be the master
    private void OnNewPositionLocal(int newPosition)
    {
        if (master && multiplayer && PhotonNetwork.InRoom)
        {
            localQueuePos = newPosition;
            photonView.RPC("OnNextPositionNetwork", RpcTarget.Others, localQueuePos);
        }
    }
    // Locally selected song, so this would have to be the master
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
        noteCubeManager.updateSong(songsUIHandler.SelectedSong);
        menuController.postSongChoiceMenu();
    }
    [PunRPC]
    // Receive new songData
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
        menuController.multiplayerButton.interactable = ProjectConfig.Settings.enableMultiplayer;

    }

    public void CreateRoom()
    {
        string roomName = createRoomText.text;
        if (RoomExists(roomName))
        {
            Debug.LogError("Room " + roomName + " already exists!");
            // TODO: Give a proper UI error message here
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
            // TODO: Give a proper UI error message here
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
        Debug.LogError("Maybe implement a you were kicked UI here. Remember that OnLeftRoom is also called though");
    }
    public void LeaveRoom()
    {
        if (!PhotonNetwork.InRoom)
            return;
        if (!master)
        {
            PhotonNetwork.LeaveRoom();
            return;
        }
        // If this is the master, need to kill the room.
        foreach (var player in PhotonNetwork.PlayerListOthers)
        {
            photonView.RPC("OnKick", player); // Kicks them
        }
        playersReady = new Dictionary<Player, bool>();
        PhotonNetwork.LeaveRoom();
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        // New Client that joins the room needs to know the current song
        photonView.RPC("OnGiveQueuePosNetwork", RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        if (master)
        {
            playersReady[PhotonNetwork.LocalPlayer] = false;
        }
    }
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        if (master)
        {
            playersReady[newPlayer] = false;
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);
        if (master)
        {
            playersReady.Remove(otherPlayer);
        }
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        menuController.preSongChoiceMenu(); // everyone goes back to where they were before the song Choice menu, so either Join Room or Create Room
        master = true;
        ProjectConfig.Settings.master = true;
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("Disconnected from Photon: " + cause);
    }
    #endregion
    #region UI

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
        Debug.Log("OnSelect");
        if (!simulator) // Not in simulator
            overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        createRoomTyping = true;
        joinRoomTyping = false;

    }
    public void OnSelectTextJoinRoom()
    {
        Debug.Log("OnSelect");
        if (!simulator) // Not in simulator
            overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        joinRoomTyping = true;
        createRoomTyping = false;
    }
    public void OnDeselectTextCreateRoom()
    {
        createRoomTyping = false;
        if (simulator) // In simulator
            createRoomText.text = "Room1";
        Debug.Log("Entered Text For Create Room with: " + createRoomText.text);
    }

    public void OnDeselectTextJoinRoom()
    {
        joinRoomTyping = false;
        if (simulator) // In simulator
            joinRoomText.text = "Room1";
        Debug.Log("Entered Text For Join Room with: " + joinRoomText.text);

    }
    #endregion 
}
