using UnityEngine;
using TMPro;

public class MultiplayerController : MonoBehaviour
{
    private TouchScreenKeyboard overlayKeyboard;
    public TMP_Text createRoomText;
    public TMP_Text joinRoomText;

    public void OnMultiplayer()
    {
        ProjectConfig.Settings.useMultiplayer = true;
    }

    public void OnSingleplayer()
    {
        ProjectConfig.Settings.useMultiplayer = false;
    }

    public void OnEnterTextCreateRoom()
    {
        overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        Debug.Log(overlayKeyboard);
        //createRoomText.text = overlayKeyboard.text;
    }

    public void OnEnterTextJoinRoom()
    {
        overlayKeyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
        //joinRoomText.text = overlayKeyboard.text;
    }
}
