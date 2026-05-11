using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

namespace Racebird.TrackBuilding
{
    /// <summary>
    /// Assembles a racing circuit from TrackSegment prefabs and generates a spline from the result.
    /// Place on an empty GameObject acting as the track root.
    /// </summary>
    public class TrackBuilder : MonoBehaviour
    {
        private const float DEFAULT_SEGMENT_SCALE = 5f;
        private const float CLOSE_LOOP_DISTANCE_THRESHOLD = 50f;

        [Header("Segment Library")]
        [SerializeField] private List<TrackSegment> segmentPrefabs = new List<TrackSegment>();

        [Header("Track")]
        [SerializeField] private SplinePath splinePath;
        [SerializeField] private List<TrackSegment> placedSegments = new List<TrackSegment>();

        [Header("State")]
        [SerializeField] private bool isLoopClosed;

        /// <summary>Whether the circuit has been marked as a closed loop.</summary>
        public bool IsLoopClosed => isLoopClosed;

        /// <summary>Number of segments currently placed.</summary>
        public int SegmentCount => placedSegments.Count;

        /// <summary>Available segment prefabs.</summary>
        public List<TrackSegment> SegmentPrefabs => segmentPrefabs;

        /// <summary>
        /// Instantiates a segment prefab and snaps it to the end of the current track.
        /// </summary>
        /// <param name="prefabIndex">Index into the segmentPrefabs list.</param>
        public void AddSegment(int prefabIndex)
        {
            if (prefabIndex < 0 || prefabIndex >= segmentPrefabs.Count)
            {
                Debug.LogError($"[TrackBuilder] Invalid prefab index: {prefabIndex}");
                return;
            }

            TrackSegment prefab = segmentPrefabs[prefabIndex];
            if (prefab == null)
            {
                Debug.LogError($"[TrackBuilder] Prefab at index {prefabIndex} is null.");
                return;
            }

#if UNITY_EDITOR
            TrackSegment instance = (TrackSegment)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, transform);
#else
            TrackSegment instance = Instantiate(prefab, transform);
#endif

            instance.transform.localScale = Vector3.one * DEFAULT_SEGMENT_SCALE;
            instance.gameObject.name = $"{prefab.gameObject.name}_{placedSegments.Count}";

            if (placedSegments.Count == 0)
            {
                // First segment: place at TrackBuilder position
                instance.transform.position = transform.position;
                instance.transform.rotation = transform.rotation;
            }
            else
            {
                // Snap entry of new segment to exit of last segment
                TrackSegment lastSegment = placedSegments[placedSegments.Count - 1];
                SnapSegment(instance, lastSegment.exitPoint);
            }

            placedSegments.Add(instance);
            isLoopClosed = false;

            Debug.Log($"[TrackBuilder] Added segment '{instance.gameObject.name}' (total: {placedSegments.Count})");
        }

        /// <summary>
        /// Removes and destroys the last placed segment.
        /// </summary>
        public void RemoveLastSegment()
        {
            if (placedSegments.Count == 0)
            {
                Debug.LogWarning("[TrackBuilder] No segments to remove.");
                return;
            }

            TrackSegment last = placedSegments[placedSegments.Count - 1];
            placedSegments.RemoveAt(placedSegments.Count - 1);

            if (Application.isPlaying)
                Destroy(last.gameObject);
            else
                DestroyImmediate(last.gameObject);

            isLoopClosed = false;

            Debug.Log($"[TrackBuilder] Removed last segment (remaining: {placedSegments.Count})");
        }

        /// <summary>
        /// Checks whether the circuit can be closed (exit of last near entry of first).
        /// </summary>
        public void CloseLoop()
        {
            if (placedSegments.Count < 2)
            {
                Debug.LogWarning("[TrackBuilder] Need at least 2 segments to close a loop.");
                return;
            }

            Transform lastExit = placedSegments[placedSegments.Count - 1].exitPoint;
            Transform firstEntry = placedSegments[0].entryPoint;
            float distance = Vector3.Distance(lastExit.position, firstEntry.position);

            if (distance > CLOSE_LOOP_DISTANCE_THRESHOLD)
            {
                Debug.LogWarning($"[TrackBuilder] Loop gap too large: {distance:F1}m (threshold: {CLOSE_LOOP_DISTANCE_THRESHOLD}m). Adjust segments before closing.");
                return;
            }

            isLoopClosed = true;
            Debug.Log($"[TrackBuilder] Loop closed (gap: {distance:F1}m)");
        }

        /// <summary>
        /// Generates a spline from all placed segments' entry/exit points.
        /// </summary>
        public void GenerateSpline()
        {
            if (placedSegments.Count == 0)
            {
                Debug.LogWarning("[TrackBuilder] No segments placed. Cannot generate spline.");
                return;
            }

            if (splinePath == null)
            {
                Debug.LogError("[TrackBuilder] SplinePath reference is not assigned.");
                return;
            }

            SplineContainer container = splinePath.GetComponent<SplineContainer>();
            if (container == null)
            {
                Debug.LogError("[TrackBuilder] No SplineContainer found on the SplinePath GameObject.");
                return;
            }

            Spline spline = container.Spline;
            spline.Clear();

            for (int i = 0; i < placedSegments.Count; i++)
            {
                TrackSegment segment = placedSegments[i];

                // Add entry point knot
                AddKnot(spline, container.transform, segment.entryPoint);

                // Add exit point knot (skip on last segment if loop is closed, the first entry handles it)
                bool isLastSegment = (i == placedSegments.Count - 1);
                if (!isLastSegment || !isLoopClosed)
                {
                    AddKnot(spline, container.transform, segment.exitPoint);
                }
            }

            spline.Closed = isLoopClosed;

            // Set tangent mode to auto-smooth for clean curves
            for (int i = 0; i < spline.Count; i++)
            {
                spline.SetTangentMode(i, TangentMode.AutoSmooth);
            }

            Debug.Log($"[TrackBuilder] Spline generated with {spline.Count} knots (closed: {isLoopClosed})");
        }

        /// <summary>
        /// Destroys all placed segments and resets the state.
        /// </summary>
        public void ClearAll()
        {
            for (int i = placedSegments.Count - 1; i >= 0; i--)
            {
                if (placedSegments[i] != null)
                {
                    if (Application.isPlaying)
                        Destroy(placedSegments[i].gameObject);
                    else
                        DestroyImmediate(placedSegments[i].gameObject);
                }
            }

            placedSegments.Clear();
            isLoopClosed = false;

            Debug.Log("[TrackBuilder] All segments cleared.");
        }

        /// <summary>
        /// Aligns a segment so its entryPoint matches the given anchor transform.
        /// </summary>
        private void SnapSegment(TrackSegment segment, Transform anchor)
        {
            // Calculate the local offset/rotation of the entry point relative to the segment root
            Vector3 entryLocalOffset = segment.transform.InverseTransformPoint(segment.entryPoint.position);
            Quaternion entryLocalRotation = Quaternion.Inverse(segment.transform.rotation) * segment.entryPoint.rotation;

            // Align rotation: make the entry point match the anchor's rotation
            segment.transform.rotation = anchor.rotation * Quaternion.Inverse(entryLocalRotation);

            // Align position: place so entry point sits exactly at anchor position
            Vector3 entryWorldOffset = segment.transform.TransformPoint(entryLocalOffset) - segment.transform.position;
            segment.transform.position = anchor.position - entryWorldOffset;
        }

        /// <summary>
        /// Adds a BezierKnot to the spline at the given world-space anchor position.
        /// </summary>
        private void AddKnot(Spline spline, Transform splineTransform, Transform anchor)
        {
            // Convert world position to spline-local position
            Vector3 localPos = splineTransform.InverseTransformPoint(anchor.position);
            Quaternion localRot = Quaternion.Inverse(splineTransform.rotation) * anchor.rotation;

            BezierKnot knot = new BezierKnot(
                new float3(localPos.x, localPos.y, localPos.z),
                float3.zero,
                float3.zero,
                new quaternion(localRot.x, localRot.y, localRot.z, localRot.w)
            );

            spline.Add(knot);
        }
    }
}
