using UnityEngine;
using System;

public class CubeFall : MonoBehaviour
{
    public PlaneController plane;
    public float fallTime;      // total seconds to reach plane
    public float startTime;     // when the note should hit the plane
    public float duration;
    public int keyIndex;
    public float pauseTime = 0;
    public static event Action deleteCube;

    public float origBlockHeight, blockDepth;
    private float spawnTime;
    private float keyWidth;
    private bool done = false;
    private bool paused = false;
    void Start()
    {
        keyWidth = plane.GetLocalKeyWidth(keyIndex);
        spawnTime = Time.time;
    }

    public void OnPause()
    {
        paused = true;
    }

    public void OnResume(float newPauseTime)
    {
        pauseTime += newPauseTime;
        paused = false;
    }

    private void unsubscribe()
    {
        NoteCubeManager.OnPause -= OnPause;
        NoteCubeManager.OnQuit -= OnQuit;
        NoteCubeManager.OnResume -= OnResume;
    }
    public void OnQuit()
    {
        unsubscribe();
        deleteCube?.Invoke();
        Destroy(gameObject);
    }

    void Update()
    {
        if (paused)
            return;
        float timeSinceSpawn = Time.time - spawnTime - pauseTime;
        if (timeSinceSpawn >= fallTime)
        {
            Stretch();  // we already reached the bottom, we have to handle this differently now
            return;
        }
        float t = Mathf.Clamp01(timeSinceSpawn / fallTime);

        if (timeSinceSpawn < 0)
            t = 0;
        // center moves from origBlockHeight/2 above the plane to origBlockHeight/2 above the bottom of the plane.
        Vector3 localKeyPos = plane.GetLocalKeyPosition(keyIndex);
        // The blockheight should not be scaled by the plane height, therefore we divide by the plane height to cancel it out 
        Vector3 top = plane.transform.TransformPoint(localKeyPos + Vector3.up * (origBlockHeight / 2f / plane.height));
        Vector3 bottom = top - plane.transform.up * plane.height;
        transform.position = Vector3.Lerp(top, bottom, t);
        // match plane rotation
        transform.rotation = plane.transform.rotation;

        transform.localScale = new Vector3(keyWidth * plane.width, origBlockHeight, blockDepth);
    }

    void Stretch()
    {
        float timeSinceSpawn = Time.time - spawnTime - pauseTime;
        float timeSinceBottom = timeSinceSpawn - fallTime;
        float t = Mathf.Clamp01(timeSinceBottom / duration);

        Vector3 localKeyPos = plane.GetLocalKeyPosition(keyIndex);
        // The blockheight should not be scaled by the plane height, therefore we divide by the plane height to cancel it out 
        // The start is at origBlockHeight/2 above the bottom of the plane, as this is the moment we touch the bottom
        Vector3 start = plane.transform.TransformPoint(localKeyPos + Vector3.up * (origBlockHeight / 2f / plane.height)) - plane.transform.up * plane.height;
        // We end at the bottom of the plane, or origBlockHeight/2 below the start
        Vector3 end = start - plane.transform.up * origBlockHeight / 2f;
        transform.position = Vector3.Lerp(start, end, t);
        // Now we need to squish the blockHeight, from origBlockHeight to 0
        float blockHeight = origBlockHeight * (1f - t);
        // match plane rotation
        transform.rotation = plane.transform.rotation;

        transform.localScale = new Vector3(keyWidth * plane.width, blockHeight, blockDepth);

        if (t >= 1f)
        {
            if (!done)
            {
                unsubscribe();
                deleteCube?.Invoke();
                Destroy(gameObject);
            }
            done = true;
        }

    }
}
