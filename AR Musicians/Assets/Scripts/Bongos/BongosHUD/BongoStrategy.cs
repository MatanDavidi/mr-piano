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

        GameObject sphere = Instantiate(noteSpherePrefab);

        // Visual Setup
        if (sphere.TryGetComponent<Renderer>(out var r)) r.material.color = isLeft ? Color.cyan : Color.magenta;

        // Calculate Start/End positions
        Vector3 targetPos = targetDrum.Center;

        // Calculate a side vector relative to the drum's normal
        Vector3 rightDir = Vector3.Cross(Vector3.up, targetDrum.Normal).normalized;
        if (rightDir.sqrMagnitude < 0.01f) rightDir = Vector3.right;

        Vector3 offset = (isLeft ? -rightDir : rightDir) * spawnDistance;
        Vector3 startPos = targetPos + offset;
        sphere.transform.position = startPos;

        // Add Behavior
        var behavior = sphere.AddComponent<BongoNoteBehavior>();
        behavior.Configure(this, startPos, targetPos, approachTime);
        RhythmGameManager.OnPause += behavior.OnPause;
        RhythmGameManager.OnResume += behavior.OnResume;
        RhythmGameManager.OnQuit += behavior.OnQuit;
    }

    public void NotifyNoteDone()
    {
        OnNoteFinished?.Invoke();
    }

    private int NoteNameToMidi(string note)
    {
        // Simple helper or reuse existing logic
        return 60; // Default
    }
}