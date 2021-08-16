using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using FSM.Graph;
namespace FSM {
    public class StateMachineRuntime : MonoBehaviour {
        public StateMachineController controller;
        public string activeState;
        public bool selected;
        public static event System.Action<string> OnParameterChanged;
        [SerializeField] internal StateMachineController controllerInstance;
        [SerializeField] protected bool m_ExecuteInEditor = true;
        public List<FSMBinderBase> m_Bindings = new List<FSMBinderBase>();
        static private void SafeDestroy(Object toDelete) {
        #if UNITY_EDITOR
            if (Application.isPlaying)
                Destroy(toDelete);
            else
                Undo.DestroyObjectImmediate(toDelete);
        #else
            Destroy(toDelete);
        #endif
        }
        void LateUpdate() {
            if (!m_ExecuteInEditor && Application.isEditor && !Application.isPlaying) return;
            for (int i = 0; i < m_Bindings.Count; i++) {
                var binding = m_Bindings[i];
                if (binding == null) {
                    Debug.LogWarning(string.Format("Property binder at index {0} of GameObject {1} is null or missing", i, gameObject.name));
                    continue;
                }
                else {
                    if (binding.IsValid(this)) binding.UpdateBinding(this);
                }
            }
        }
        public T AddPropertyBinder<T>() where T : FSMBinderBase {
            return gameObject.AddComponent<T>();
        }
        public void RemovePropertyBinder(FSMBinderBase binder) {
            if (binder.gameObject == this.gameObject)
                SafeDestroy(binder);
        }
        private void Start() {
            if (controller == null) return;
            controllerInstance = controller.Clone(this); 
            controllerInstance.CloneStateBehaviours();
            if (EditorWindow.HasOpenInstances<StateMachineGraphWindow>()){
                var window = EditorWindow.GetWindow<StateMachineGraphWindow>();
                if (window.controller == controller) {
                    StateMachineGraphWindow.OpenGraphWindow(controllerInstance);
                    selected = true;
                }
            }          
            controllerInstance.SetState(controllerInstance.GetEntryState());
        }
        private void OnEnable() {
            StateMachineGraphWindow.OnControllerSelected += OnControllerChanged;
        }
        private void OnDisable() {
            StateMachineGraphWindow.OnControllerSelected -= OnControllerChanged;
        }
        void OnControllerChanged(StateMachineController newController) {
            if (controllerInstance == null) return;
            selected = newController == controllerInstance;
        }
        private void OnApplicationQuit() {
            controller?.InitalizeControllers(controller);
            if (EditorWindow.HasOpenInstances<StateMachineGraphWindow>()){
                var window = EditorWindow.GetWindow<StateMachineGraphWindow>();
                if (window.controller == controllerInstance) {
                    window.ReloadGraph(controller);
                }
            }
        }
        private void Update() {
            if (controllerInstance == null) return;
            activeState = controllerInstance.CurrentState == null ? "No State" : controllerInstance.CurrentState.stateName;
            controllerInstance?.UpdateCurrentState(this, selected);
        }

#region Parameter Methods
        public void SetFloat(string parameter, float value) {
            var param = controllerInstance.GetParameter<FloatParameter>(parameter.GetHashCode());
            if (param == null) {
                Debug.Log($"Parameter name '{parameter.GetHashCode()}' was not found");
                return;
            }
            if (param.value.FloatValue != value) {
                param.value.FloatValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetFloat(int parameter, float value) {
            var param = controllerInstance.GetParameter<FloatParameter>(parameter);
            if (param == null) {
                Debug.Log($"Parameter name '{parameter.GetHashCode()}' was not found");
                return;
            }
            if (param.value.FloatValue != value) {
                param.value.FloatValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetInt(int parameter, int value) {
            var param = controllerInstance.GetParameter<IntParameter>(parameter);
            if (param == null) {
                Debug.Log($"Parameter name '{parameter.GetHashCode()}' was not found");
                return;
            }
            if (param.value.IntValue != value) {
                param.value.IntValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetInt(string parameter, int value) {
            var param = controllerInstance.GetParameter<IntParameter>(parameter.GetHashCode());
            if (param == null) {
                Debug.Log($"Parameter name '{parameter.GetHashCode()}' was not found");
                return;
            }
            if (param.value.IntValue != value) {
                param.value.IntValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetBool(int parameter, bool value) {
            var param = controllerInstance.GetParameter<BoolParameter>(parameter);
            if (param == null) {
                Debug.Log($"Parameter name '{parameter.GetHashCode()}' was not found");
                return;
            }
            if (param.value.BoolValue != value) {
                param.value.BoolValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        public void SetBool(string parameter, bool value) {
            var param = controllerInstance?.GetParameter<BoolParameter>(parameter.GetHashCode());
            if (param == null) {
                return;
            }
            if (param.value.BoolValue != value) {
                Debug.Log($"{param.name} Bool parameter changed to {value}");
                param.value.BoolValue = value;
                if (selected) OnParameterChanged?.Invoke(param.name);
            }
        }
        internal void SetTransformProperty(string property, Transform value) {
            var val = new TransformData(value);
            var prop = controllerInstance != null 
                ? controllerInstance.GetProperty<TransformProperty>(property.GetHashCode())
                : controller.GetProperty<TransformProperty>(property.GetHashCode());
            if (prop == null) {
                Debug.Log($"Parameter name '{prop.GetHashCode()}' was not found");
                return;
            }
            if (prop.value.TransformValue != val) {
                prop.value.TransformValue = val;
            }
        }
        public float GetFloat(string parameter) => controllerInstance.GetParameter<FloatParameter>(parameter.GetHashCode()).value.FloatValue;
        public int GetInt(string parameter) => controllerInstance.GetParameter<IntParameter>(parameter.GetHashCode()).value.IntValue;
        public bool GetBool(string parameter) => controllerInstance.GetParameter<BoolParameter>(parameter.GetHashCode()).value.BoolValue;
        public float GetFloat(int parameter) => controllerInstance.GetParameter<FloatParameter>(parameter).value.FloatValue;
        public int GetInt(int parameter) => controllerInstance.GetParameter<IntParameter>(parameter).value.IntValue;
        public bool GetBool(int parameter) => controllerInstance.GetParameter<BoolParameter>(parameter).value.BoolValue;
        public bool HasTransform(int property) => controller.GetProperty<TransformProperty>(property) != null;
#endregion
    #if UNITY_EDITOR
        [CustomEditor(typeof(StateMachineRuntime))]
        class StateMachineRuntimeEditor : Editor {
            StateMachineRuntime Controller => target as StateMachineRuntime;
            UnityEditorInternal.ReorderableList propertyList;
            SerializedProperty propertyListProp, controllerProp;
            FSMBinderEditor elementEditor;
            GenericMenu m_Menu;
            static readonly Color validColor = new Color(0.5f, 1.0f, 0.2f);
            static readonly Color invalidColor = new Color(1.0f, 0.5f, 0.2f);
            static readonly Color errorColor = new Color(1.0f, 0.2f, 0.2f);

            private void OnEnable() {
                BuildMenu();
                controllerProp = serializedObject.FindProperty("controller");
                propertyListProp = serializedObject.FindProperty("m_Bindings");
                propertyList = new UnityEditorInternal.ReorderableList(serializedObject, propertyListProp, true, true, true, true);
                propertyList.onAddDropdownCallback = (rect, list) => {
                    m_Menu.ShowAsContext();
                };
                propertyList.drawElementCallback = (rect, index, isActive, isFocused) => {
                    var property = propertyListProp.GetArrayElementAtIndex(index).objectReferenceValue as FSMBinderBase;
                    Rect labelRect = new Rect(rect.x + 16, rect.y, rect.width, rect.height);
                    Rect iconRect = new Rect(rect.xMin + 4, rect.yMin + 7, 8, 8);
                    if (property != null && property.Initialized) {
                        var element = property.ToString();
                        GUI.Label(labelRect, new GUIContent(element), EditorStyles.label);
                        bool valid = property.IsValid(Controller);
                        EditorGUI.DrawRect(iconRect, valid ? validColor : invalidColor);
                    }
                    else {
                        EditorGUI.DrawRect(iconRect, errorColor);
                        GUI.Label(rect, "<color=red>(Missing or Null Property Binder)</color>", EditorStyles.label);
                    }
                };
                propertyList.drawHeaderCallback = rect => {
                    EditorGUI.LabelField(rect, "Property Binds");
                };
                propertyList.onSelectCallback = list => {
                    UpdateSelection(list.index);
                };
                propertyList.onRemoveCallback = list => {
                    int index = propertyList.index;
                    var element = propertyListProp.GetArrayElementAtIndex(index).objectReferenceValue;
                    if (element != null) {
                        Undo.DestroyObjectImmediate(element);
                    }
                    else {
                        Undo.RecordObject(serializedObject.targetObject, "Remove null entry");
                    }
                    propertyListProp.DeleteArrayElementAtIndex(index); // Remove list entry
                    UpdateSelection(-1);
                };
            }
            public void UpdateSelection(int selected) {
                if (selected >= 0) {
                    Editor editor = null;
                    CreateCachedEditor(propertyListProp.GetArrayElementAtIndex(selected).objectReferenceValue, typeof(FSMBinderEditor), ref editor);
                    elementEditor = editor as FSMBinderEditor;
                } else elementEditor = null;
            }
            public override void OnInspectorGUI() {
                serializedObject.Update();
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(controllerProp);
                EditorGUILayout.Space();
                propertyList.DoLayoutList();
                EditorGUILayout.Space();
                if (EditorGUI.EndChangeCheck())
                    serializedObject.ApplyModifiedProperties();
                if (elementEditor != null && elementEditor.target != null && elementEditor.serializedObject.targetObject != null) {
                    elementEditor.serializedObject.Update();

                    EditorGUI.BeginChangeCheck();
                    var fieldAttribute = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
                    var binding = elementEditor.serializedObject.targetObject;
                    var type = binding.GetType();
                    var fields = type.GetFields(fieldAttribute);

                    foreach (var field in fields) {
                        var property = elementEditor.serializedObject.FindProperty(field.Name);
                        if (property == null) continue;
                        EditorGUILayout.PropertyField(property, true);
                    }
                    if (EditorGUI.EndChangeCheck())
                        elementEditor.serializedObject.ApplyModifiedProperties();
                    bool valid = (binding as FSMBinderBase).IsValid(Controller);
                    if (!valid){
                        EditorGUILayout.HelpBox("This binding is not correctly configured, please ensure Property is valid and/or objects are not null", MessageType.Warning);
                    }
                }
            }
            private static System.Type[] s_ConcreteBinders = null;
            public void BuildMenu() {
                m_Menu = new GenericMenu();
                s_ConcreteBinders = TypeCache.GetTypesDerivedFrom<FSMBinderBase>().ToArray();
                if (s_ConcreteBinders == null) {
                    Debug.Log("No binder types found");
                    return;
                }
                foreach (var type in s_ConcreteBinders){
                    string name = type.ToString();
                    var attrib = type.GetCustomAttributes(true).OfType<FSMBinderAttribute>().First();
                    name = attrib.MenuPath;
                    m_Menu.AddItem(new GUIContent(name), false, AddBinding, type);
                }
            }
            public void AddBinding(object type) {
                System.Type t = type as System.Type;
                var obj = Controller.gameObject;
                Undo.AddComponent(obj, t);
            }
        }
    #endif
    }
}