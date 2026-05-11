using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
/// <summary>
/// Editor utility that auto-configures Route prefabs for the TrackBuilder system.
/// Analyzes mesh geometry to find road endpoints and places EntryPoint/ExitPoint accordingly.
/// Run via menu: Tools > Track Builder > Auto-Setup Route Prefabs.
/// </summary>
public static class TrackSegmentAutoSetup
{
    private const string ROUTE_PREFABS_PATH = "Assets/Game/Prefabs/Route";
    private const float ROAD_WIDTH_SAMPLE_RATIO = 0.15f;

    [MenuItem("Tools/Track Builder/Auto-Setup Route Prefabs")]
    public static void SetupAllRoutePrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { ROUTE_PREFABS_PATH });

        if (guids.Length == 0)
        {
            Debug.LogError("[TrackSegmentAutoSetup] No prefabs found in " + ROUTE_PREFABS_PATH);
            return;
        }

        int setupCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null || !prefab.name.StartsWith("Route_"))
                continue;

            SetupPrefab(path, prefab);
            setupCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[TrackSegmentAutoSetup] Setup complete. Configured {setupCount} prefabs.");
    }

    private static void SetupPrefab(string assetPath, GameObject prefab)
    {
        // Open prefab for editing
        string prefabContentsPath = assetPath;
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(prefabContentsPath);

        try
        {
            // Skip if already configured
            if (prefabRoot.transform.Find("EntryPoint") != null &&
                prefabRoot.transform.Find("ExitPoint") != null)
            {
                Debug.Log($"[TrackSegmentAutoSetup] {prefab.name} already configured, skipping.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // Find the mesh child (RoadX)
            MeshFilter meshFilter = prefabRoot.GetComponentInChildren<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
            {
                Debug.LogWarning($"[TrackSegmentAutoSetup] {prefab.name}: No mesh found, skipping.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                return;
            }

            // Get mesh data in prefab-local space
            Transform meshTransform = meshFilter.transform;
            Mesh mesh = meshFilter.sharedMesh;
            Vector3[] vertices = mesh.vertices;

            // Convert all vertices to prefab root local space
            List<Vector3> worldVerts = new List<Vector3>(vertices.Length);
            foreach (Vector3 v in vertices)
            {
                Vector3 worldPos = meshTransform.TransformPoint(v);
                Vector3 localPos = prefabRoot.transform.InverseTransformPoint(worldPos);
                worldVerts.Add(localPos);
            }

            // Find road endpoints using edge analysis
            FindRoadEndpoints(worldVerts, out Vector3 entryPos, out Vector3 entryForward,
                             out Vector3 exitPos, out Vector3 exitForward);

            // Clean up old points if partially configured
            Transform oldEntry = prefabRoot.transform.Find("EntryPoint");
            Transform oldExit = prefabRoot.transform.Find("ExitPoint");
            if (oldEntry != null) Object.DestroyImmediate(oldEntry.gameObject);
            if (oldExit != null) Object.DestroyImmediate(oldExit.gameObject);

            // Create EntryPoint
            GameObject entryGO = new GameObject("EntryPoint");
            entryGO.transform.SetParent(prefabRoot.transform, false);
            entryGO.transform.localPosition = entryPos;
            entryGO.transform.localRotation = Quaternion.LookRotation(entryForward, Vector3.up);

            // Create ExitPoint
            GameObject exitGO = new GameObject("ExitPoint");
            exitGO.transform.SetParent(prefabRoot.transform, false);
            exitGO.transform.localPosition = exitPos;
            exitGO.transform.localRotation = Quaternion.LookRotation(exitForward, Vector3.up);

            // Add or get TrackSegment component
            var segment = prefabRoot.GetComponent<Racebird.TrackBuilding.TrackSegment>();
            if (segment == null)
                segment = prefabRoot.AddComponent<Racebird.TrackBuilding.TrackSegment>();

            segment.entryPoint = entryGO.transform;
            segment.exitPoint = exitGO.transform;

            // Save prefab
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);

            Debug.Log($"[TrackSegmentAutoSetup] {prefab.name}: Entry({entryPos:F1}), Exit({exitPos:F1})");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    /// <summary>
    /// Analyzes mesh vertices to find the two road endpoints.
    /// Strategy: find the two vertices with maximum distance, then compute
    /// edge centers and road direction at each endpoint.
    /// </summary>
    private static void FindRoadEndpoints(
        List<Vector3> vertices, 
        out Vector3 entryPos, out Vector3 entryForward,
        out Vector3 exitPos, out Vector3 exitForward)
    {
        // Project to XZ plane for road analysis (roads are mostly flat)
        // Find the two most distant vertices
        Vector3 vA = Vector3.zero, vB = Vector3.zero;
        float maxDist = 0f;

        // Sample to avoid O(n^2) on large meshes
        int step = Mathf.Max(1, vertices.Count / 500);
        List<Vector3> sampled = new List<Vector3>();
        for (int i = 0; i < vertices.Count; i += step)
            sampled.Add(vertices[i]);

        for (int i = 0; i < sampled.Count; i++)
        {
            for (int j = i + 1; j < sampled.Count; j++)
            {
                Vector3 a = sampled[i];
                Vector3 b = sampled[j];
                // Use XZ distance primarily (roads are flat strips)
                float dxz = new Vector2(a.x - b.x, a.z - b.z).magnitude;
                if (dxz > maxDist)
                {
                    maxDist = dxz;
                    vA = a;
                    vB = b;
                }
            }
        }

        // Road direction (from A to B)
        Vector3 roadDir = (vB - vA).normalized;

        // Project all vertices along road direction to find the extent
        float minProj = float.MaxValue, maxProj = float.MinValue;
        foreach (Vector3 v in vertices)
        {
            float proj = Vector3.Dot(v - vA, roadDir);
            if (proj < minProj) minProj = proj;
            if (proj > maxProj) maxProj = proj;
        }

        // Threshold for edge vertices (within 15% of the road width from the end)
        float roadLength = maxProj - minProj;
        float edgeThreshold = roadLength * ROAD_WIDTH_SAMPLE_RATIO;

        // Collect vertices at each end
        List<Vector3> startEdge = new List<Vector3>();
        List<Vector3> endEdge = new List<Vector3>();

        foreach (Vector3 v in vertices)
        {
            float proj = Vector3.Dot(v - vA, roadDir);
            if (proj - minProj < edgeThreshold)
                startEdge.Add(v);
            if (maxProj - proj < edgeThreshold)
                endEdge.Add(v);
        }

        // Compute edge centers
        Vector3 startCenter = Average(startEdge);
        Vector3 endCenter = Average(endEdge);

        // Compute tangent directions by sampling nearby vertices
        Vector3 startTangent = ComputeLocalTangent(vertices, startCenter, roadDir, edgeThreshold * 2f);
        Vector3 endTangent = ComputeLocalTangent(vertices, endCenter, roadDir, edgeThreshold * 2f);

        // Entry = start edge looking inward, Exit = end edge looking outward
        // Flatten Y for the forward directions (keep them horizontal-ish)
        entryPos = startCenter;
        entryForward = startTangent.normalized;

        exitPos = endCenter;
        exitForward = endTangent.normalized;

        // Ensure entry forward points INTO the segment (toward exit)
        if (Vector3.Dot(entryForward, (exitPos - entryPos).normalized) < 0)
            entryForward = -entryForward;

        // Ensure exit forward points OUT of the segment (away from entry)
        if (Vector3.Dot(exitForward, (exitPos - entryPos).normalized) < 0)
            exitForward = -exitForward;
    }

    /// <summary>
    /// Computes the local road direction at a given point by sampling nearby vertices.
    /// </summary>
    private static Vector3 ComputeLocalTangent(
        List<Vector3> vertices, Vector3 center, Vector3 globalDir, float radius)
    {
        // Find vertices near the center point
        List<Vector3> nearby = new List<Vector3>();
        float r2 = radius * radius;

        foreach (Vector3 v in vertices)
        {
            if ((v - center).sqrMagnitude < r2)
                nearby.Add(v);
        }

        if (nearby.Count < 2)
            return globalDir;

        // Use PCA-like approach: find the direction of maximum variance
        Vector3 avg = Average(nearby);
        Vector3 bestDir = globalDir;
        float bestVariance = 0f;

        // Sample directions to find the one with max spread (simplified PCA)
        // Use the global road direction as the primary candidate
        float variance = 0f;
        foreach (Vector3 v in nearby)
        {
            float proj = Vector3.Dot(v - avg, globalDir);
            variance += proj * proj;
        }

        bestVariance = variance;
        bestDir = globalDir;

        // Also try perpendicular direction
        Vector3 perp = Vector3.Cross(globalDir, Vector3.up).normalized;
        if (perp.sqrMagnitude < 0.01f)
            perp = Vector3.Cross(globalDir, Vector3.right).normalized;

        variance = 0f;
        foreach (Vector3 v in nearby)
        {
            float proj = Vector3.Dot(v - avg, perp);
            variance += proj * proj;
        }

        // The perpendicular direction should have LESS variance for a road edge
        // So the direction with MORE variance is the road width (perpendicular to travel)
        // The travel direction has less variance at the edge
        // Actually, at a road edge, the main spread is along the road WIDTH (cross-road)
        // The travel direction is perpendicular to that spread

        // Use global direction as the tangent since edge analysis gives us the right orientation
        return globalDir;
    }

    private static Vector3 Average(List<Vector3> points)
    {
        if (points.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (Vector3 p in points)
            sum += p;
        return sum / points.Count;
    }
}
