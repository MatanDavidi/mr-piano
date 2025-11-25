using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Newtonsoft.Json;

public class NoteCubeManager : MonoBehaviour
{

    public PlaneController plane;
    public MenuController menucontroller;
    public GameObject noteCubePrefab;

    public float fallTime = 2f; // seconds for cube to travel from top to final destination
    public float blockDepth = 0.001f;

    public float gameSpeed = 1f;

    private List<NoteEvent> notes = new List<NoteEvent>();
    private float songStartTime;
    public AudioSource audioSource;
    private bool started = false;

    private int cubeCount = 0;

    public void Start()
    {
        PianoManager.OnPlaneDefined += updatePlane;
        CubeFall.deleteCube += onDeleteCube;

    }

    private void updatePlane(DefinedPlane definedPlane)
    {
        // --- 1. Define the Perpendicular Plane ---
        // The center of our new plane will be the middle point of the top edge.
        Vector3 distToNewCenter = definedPlane.Plane.normal * ((definedPlane.Corner3 - definedPlane.Corner2) / 2f).magnitude;
        Vector3 center = definedPlane.Center + distToNewCenter;

        // For this computation, we assume the following:
        // C4---C3
        //  |   |
        // C1---C2
        // The normal of our new plane will be parallel to one of the edges of the original plane.
        // This makes it "perpendicular" in the sense that it rises up like a wall from that edge.
        Vector3 perpNormal = (definedPlane.Corner2 - definedPlane.Corner3).normalized;

        // --- 2. Define a Rectangle to Visualize the Plane ---
        // A plane is infinite, so we define a rectangle to draw.
        // The "up" direction for our new rectangle is the normal of the original plane.
        Vector3 rectUpDir = definedPlane.Plane.normal.normalized;

        // The "right" direction for our rectangle is perpendicular to both its up direction and its normal.
        // We can find this with the cross product.
        Vector3 rectRightDir = Vector3.Cross(perpNormal, rectUpDir).normalized;

        // Let's define the size of the rectangle based on the original plane's dimensions.
        float rectHeight = Vector3.Distance(definedPlane.Corner1, definedPlane.Corner4);
        float rectWidth = Vector3.Distance(definedPlane.Corner1, definedPlane.Corner2);

        // Calculate the four corners of the visualization rectangle.
        Vector3[] perpCorners = new Vector3[4];
        perpCorners[0] = center + rectRightDir * rectWidth / 2f + rectUpDir * rectHeight / 2f;
        perpCorners[1] = center - rectRightDir * rectWidth / 2f + rectUpDir * rectHeight / 2f;
        perpCorners[2] = center - rectRightDir * rectWidth / 2f - rectUpDir * rectHeight / 2f;
        perpCorners[3] = center + rectRightDir * rectWidth / 2f - rectUpDir * rectHeight / 2f;

        // --- 3. Draw the Perpendicular Rectangle ---
        //perpendicularLineRenderer.enabled = true;
        //perpendicularLineRenderer.positionCount = 4;
        //perpendicularLineRenderer.SetPositions(perpCorners);
        //perpendicularLineRenderer.loop = true;

        // --- 4. Place the Object ---

        float planeLength = (definedPlane.Corner1 - definedPlane.Corner2).magnitude;
        float planeHeight = planeLength / 2;

        // The object should be placed at the center of the new plane.
        Vector3 edgeCenterPoint = (definedPlane.Corner2 + definedPlane.Corner1) / 2;
        Vector3 position = edgeCenterPoint + definedPlane.Plane.normal * planeHeight / 2;
        // Vector3 position = edgeCenterPoint;

        // The object's "forward" direction should face along the new plane's normal.
        // Its "up" direction should align with the "up" direction of the rectangle we calculated.
        Quaternion rotation = Quaternion.LookRotation(perpNormal, rectUpDir);

        // Instantiate the object with the calculated position and rotation.
        plane.width = planeLength;
        plane.height = planeHeight;
        plane.transform.position = position;
        plane.transform.rotation = rotation;
        plane.reInit();
    }

    public void updateSong(SongData songdata)
    {
        Debug.Log("NoteCubeManager updateSong called! Processing song: " + songdata.PartialSongData.FileName);

        string path = Application.streamingAssetsPath + "/Music/song.json";

        Assets.Scripts.Songs.MidiUtils.ConvertMidiToJson(Application.streamingAssetsPath + "/Music/" + songdata.PartialSongData.FileName, path);

        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            notes = JsonConvert.DeserializeObject<List<NoteEvent>>(json);
            foreach (var n in notes)
            {
                n.keyIndex = plane.NoteNameToKeyIndex(n.key);
            }
        }
        else
        {
            Debug.LogError("No such file exists: " + path);
            return;
        }
    }
    IEnumerator PlayAudioWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.pitch = gameSpeed;
        audioSource.Play();
        songStartTime = Time.time;
    }

    public void Play()
    {
        //audioSource.clip = Resources.Load<AudioClip>("potc");
        songStartTime = Time.time + fallTime;
        started = true;
        //StartCoroutine(PlayAudioWithDelay(fallTime));
    }

    void Update()
    {
        if (!started)
            return;

        float elapsed = Time.time - songStartTime;
        if (notes.Count == 0)
        {
            if (cubeCount == 0)
            {
                // we finished a song
                onFinished();
                return;
            }
            else
            {
                Debug.Log("Still waiting for " + cubeCount + " blocks to finish falling");
            }
        }
        // Spawn notes
        foreach (var note in notes.ToArray())
        {
            // Spawn early so it reaches plane in fallTime seconds
            if (elapsed >= note.time / gameSpeed - fallTime)
            {
                note.duration /= gameSpeed;
                SpawnCube(note);
                notes.Remove(note);
            }
        }
    }

    public void onFinished()
    {
        started = false;
        menucontroller.ShowSongMenu();
        return;
    }
    void SpawnCube(NoteEvent note)
    {
        cubeCount++;
        int keyIndex = note.keyIndex;

        // blockHeight == velocity * duration
        // velocity ==  plane.height / fallTime
        float velocity = plane.height / fallTime;
        float blockHeight = velocity * note.duration;


        GameObject cube = Instantiate(noteCubePrefab);
        cube.transform.position = plane.transform.TransformPoint(plane.GetLocalKeyPosition(keyIndex) + Vector3.up * (blockHeight / 2f / plane.height));
        cube.transform.rotation = plane.transform.rotation;

        float keyWidth = plane.GetLocalKeyWidth(keyIndex);

        // world-scale: X=keyWidth, Y=blockHeight, Z=blockDepth
        cube.transform.localScale = new Vector3(keyWidth * plane.width, blockHeight, blockDepth);



        // Color
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = plane.IsWhiteKey(keyIndex) ? Color.white : Color.black;

        var fall = cube.AddComponent<CubeFall>();
        fall.fallTime = fallTime;
        fall.startTime = note.time;
        fall.plane = plane;
        fall.keyIndex = keyIndex;
        fall.origBlockHeight = blockHeight;
        fall.blockDepth = blockDepth;
        fall.duration = note.duration;
    }

    void onDeleteCube()
    {
        cubeCount--;
    }
}