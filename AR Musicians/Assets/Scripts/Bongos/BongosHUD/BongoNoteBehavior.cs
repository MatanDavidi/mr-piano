using NUnit;
using System;
using UnityEngine;

/// <summary>
/// Equivalent of <see cref="CubeFall"/>, but instead of falling and stretching, it floats from a start position (side) to the target position (drum center).
/// </summary>
public class BongoNoteBehavior : MonoBehaviour
{
    private BongoStrategy strategy;
    private Vector3 start, end;
    private float duration;
    private float startTime;

    public void Configure(BongoStrategy strat, Vector3 s, Vector3 e, float time)
    {
        strategy = strat;
        start = s;
        end = e;
        duration = time;
        startTime = Time.time;
    }

    void Update()
    {
        float t = (Time.time - startTime) / duration;
        if (t > 1.0f)
        {
            strategy.NotifyNoteDone();
            Destroy(gameObject);
            return;
        }

        transform.position = Vector3.Lerp(start, end, t);
    }
}