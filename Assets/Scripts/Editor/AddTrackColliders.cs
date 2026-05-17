using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility that adds MeshColliders to all track barrier and road GameObjects
/// that don't already have one. Accessible via Tools → Add Track Colliders.
/// </summary>
public static class AddTrackColliders
{
    private static readonly string[] NamePatterns =
    {
        "Road", "Route", "Fence", "Barrier",
        "SM_Reflectors", "SM_gradin", "SM_Metal", "SM_Ban"
    };

    [MenuItem("Tools/Add Track Colliders")]
    public static void Execute()
    {
        int added = 0;
        int skipped = 0;

        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);

        foreach (GameObject go in allObjects)
        {
            if (!MatchesAnyPattern(go.name))
                continue;

            // Skip if already has a Collider of any kind
            if (go.GetComponent<Collider>() != null)
            {
                skipped++;
                continue;
            }

            MeshFilter meshFilter = go.GetComponent<MeshFilter>();
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            MeshCollider mc = Undo.AddComponent<MeshCollider>(go);
            mc.sharedMesh = meshFilter.sharedMesh;
            mc.convex = false;
            mc.isTrigger = false;

            added++;
        }

        Debug.Log($"[AddTrackColliders] Terminé — {added} MeshColliders ajoutés, {skipped} GameObjects déjà équipés d'un collider.");
    }

    private static bool MatchesAnyPattern(string name)
    {
        for (int i = 0; i < NamePatterns.Length; i++)
        {
            if (name.IndexOf(NamePatterns[i], System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }
        return false;
    }
}
