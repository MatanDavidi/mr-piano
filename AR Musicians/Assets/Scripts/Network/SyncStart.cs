using UnityEngine;
using Photon.Pun;

public class SyncStart : MonoBehaviourPun
{
    private bool started = false;

    public GameObject perpendicularPlaneFinderObject;
    private NoteCubeManager Manager;

    [PunRPC]
    public void StartExperience()
    {
        PerpendicularPlaneFinder finder = perpendicularPlaneFinderObject.GetComponent<PerpendicularPlaneFinder>();
        PlaneController pianoHUD = finder.objectToPlaceOnPlane.GetComponent<PlaneController>();
        Debug.Log(pianoHUD);
        Manager = pianoHUD.GetComponentInChildren<NoteCubeManager>();
        Debug.Log(Manager);
        Debug.Log("Experience started!");
        started = true;
        Manager.Play();
    }

    public void TriggerStart()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC("StartExperience", RpcTarget.All);
        }
    }

    void Update()
    {
        if (ProjectConfig.Settings.enableMultiplayer)
        {
            // Press Spacebar to trigger the sync
            if (!started && Input.GetKeyDown(KeyCode.Space))
            {
                TriggerStart();
            }
        }
    }
}
