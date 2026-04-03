using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Simplified wrapper around Unity Splines for the AI racing system.
/// Attach to the same GameObject that holds a SplineContainer.
/// </summary>
[RequireComponent(typeof(SplineContainer))]
public class SplinePath : MonoBehaviour
{
    private SplineContainer splineContainer;

    private void Awake()
    {
        splineContainer = GetComponent<SplineContainer>();
    }

    /// <summary>Returns the world position on the spline at a given progress (0 to 1).</summary>
    public Vector3 GetPosition(float progress)
    {
        EnsureContainer();
        progress = Mathf.Repeat(progress, 1f);
        return (Vector3)splineContainer.EvaluatePosition(progress);
    }

    /// <summary>Returns the normalised tangent direction at a given progress (0 to 1).</summary>
    public Vector3 GetDirection(float progress)
    {
        EnsureContainer();
        progress = Mathf.Repeat(progress, 1f);
        Vector3 dir = (Vector3)splineContainer.EvaluateTangent(progress);
        return dir.sqrMagnitude > 0f ? dir.normalized : Vector3.forward;
    }

    /// <summary>
    /// Returns a curvature value from 0 (straight) to 1 (tight turn at 90 degrees or more).
    /// </summary>
    public float GetCurvature(float progress)
    {
        const float sampleOffset = 0.01f;
        Vector3 dirBefore = GetDirection(progress - sampleOffset);
        Vector3 dirAfter  = GetDirection(progress + sampleOffset);
        return Mathf.Clamp01(Vector3.Angle(dirBefore, dirAfter) / 90f);
    }

    /// <summary>Returns the total arc length of the spline in world units.</summary>
    public float GetTotalLength()
    {
        EnsureContainer();
        return splineContainer.Spline != null ? splineContainer.Spline.GetLength() : 0f;
    }

    private void EnsureContainer()
    {
        if (splineContainer == null)
            splineContainer = GetComponent<SplineContainer>();
    }
}
