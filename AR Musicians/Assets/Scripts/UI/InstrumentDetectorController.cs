using UnityEngine;

public class InstrumentDetectorController : MonoBehaviour
{
    bool automatic = true;
    public PianoManager pianoManager;
    public MenuController menuController;
    public void OnToggle(bool automatic)
    {
        this.automatic = automatic;
    }

    public void OnPianoPress()
    {
        pianoManager.Activate(automatic);
        if (automatic)
        {
            menuController.ShowCVPlaneDefinitionMenu();
        } else
        {
            menuController.ShowManualPlaneDefinitionMenu();
        }
    }

    public void OnBongosPress()
    {
        Debug.LogError("Bongos are not supported yet");
    }
}
