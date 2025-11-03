using Meta.XR;
using UnityEngine;
using UnityEngine.Serialization;

public class RayCastPlaneFinder : AbstractPlaneFinder
{
    [Header("Ray Casting")]
    public Transform rightControllerAnchor;
    public EnvironmentRaycastManager raycastManager;

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.RIndexTrigger))
        {
            var ray = new Ray(
                rightControllerAnchor.position,
                rightControllerAnchor.forward
            );

            if (raycastManager.Raycast(ray, out var hit))
            {
                CapturePoint(hit.point);
            }
        }
    }
}
