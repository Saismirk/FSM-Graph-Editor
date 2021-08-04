using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
namespace FSM.Graph {
    public class GraphNodeSearchWindow : ScriptableObject, ISearchWindowProvider {
        StateMachineGraphView graphView;
        EditorWindow window;

        public void Init(StateMachineGraphView graphView, EditorWindow window = null) {
            this.graphView = graphView;
            this.window = window;
        }
        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context) {
            var tree = new List<SearchTreeEntry> {
                new SearchTreeGroupEntry(new GUIContent("States"), 0),
                new SearchTreeEntry(new GUIContent("State")) {
                    userData = CreateInstance(typeof(State)),
                    level = 1
                },
                new SearchTreeEntry(new GUIContent("Sub-State")) {
                    userData = CreateInstance(typeof(SubStateMachineController)),
                    level = 1
                }
            };
            return tree;
        }
        public void AddNodeEntries<T>(ref List<SearchTreeEntry> tree, string label, int lvl = 1) where T : State{
            tree.Add(new SearchTreeGroupEntry(new GUIContent(label), lvl));
            var types = TypeCache.GetTypesDerivedFrom<T>();
            foreach (var type in types) {
                tree.Add(new SearchTreeEntry(new GUIContent(type.Name)) {
                    userData = CreateInstance(type),
                    level = lvl + 1
                });
            }
        }
        public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context) {
            var worldMousePosition = window.rootVisualElement.ChangeCoordinatesTo(window.rootVisualElement.parent, context.screenMousePosition - window.position.position);
            var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);
            switch (SearchTreeEntry.userData) {
                case State node :
                    if (node is EntryState && graphView.controller.GetEntryState() != null) {
                        Debug.Log("There is already an Entry State. Only one can exist in the graph.");
                        return true;
                    }
                    graphView.CreateNode(node.GetType(), localMousePosition);
                    return true;
                case SubStateMachineController sm:
                    graphView.CreateSubStateMachine(sm.GetType(), localMousePosition);
                    return true;
                default :
                    return false;
            }
        }
    }

#endif
}
