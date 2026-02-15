using UnityEditor;
using UnityEditor.AssetImporters;
using Miventech.NativeVoxReader.VoxRenderer.Types;
using System.Linq;
using UnityEngine;
using Miventech.NativeVoxReader.Editor;

namespace Miventech.NativeVoxReader.Editor
{
    [CustomEditor(typeof(NativeVoxImporter))]
    public class NativeVoxImporterEditor : ScriptedImporterEditor
    {
        public override void OnInspectorGUI()
        {
            var importer = (NativeVoxImporter)target;
            
            var renderTypes = VoxRenderAbstract.GetAllRenderTypes();
            var typeNames = renderTypes.Select(t => t.Name).ToArray();
            
            int currentIndex = System.Array.IndexOf(typeNames, importer.selectedRenderType);
            if (currentIndex == -1) currentIndex = 0;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Native Vox Importer Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUI.BeginChangeCheck();
            int newIndex = EditorGUILayout.Popup("Render Mode", currentIndex, typeNames);
            if (EditorGUI.EndChangeCheck())
            {
                string newTypeName = typeNames[newIndex];
                
                SerializedProperty typeProp = serializedObject.FindProperty("selectedRenderType");
                typeProp.stringValue = newTypeName;

                var newType = VoxRenderAbstract.GetTypeByName(newTypeName);
                if (newType != null)
                {
                    GameObject temp = new GameObject();
                    temp.hideFlags = HideFlags.HideAndDontSave;
                    var renderer = (VoxRenderAbstract)temp.AddComponent(newType);
                    var settingsType = renderer.SettingsType;
                    Object.DestroyImmediate(temp);

                    SerializedProperty settingsProp = serializedObject.FindProperty("settings");
                    settingsProp.managedReferenceValue = System.Activator.CreateInstance(settingsType);
                }
            }

            EditorGUILayout.Space();

            SerializedProperty sProp = serializedObject.FindProperty("settings");
            if (sProp != null && sProp.managedReferenceValue != null)
            {
                EditorGUILayout.LabelField("Renderer Settings", EditorStyles.boldLabel);
                SerializedProperty iterator = sProp.Copy();
                bool enterChildren = true;
                while (iterator.NextVisible(enterChildren))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                    enterChildren = false;
                }
            }

            EditorGUILayout.Space();
            
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
