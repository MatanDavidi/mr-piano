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
    public GameObject noteCubePrefab;

    public float fallTime = 2f; // seconds for cube to travel from top to final destination
    public float blockDepth = 0.5f;

    public float gameSpeed = 1f;

    private List<NoteEvent> notes = new List<NoteEvent>();
    private float songStartTime;
    public AudioSource audioSource;

    private static string[] noteNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

    void Start()
    {
        string path = Application.dataPath + "/Music/song.json";

        ConvertMidiToJson(Application.dataPath + "/Music/potc.mid", path);

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
        audioSource.clip = Resources.Load<AudioClip>("potc");
        songStartTime = Time.time + fallTime;
        StartCoroutine(PlayAudioWithDelay(fallTime));

    }
    IEnumerator PlayAudioWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        audioSource.pitch = gameSpeed;
        audioSource.Play();
        songStartTime = Time.time;
    }

    void Update()
    {
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


    void ConvertMidiToJson(string midiPath, string outputPath)
    {
        var midiFile = MidiFile.Read(midiPath);

        // Get tempo map (for converting ticks to seconds)
        var tempoMap = midiFile.GetTempoMap();

        // Extract note events
        var notes = midiFile.GetNotes();

        List<NoteEvent> noteList = new List<NoteEvent>();

        foreach (var note in notes)
        {
            // Convert MIDI pitch to note name (like "C4")
            string noteName = GetNoteName(note.NoteNumber);

            // Convert time and duration to seconds
            double startSec = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap).TotalSeconds;
            double durSec = LengthConverter.ConvertTo<MetricTimeSpan>(note.Length, note.Time, tempoMap).TotalSeconds;

            noteList.Add(new NoteEvent
            {
                key = noteName,
                time = (float)startSec,
                duration = (float)durSec
            });
        }

        // Convert to JSON
        string json = JsonConvert.SerializeObject(noteList, Formatting.Indented);


        // Save JSON
        File.WriteAllText(outputPath, json);
    }

    public static string GetNoteName(int midiNumber)
    {
        int octave = midiNumber / 12;
        string note = noteNames[midiNumber % 12];
        return note + octave;
    }
}