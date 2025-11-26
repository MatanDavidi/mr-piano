using UnityEngine;

public class InstrumentDetectorController : MonoBehaviour
{
    bool automatic = true;
    public PianoManager pianoManager;
    public void OnToggle(bool automatic)
    {
        this.automatic = automatic;
    }

    public void OnPianoPress()
    {
        pianoManager.Activate(automatic);
    }

    public void OnBongosPress()
    {
        Debug.LogError("Bongos are not supported yet");
    }
}
