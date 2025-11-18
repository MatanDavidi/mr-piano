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
    public float blockDepth = 0.5f;

    public float gameSpeed = 1f;

    private List<NoteEvent> notes = new List<NoteEvent>();
    private float songStartTime;
    public AudioSource audioSource;

    private bool started = false;
    public void updateSong(PartialSongData songdata)
    {
        Debug.Log("NoteCubeManager updateSong called! Processing song: " + songdata.FileName);

        string path = Application.streamingAssetsPath + "/Music/song.json";

        Assets.Scripts.Songs.MidiUtils.ConvertMidiToJson(Application.streamingAssetsPath + "/Music/" + songdata.FileName, path);

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
        audioSource.clip = Resources.Load<AudioClip>("potc");
        songStartTime = Time.time + fallTime;
        started = true;
        StartCoroutine(PlayAudioWithDelay(fallTime));
    }

    void Update()
    {
        if (!started)
            return;
        if (notes.Count == 0)
        {
            // we finished a song
            onFinished();
            return;
        }
        float elapsed = Time.time - songStartTime;
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
}