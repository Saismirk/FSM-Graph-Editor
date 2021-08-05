using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

namespace FSM {
    public class FSMSettings : ScriptableObject {
        public const string settingsPath = "Assets/Editor/FSMEditor/FSMSettings.asset";
        [SerializeField] public readonly string PathToTemplate = "Assets/Scripts/FSM/Behaviours/StateBehaviourTemplate.cs.txt";
        [SerializeField] public readonly string PathToBehaviours = "Assets/Scripts/FSM/Behaviours/";
        
        internal static FSMSettings GetOrCreateSettings() {
            var settings = AssetDatabase.LoadAssetAtPath<FSMSettings>(settingsPath);
            if (settings == null) {
                settings = CreateInstance<FSMSettings>();
                AssetDatabase.CreateAsset(settings, settingsPath);
                AssetDatabase.SaveAssets();
            }
            return settings;
        }
        internal static SerializedObject GetSerializedSettings() => new SerializedObject(GetOrCreateSettings());
    }

    static class FSMSettingsIMGUIRegister {
        [SettingsProvider]
        public static SettingsProvider CreateFSMSettingsProvider() {
            var provider = new SettingsProvider("Project/FSM Settings", SettingsScope.Project) {
                label = "FSM Settings",
                guiHandler = (searchContext) => {
                    var settings = FSMSettings.GetSerializedSettings();
                    EditorGUILayout.PropertyField(settings.FindProperty("PathToTemplate"), new GUIContent("Path To Template File"));
                    EditorGUILayout.PropertyField(settings.FindProperty("PathToBehaviours"), new GUIContent("Path To Behaviours Folder"));
                },
                keywords = new HashSet<string>(new[] { "Template File", "Behaviours Folder" })
            };

            return provider;
        }
    }
}
#endif