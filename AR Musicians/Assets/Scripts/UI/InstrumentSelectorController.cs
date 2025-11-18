using UnityEngine;
using UnityEngine.UI;

public class InstrumentSelectorController : MonoBehaviour
{

    public Button pianoButton;
    public Button bongosButton;

    public void selectPiano()
    {
        ProjectConfig.Settings.instrument = "piano";
    }

    public void selectBongos()
    {
        ProjectConfig.Settings.instrument = "bongos";
    }

    public void onConfiguredNewInstrument()
    {
        if (ProjectConfig.Settings.configured_instruments.Contains("piano"))
        {
            pianoButton.interactable = true;
        }
        if (ProjectConfig.Settings.configured_instruments.Contains("bongos"))
        {
            bongosButton.interactable = true;
        }
    }
}
