using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Defines a closed circuit path using Catmull-Rom spline interpolation between waypoints.
/// </summary>
public class SplinePath : MonoBehaviour
{
    [SerializeField] private List<Transform> waypoints = new List<Transform>();

    private const int LengthSampleCount = 200;
    private const int GizmoSegmentsPerWaypoint = 20;

    /// <summary>Total arc length of the circuit in world units.</summary>
    public float TotalLength { get; private set; }

    private void Awake()
    {
        CalculateTotalLength();
    }

    private void OnValidate()
    {
        CalculateTotalLength();
    }

    private void CalculateTotalLength()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        TotalLength = 0f;
        Vector3 previous = GetPosition(0f);
        for (int i = 1; i <= LengthSampleCount; i++)
        {
            float t = (float)i / LengthSampleCount;
            Vector3 current = GetPosition(t);
            TotalLength += Vector3.Distance(previous, current);
            previous = current;
        }
    }

    /// <summary>
    /// Returns the world position on the circuit for a given progress value (0 to 1).
    /// 0 and 1 both map to the first waypoint — the circuit loops seamlessly.
    /// </summary>
    public Vector3 GetPosition(float progress)
    {
        if (waypoints == null || waypoints.Count == 0) return Vector3.zero;
        if (waypoints.Count == 1) return waypoints[0].position;

        progress = Mathf.Repeat(progress, 1f);
        int count = waypoints.Count;
        float scaledT = progress * count;
        int index = Mathf.FloorToInt(scaledT);
        float localT = scaledT - index;

        Vector3 p0 = GetWaypointPosition(index - 1);
        Vector3 p1 = GetWaypointPosition(index);
        Vector3 p2 = GetWaypointPosition(index + 1);
        Vector3 p3 = GetWaypointPosition(index + 2);

        return CatmullRom(p0, p1, p2, p3, localT);
    }

    /// <summary>
    /// Returns the normalised tangent direction of the circuit at a given progress (0 to 1).
    /// </summary>
    public Vector3 GetDirection(float progress)
    {
        const float epsilon = 0.0001f;
        Vector3 ahead = GetPosition(progress + epsilon);
        Vector3 behind = GetPosition(progress - epsilon);
        return (ahead - behind).normalized;
    }

    /// <summary>
    /// Returns the curvature at a given progress (0 to 1).
    /// 0 = straight line, 1 = tightest possible turn.
    /// </summary>
    public float GetCurvature(float progress)
    {
        const float sampleOffset = 0.01f;
        Vector3 dirBefore = GetDirection(progress - sampleOffset);
        Vector3 dirAfter = GetDirection(progress + sampleOffset);
        return 1f - Mathf.Clamp01(Vector3.Dot(dirBefore, dirAfter));
    }

    // --- Private helpers ---

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            2f * p1 +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private Vector3 GetWaypointPosition(int index)
    {
        int count = waypoints.Count;
        int wrappedIndex = ((index % count) + count) % count;
        Transform wp = waypoints[wrappedIndex];
        return wp != null ? wp.position : Vector3.zero;
    }

    // --- Editor visualization ---

    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Count < 2) return;

        Gizmos.color = Color.yellow;
        foreach (Transform wp in waypoints)
        {
            if (wp != null)
                Gizmos.DrawSphere(wp.position, 0.5f);
        }

        Gizmos.color = Color.green;
        int totalSegments = waypoints.Count * GizmoSegmentsPerWaypoint;
        Vector3 previous = GetPosition(0f);
        for (int i = 1; i <= totalSegments; i++)
        {
            float t = (float)i / totalSegments;
            Vector3 current = GetPosition(t);
            Gizmos.DrawLine(previous, current);
            previous = current;
        }
    }
}
