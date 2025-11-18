using Meta.XR;
using UnityEngine;
using UnityEngine.Serialization;

public class RayCastPlaneFinder : AbstractPlaneFinder
{
    [Header("Ray Casting")]
    public Transform rightControllerAnchor;
    public EnvironmentRaycastManager raycastManager;

    public bool active = false;

    private void Update()
    {
        if (active && OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            Debug.Log("Pushed the IndexTrigger");
            var ray = new Ray(
                rightControllerAnchor.position,
                rightControllerAnchor.forward
            );

            if (raycastManager.Raycast(ray, out var hit))
            {
                Debug.Log("Hit something");
                CapturePoint(hit.point);
            }
        }
    }
}
