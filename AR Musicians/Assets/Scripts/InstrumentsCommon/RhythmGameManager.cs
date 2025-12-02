using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class RhythmGameManager : MonoBehaviour
{
    [Header("References")]
    public MenuControllerGeneric menucontroller;
    public AudioSource audioSource;

    // This handles the specific logic for Piano OR Bongos
    private IGameplayStrategy currentStrategy;

    [Header("General Settings")]
    public float gameSpeed = 1f;

    private List<NoteEvent> notes = new List<NoteEvent>();
    private float songStartTime;
    private bool started = false;

    // We count active notes to know when the song is truly "done" visually
    private int activeNoteCount = 0;

    // Call this from MenuController when selecting the instrument
    public void SetStrategy(IGameplayStrategy strategy)
    {
        // Unsubscribe from old strategy if exists
        if (currentStrategy != null)
        {
            currentStrategy.OnNoteFinished -= HandleNoteFinished;
        }

        currentStrategy = strategy;

        if (currentStrategy != null)
        {
            currentStrategy.OnNoteFinished += HandleNoteFinished;
        }
    }

    private void HandleNoteFinished()
    {
        activeNoteCount--;
    }

    public void Play()
    {
        if (currentStrategy == null || !currentStrategy.IsInstrumentReady())
        {
            Debug.LogError("Cannot play: Instrument not defined or Strategy missing.");
            return;
        }

        // Use the strategy's specific lookahead time (FallTime or ApproachTime)
        songStartTime = Time.time + currentStrategy.ApproachTime;
        started = true;

        StartCoroutine(PlayAudioWithDelay(currentStrategy.ApproachTime));
    }

    IEnumerator PlayAudioWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        //audioSource.pitch = gameSpeed;
        //audioSource.Play();
        // Resync to ensure exact timing
        songStartTime = Time.time;
    }

    public void updateSong(SongData songdata)
    {
        StartCoroutine(updateSongRoutine(songdata));
    }

    public IEnumerator updateSongRoutine(SongData songdata)
    {
        Debug.Log("Processing song: " + songdata.PartialSongData.JsonPath);
        string fullPath = Path.Combine(Application.streamingAssetsPath, songdata.PartialSongData.JsonPath);
        string jsonContent = null;

        // Android/Desktop compatible loading
#if UNITY_ANDROID && !UNITY_EDITOR
        using (UnityWebRequest www = UnityWebRequest.Get(fullPath))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success) jsonContent = www.downloadHandler.text;
        }
#else
        if (File.Exists(fullPath)) jsonContent = File.ReadAllText(fullPath);
        yield return null;
#endif

        if (!string.IsNullOrEmpty(jsonContent))
        {
            notes = JsonConvert.DeserializeObject<List<NoteEvent>>(jsonContent);

            // DELEGATE: Ask the strategy to interpret the notes (Piano keys vs Bongo sides)
            foreach (var n in notes)
            {
                if (currentStrategy != null)
                    currentStrategy.PreprocessNote(n);
            }
        }
    }

    void Update()
    {
        if (!started) return;

        // Check if song is over
        if (notes.Count == 0 && activeNoteCount <= 0)
        {
            OnFinished();
            return;
        }

        float elapsed = Time.time - songStartTime;

        foreach (var note in notes.ToArray())
        {
            // DELEGATE: Use strategy.ApproachTime instead of hardcoded fallTime
            if (elapsed >= note.time / gameSpeed - currentStrategy.ApproachTime)
            {
                note.duration /= gameSpeed;

                // DELEGATE: The strategy spawns the actual object
                activeNoteCount++;
                currentStrategy.SpawnNote(note, gameSpeed);

                notes.Remove(note);
            }
        }
    }

    public void OnFinished()
    {
        started = false;
        menucontroller.ShowSongMenu();
    }
}