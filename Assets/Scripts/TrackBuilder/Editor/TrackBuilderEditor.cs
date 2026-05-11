using UnityEngine;
using UnityEditor;
using Racebird.TrackBuilding;

/// <summary>
/// Custom Inspector for TrackBuilder. Provides buttons to add, remove,
/// and manage track segments, plus spline generation controls.
/// </summary>
[CustomEditor(typeof(TrackBuilder))]
public class TrackBuilderEditor : Editor
{
    private const float BUTTON_HEIGHT = 30f;
    private const float SECTION_SPACING = 10f;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TrackBuilder builder = (TrackBuilder)target;

        EditorGUILayout.Space(SECTION_SPACING);

        // --- Status ---
        EditorGUILayout.LabelField("Status", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Segments placed", builder.SegmentCount.ToString());
        EditorGUILayout.LabelField("Loop closed", builder.IsLoopClosed ? "Yes" : "No");

        EditorGUILayout.Space(SECTION_SPACING);

        // --- Segment Buttons ---
        EditorGUILayout.LabelField("Add Segment", EditorStyles.boldLabel);

        if (builder.SegmentPrefabs != null && builder.SegmentPrefabs.Count > 0)
        {
            for (int i = 0; i < builder.SegmentPrefabs.Count; i++)
            {
                string buttonLabel = builder.SegmentPrefabs[i] != null
                    ? builder.SegmentPrefabs[i].gameObject.name
                    : $"(empty slot {i})";

                EditorGUI.BeginDisabledGroup(builder.SegmentPrefabs[i] == null);
                if (GUILayout.Button(buttonLabel, GUILayout.Height(BUTTON_HEIGHT)))
                {
                    Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Add Track Segment");
                    builder.AddSegment(i);
                    EditorUtility.SetDirty(builder);
                }
                EditorGUI.EndDisabledGroup();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("No segment prefabs assigned. Add TrackSegment prefabs to the Segment Library list.", MessageType.Info);
        }

        EditorGUILayout.Space(SECTION_SPACING);

        // --- Track Actions ---
        EditorGUILayout.LabelField("Track Actions", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(builder.SegmentCount == 0);

        if (GUILayout.Button("Remove Last", GUILayout.Height(BUTTON_HEIGHT)))
        {
            Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Remove Last Segment");
            builder.RemoveLastSegment();
            EditorUtility.SetDirty(builder);
        }

        if (GUILayout.Button("Close Loop", GUILayout.Height(BUTTON_HEIGHT)))
        {
            builder.CloseLoop();
            EditorUtility.SetDirty(builder);
        }

        if (GUILayout.Button("Generate Spline", GUILayout.Height(BUTTON_HEIGHT)))
        {
            builder.GenerateSpline();
        }

        EditorGUI.EndDisabledGroup();

        EditorGUILayout.Space(SECTION_SPACING / 2f);

        // --- Danger Zone ---
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        EditorGUI.BeginDisabledGroup(builder.SegmentCount == 0);
        if (GUILayout.Button("Clear All", GUILayout.Height(BUTTON_HEIGHT)))
        {
            if (EditorUtility.DisplayDialog("Clear All Segments",
                "This will destroy all placed segments. Are you sure?",
                "Clear All", "Cancel"))
            {
                Undo.RegisterFullObjectHierarchyUndo(builder.gameObject, "Clear All Segments");
                builder.ClearAll();
                EditorUtility.SetDirty(builder);
            }
        }
        EditorGUI.EndDisabledGroup();
        GUI.backgroundColor = Color.white;
    }
}
