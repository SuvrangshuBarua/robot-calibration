#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SplineControlSystem))]
public class SplineControlSystemEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SplineControlSystem controller = (SplineControlSystem)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Spline Operations", EditorStyles.boldLabel);

        if (GUILayout.Button("Initialize Spline from Bones"))
        {
            controller.InitializeSplineFromBones();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Update Spline from Bones"))
        {
            controller.UpdateSplineFromBones();
            EditorUtility.SetDirty(controller);
        }

        if (GUILayout.Button("Update Bones from Spline"))
        {
            controller.UpdateBonesFromSpline();
        }

        if (GUILayout.Button("Recalculate Tangents"))
        {
            controller.RecalculateTangents();
            EditorUtility.SetDirty(controller);
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "1. Assign bone root transforms to the list\n" +
            "2. Click 'Initialize Spline from Bones'\n" +
            "3. Use Spline tools in Scene view to manipulate\n" +
            "4. Enable 'Auto Update Spline' for real-time updates",
            MessageType.Info
        );
    }
}


#endif
