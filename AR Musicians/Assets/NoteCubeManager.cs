using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;


public class NoteCubeManager : MonoBehaviour
{

    public PlaneController plane;
    public GameObject noteCubePrefab;

    public float fallTime = 2f; // seconds for cube to travel from top to final destination

    private List<NoteEvent> notes = new List<NoteEvent>();
    private float songStartTime;



    void Start()
    {
        string path = Application.dataPath + "/song.json";
        if (File.Exists(path))
        {
            string json = File.ReadAllText(path);
            NoteEventList noteList = JsonUtility.FromJson<NoteEventList>("{\"events\":" + json + "}");
            foreach (var n in noteList.events)
            {
                n.keyIndex = plane.NoteNameToKeyIndex(n.key);
            }
            notes = new List<NoteEvent>(noteList.events);
        }
        else
        {
            Debug.LogError("No such file exists: " + path);
            return;
        }

        songStartTime = Time.time + 1;
    }

    void Update()
    {
        float elapsed = Time.time - songStartTime;
        if (elapsed < 0)
            return;
        // Spawn notes
        foreach (var note in notes.ToArray())
        {
            // Calculate when we need to spawn: spawn early so it reaches plane in 'fallTime' seconds
            if (elapsed >= note.time - fallTime)
            {
                SpawnCube(note);
                notes.Remove(note); // remove from list so we don’t spawn again
            }
        }
    }

    void SpawnCube(NoteEvent note)
    {
        int keyIndex = note.keyIndex;

        // blockHeight == velocity * duration
        // velocity ==  plane.height / fallTime
        float velocity = plane.height / fallTime;
        float blockHeight = velocity * note.duration;
        float blockDepth = 0.5f;

        GameObject cube = Instantiate(noteCubePrefab);
        cube.transform.position = plane.transform.TransformPoint(plane.GetLocalKeyPosition(keyIndex) + Vector3.up * (blockHeight / 2f));
        cube.transform.rotation = plane.transform.rotation;

        float keyWidth = plane.GetLocalKeyWidth(keyIndex);

        // world-scale: X=keyWidth, Y=blockHeight, Z=blockDepth
        cube.transform.localScale = new Vector3(keyWidth, blockHeight, blockDepth);



        // Color
        Renderer rend = cube.GetComponent<Renderer>();
        if (rend != null)
            rend.material.color = plane.IsWhiteKey(keyIndex) ? Color.white : Color.black;

        var fall = cube.AddComponent<CubeFall>();
        fall.fallTime = fallTime;
        fall.startTime = note.time;
        fall.plane = plane;
        fall.keyIndex = keyIndex;
        fall.blockHeight = blockHeight;
        fall.blockDepth = blockDepth;
        fall.duration = note.duration;
    }

}
