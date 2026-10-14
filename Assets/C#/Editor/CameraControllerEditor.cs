#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CameraController))]
public class CameraControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        CameraController controller = (CameraController)target;
        
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Apply Current Resolution"))
        {
            controller.ApplyCurrentResolution();
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Preset Resolutions", EditorStyles.boldLabel);
        
        string[] presetNames = {
            "4:3 Standard (800x600)",
            "9:16 Portrait (1080x1920)",
            "iPhone 6/7/8 (750x1334)",
            "iPhone XR (828x1792)",
            "Android Standard (1080x2340)",
            "HD Portrait (720x1280)"
        };
        
        for (int i = 0; i < presetNames.Length; i++)
        {
            if (GUILayout.Button(presetNames[i]))
            {
                controller.UsePresetResolution(i);
            }
        }
    }
}
#endif