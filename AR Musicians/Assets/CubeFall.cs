using UnityEngine;

public class CubeFall : MonoBehaviour
{
    public PlaneController plane;
    public float fallTime;      // total seconds to reach plane
    public float startTime;     // when the note should hit the plane
    public float duration;
    public int keyIndex;
    public float blockHeight, blockDepth;
    private float spawnTime;
    private float keyWidth;
    void Start()
    {
        keyWidth = plane.GetLocalKeyWidth(keyIndex);
        spawnTime = Time.time;
    }

    void Update()
    {

        float timeSinceSpawn = Time.time - spawnTime;
        float t = Mathf.Clamp01(timeSinceSpawn / (fallTime + duration));

        if (timeSinceSpawn < 0)
            t = 0;

        // center moves from blockHeight/2 above the plane to blockHeight/2 below the plane. This way we wait out the full duration of the note
        Vector3 localKeyPos = plane.GetLocalKeyPosition(keyIndex);
        // The blockheight should not be scaled by the plane height, therefore we divide by the plane height to cancel it out 
        Vector3 top = plane.transform.TransformPoint(localKeyPos + Vector3.up * (blockHeight / 2f / plane.height));
        Vector3 bottom = top - plane.transform.up * plane.height - plane.transform.up * (blockHeight / 2f / plane.height);
        transform.position = Vector3.Lerp(top, bottom, t);
        // match plane rotation
        transform.rotation = plane.transform.rotation;
        // TODO: Reduce block height instead of moving blocks downwards in the end, such that the blocks don't go below the plane
        transform.localScale = new Vector3(keyWidth * plane.width, blockHeight, blockDepth);

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }
}
