using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.InputSystem;

[ExecuteAlways]
public class PlaneController : MonoBehaviour
{
    [Header("Plane Dimensions")]
    public float width = 10f;
    public float height = 1f;

    [Header("Piano Setup")]
    public int totalKeys = 88;
    public string leftmostKey = "A0";

    private int leftmostKeyIndex = 0;
    public float whiteToBlackRatio = 1.66f;

    private List<Vector3> localKeyCenters = new List<Vector3>(); // Positions in *Local* Space!
    private List<float> keyWidths = new List<float>();
    private static string[] noteNames =
            { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" }; // used to map note names to key indices

    void Start()
    {
        leftmostKeyIndex = NoteNameToKeyIndex(leftmostKey);
        BuildKeyLayout();
    }
    void Update()
    {
        transform.localScale = new Vector3(width, height, 1f);

    }

    // Determine which keys are white or black and space them correctly
    void BuildKeyLayout()
    {
        localKeyCenters.Clear();
        keyWidths.Clear();

        // Setup unit width for both types of keys
        float whiteWidthUnit = 1f;
        float blackWidthUnit = whiteWidthUnit / whiteToBlackRatio;

        // Pattern: true = white, false = black
        bool[] pattern = {
            true,false,true,false,true,true,false,true,false,true,false,true
        };

        // Count total width units first
        float totalUnits = 0f;
        for (int i = 0; i < totalKeys; i++)
        {
            bool isWhite = pattern[(leftmostKeyIndex + i) % 12]; // wrap around as it repeats every 12 keys
            totalUnits += isWhite ? whiteWidthUnit : blackWidthUnit;
        }

        float unitToLocal = 1f / totalUnits;

        // Compute keycenter positions with the corresponding width
        float currentX = -0.5f;
        for (int i = 0; i < totalKeys; i++)
        {
            bool isWhite = pattern[(leftmostKeyIndex + i) % 12];
            float wUnits = isWhite ? whiteWidthUnit : blackWidthUnit;
            float worldWidth = wUnits * unitToLocal; // width of the key in local space

            float centerX = currentX + worldWidth / 2f;
            localKeyCenters.Add(new Vector3(centerX, 0.5f, 0f)); // spawn at the top of the plane (total height is 1 in local space, since center is (0,0), 0.5 is at the top)
            keyWidths.Add(worldWidth);

            currentX += worldWidth;
        }
    }

    // check if the key at keyIndex is a white key
    public bool IsWhiteKey(int keyIndex)
    {
        bool[] pattern = { true, false, true, false, true, true, false, true, false, true, false, true };
        return pattern[keyIndex % 12];
    }

    public float GetLocalKeyWidth(int keyIndex)
    {
        return keyWidths[keyIndex - leftmostKeyIndex] / width;
    }

    // Return the local position of the key
    public Vector3 GetLocalKeyPosition(int keyIndex)
    {
        if (localKeyCenters.Count == 0) BuildKeyLayout();

        keyIndex = Mathf.Clamp(keyIndex, leftmostKeyIndex, totalKeys + leftmostKeyIndex - 1); //TODO: What happens if we have a piano that doesn't have a certain key?
        Vector3 localPos = localKeyCenters[keyIndex - leftmostKeyIndex];
        return localPos;
    }

    public int NoteNameToKeyIndex(string note)
    {
        string namePart = note.Substring(0, note.Length - 1); // everything but the last character is part of the name
        int octave = int.Parse(note.Substring(note.Length - 1)); // gives the octave

        int noteNumber = System.Array.IndexOf(noteNames, namePart);

        // Piano key index = noteNumber + 12 * octave number
        int keyIndex = noteNumber + 12 * octave;
        keyIndex = Math.Clamp(keyIndex, leftmostKeyIndex, totalKeys - 1);
        return keyIndex;
    }
}
