using System;
using UnityEngine;

public class BongoStrategy : MonoBehaviour, IGameplayStrategy
{
    [Header("Bongo Settings")]
    public BongosManager BongosManager; // Holds the DefinedCircle data
    public GameObject noteSpherePrefab;
    public float approachTime = 1.5f;
    public float spawnDistance = 0.8f;

    // Tracks the drums defined in BongosManager
    private DefinedCircle? leftDrum;
    private DefinedCircle? rightDrum;

    public event Action OnNoteFinished;

    private void Start()
    {
        BongosManager.OnBongoDefined += HandleBongoDefined;
    }

    private void OnDestroy()
    {
        BongosManager.OnBongoDefined -= HandleBongoDefined;
    }

    private void HandleBongoDefined(DefinedCircle circle)
    {
        if (leftDrum == null) leftDrum = circle;
        else if (rightDrum == null) rightDrum = circle;
    }

    // Interface Implementation
    public float ApproachTime => approachTime;

    public bool IsInstrumentReady()
    {
        return leftDrum.HasValue && rightDrum.HasValue;
    }

    public void PreprocessNote(NoteEvent note)
    {
        // Map Keys to Drums (Simple split)
        // You can make this smarter (e.g., specific track names in MIDI)
        int midi = NoteNameToMidi(note.key);
        note.keyIndex = midi < 60 ? 0 : 1; // 0 = Left, 1 = Right
    }

    public void SpawnNote(NoteEvent note, float gameSpeed)
    {
        bool isLeft = note.keyIndex == 0;
        DefinedCircle targetDrum = isLeft ? leftDrum.Value : rightDrum.Value;

        GameObject noteObj = Instantiate(noteSpherePrefab);

        noteObj.transform.position = targetDrum.Center;

        // Orient the object so its "Up" matches the Drum Normal.
        // This ensures the X/Z drawing plane lies flat on the drum surface.
        if (targetDrum.Normal != Vector3.zero)
        {
            noteObj.transform.rotation = Quaternion.LookRotation(Vector3.forward, targetDrum.Normal);
        }

        Color c = isLeft ? Color.cyan : Color.magenta;
        var arcBehavior = noteObj.GetComponent<BongoArcBehavior>();
        if (arcBehavior == null) arcBehavior = noteObj.AddComponent<BongoArcBehavior>();

        float drumRadius = targetDrum.Radius;

        arcBehavior.Configure(this, isLeft, drumRadius, spawnDistance, approachTime, c);

        // Events
        RhythmGameManager.OnPause += arcBehavior.OnPause;
        RhythmGameManager.OnResume += arcBehavior.OnResume;
        RhythmGameManager.OnQuit += arcBehavior.OnQuit;
    }

    public void NotifyNoteDone()
    {
        OnNoteFinished?.Invoke();
    }

    private int NoteNameToMidi(string note)
    {
        return 60; // Default
    }
}