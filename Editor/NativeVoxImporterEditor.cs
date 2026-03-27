using UnityEditor;
using UnityEditor.AssetImporters;
using Miventech.NativeVoxReader.VoxRenderer.Types;
using Miventech.NativeVoxReader.Tools;
using Miventech.NativeVoxReader.Data;
using System.Linq;
using UnityEngine;
using Miventech.NativeVoxReader.Editor;

namespace Miventech.NativeVoxReader.Editor
{
    [CustomEditor(typeof(NativeVoxImporter))]
    public class NativeVoxImporterEditor : ScriptedImporterEditor
    {
        private bool _showPalette = false;
        private Color32[] _palette;

        public override void OnEnable()
        {
            base.OnEnable();
            var importer = (NativeVoxImporter)target;
            if (importer != null && !string.IsNullOrEmpty(importer.assetPath))
            {
                var loadedVoxFile = new Miventech.NativeVoxReader.Runtime.Tools.ReaderFile.ReaderVoxFile().Read(importer.assetPath);
                if (loadedVoxFile != null && loadedVoxFile.palette != null)
                {
                    _palette = loadedVoxFile.palette.ToColor32Array();
                }
            }
        }

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

            SerializedProperty overrideProp = serializedObject.FindProperty("overridePalette");
            SerializedProperty customPaletteProp = serializedObject.FindProperty("customPalette");
            
            EditorGUILayout.PropertyField(overrideProp);

            if (_palette != null && _palette.Length > 0)
            {
                _showPalette = EditorGUILayout.Foldout(_showPalette, $"Palette ({_palette.Length} Colors)", true);
                if (_showPalette)
                {
                    EditorGUI.indentLevel++;
                    
                    // Init custom palette array if not initialized and override is true
                    if (overrideProp.boolValue && (importer.customPalette == null || importer.customPalette.Length != _palette.Length))
                    {
                        importer.customPalette = (Color32[])_palette.Clone();
                        serializedObject.Update();
                    }

                    int columns = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 30) / 22);

                   

                    if (overrideProp.boolValue)
                    {
                        columns = Mathf.FloorToInt((EditorGUIUtility.currentViewWidth - 80) / 80);
                    }
                    if (columns < 1) columns = 1;

                    GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
                    boxStyle.margin = new RectOffset(1, 1, 1, 1);
                    boxStyle.padding = new RectOffset(0, 0, 0, 0);

                    GUILayout.BeginVertical();
                    int count = 0;
                    GUILayout.BeginHorizontal();
                    for (int i = 0; i < _palette.Length; i++)
                    {
                        var color = _palette[i];
                        if (color.a == 0 && color.r == 0 && color.g == 0 && color.b == 0) continue; // Skip empty colors

                        Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.ExpandWidth(false));
                        
                        if (overrideProp.boolValue && importer.customPalette != null && i < importer.customPalette.Length)
                        {
                            r = GUILayoutUtility.GetRect(60, 20, GUILayout.ExpandWidth(false));

                            // Editable color rect
                            Color currentColor = importer.customPalette[i];
                            EditorGUI.BeginChangeCheck();
                            Color newColor = EditorGUI.ColorField(r, new GUIContent("", $"Color {i}"), currentColor, false, true, false);
                            if (EditorGUI.EndChangeCheck())
                            {
                                importer.customPalette[i] = newColor;
                                EditorUtility.SetDirty(importer);
                            }
                        }
                        else
                        {
                            // Read-only color rect
                            EditorGUI.DrawRect(r, color);
                        }

                        count++;
                        if (count >= columns)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                            count = 0;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.Space();
            
            serializedObject.ApplyModifiedProperties();
            ApplyRevertGUI();
        }
    }
}
