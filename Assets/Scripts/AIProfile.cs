using UnityEngine;

/// <summary>
/// Defines the personality and performance characteristics of an AI racer.
/// Assign to an AIRacer to vary behaviour without modifying code.
/// </summary>
[CreateAssetMenu(menuName = "Racing/AI Profile")]
public class AIProfile : ScriptableObject
{
    [Header("Speed")]
    public float baseSpeed = 60f;
    public float cornerSpeedMultiplier = 0.4f;

    [Header("Behavior")]
    [Range(0f, 1f)]
    public float skillLevel = 0.5f;
    public float lookAheadDistance = 0.05f;
    public float lateralVariation = 0.5f;

    [Header("Visual")]
    public float rollMultiplier = 1f;
    public float pitchMultiplier = 1f;
}
