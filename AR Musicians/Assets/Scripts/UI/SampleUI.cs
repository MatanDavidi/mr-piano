using System;
using UnityEngine;

public class SampleUI : MonoBehaviour
{
    public static void PrintOnClick()
    {
        Debug.Log($"Clicked at timestamp {System.DateTime.Now.ToString("F")}");
    }
}
