using UnityEngine;
using UnityEngine.UI;

public class InstrumentDetectorController : MonoBehaviour
{
    bool automatic = true;
    public PianoManager pianoManager;
    public MenuController menuController;
    [SerializeField]
    private Toggle automaticToggle;
    public void OnToggle()
    {
        this.automatic = automaticToggle.isOn;
    }

    public void OnPianoPress()
    {
        pianoManager.Activate(automatic);
        if (automatic)
        {
            menuController.ShowCVPlaneDefinitionMenu();
        }
        else
        {
            menuController.ShowManualPlaneDefinitionMenu();
        }
    }

    public void OnBongosPress()
    {
        Debug.LogError("Bongos are not supported yet");
    }
}
