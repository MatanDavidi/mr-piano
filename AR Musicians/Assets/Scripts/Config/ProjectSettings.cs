using UnityEngine;

[CreateAssetMenu(fileName = "ProjectSettings", menuName = "Config/Project Settings")]
public class ProjectSettings : ScriptableObject
{
    [Header("General Settings")]
    public bool enableMultiplayer = true;
}
