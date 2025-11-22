using UnityEngine;

public class InstrumentDetectorController : MonoBehaviour
{
    bool automatic = true;
    public RayCastPlaneFinder rayCastPlaneFinder;
    public PianoManager pianoManager;
    public void OnToggle(bool automatic)
    {
        this.automatic = automatic;
    }

    public void OnPianoPress()
    {
        if (automatic)
        {
            Debug.LogError("Need to add automatic Piano detection here");
        }
        else
        {
            pianoManager.Active = true;
            pianoManager.ResetDefinition();
            rayCastPlaneFinder.active = true;
        }
    }

    public void OnBongosPress()
    {
        Debug.LogError("Bongos are not supported yet");
    }
}
