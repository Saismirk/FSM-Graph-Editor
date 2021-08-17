
using UnityEngine.UIElements;
using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEditor.Experimental.GraphView;
#endif
namespace FSM.Graph {
    public class InspectorPanel : GraphElement, ISelection {
        static readonly string StyleSheetPath = "StyleSheets/GraphView/Blackboard.uss";
        static readonly string pathToSettings = "Assets/Editor/FSMEditor/FSMSettings.asset";
        readonly VisualElement m_MainContainer;
        readonly VisualElement m_ContentContainer;
        readonly VisualElement m_Root;
        readonly Label m_TitleLabel;
        readonly VisualElement m_HeaderItem;
        readonly bool m_Scrollable = true;
        readonly Dragger m_Dragger;
        Label m_SubTitleLabel;
        ScrollView m_ScrollView;
        InspectorElement inspectorElement;
        TextField nameField;
        GraphView m_GraphView;
        public GraphView graphView {
            get {
                return m_GraphView ??= GetFirstAncestorOfType<GraphView>();
            }
            set {
                m_GraphView = value;
            }
        }
        Vector2 position;
        public bool PositionHasChanged {
            get {
                var newPosition = GetPosition().position;
                if (position != newPosition) {
                    position = newPosition;
                    return true;
                }
                return false;
            }
        }

        public override VisualElement contentContainer => m_ContentContainer;
        public List<ISelectable> selection => throw new NotImplementedException();
        public InspectorPanel() {
            var tpl = Resources.Load<VisualTreeAsset>("FSMEditor/UXML/GraphInspector");
            styleSheets.Add(Resources.Load<StyleSheet>("FSMEditor/StyleSheets/Blackboard"));
            m_MainContainer = tpl.Instantiate();
            position = GetPosition().position;
            m_MainContainer.AddToClassList("mainContainer");
            m_Root = m_MainContainer.Q("content");

            m_HeaderItem = m_MainContainer.Q("header");
            m_HeaderItem.AddToClassList("blackboardHeader");

            m_TitleLabel = m_MainContainer.Q<Label>(name: "titleLabel");
            m_TitleLabel.text = "Graph Inspector";
            m_SubTitleLabel = m_MainContainer.Q<Label>(name: "subTitleLabel");
            m_SubTitleLabel.text = "";
            m_ContentContainer = m_MainContainer.Q(name: "contentContainer");
            m_ScrollView = m_MainContainer.Q<ScrollView>(name: "ScrollView");
            capabilities |= Capabilities.Movable | Capabilities.Resizable;
            style.overflow = Overflow.Hidden;

            ClearClassList();
            hierarchy.Add(m_MainContainer);
            AddToClassList("blackboard");

            m_Dragger = new Dragger { clampToParentEdges = true };
            this.AddManipulator(m_Dragger);
            hierarchy.Add(new Resizer());
            focusable = true;
            UnityEngine.Object obj = null;
            inspectorElement = new InspectorElement(obj);
            m_ScrollView.Add(inspectorElement);
        }
        public void DisplayInspector(UnityEngine.Object obj) {
            inspectorElement?.RemoveFromHierarchy();
            if (obj == null) {
                nameField?.RemoveFromHierarchy();
                return;
            }
            inspectorElement = new InspectorElement(obj);
            nameField?.RemoveFromHierarchy();
            nameField = new TextField("Name: ") {
                value = obj.name,
                isDelayed = true,
            };
            nameField.style.flexDirection = FlexDirection.Column;
            nameField.RegisterValueChangedCallback(evt => {
                obj.name = evt.newValue;
                (graphView as StateMachineGraphView)?.UpdateNodeNames();
                AssetDatabase.SaveAssets();
            });
            if (!(obj is EntryState) && !(obj is AnyState) && !(obj is ExitState) && !(obj is UpState)) m_ScrollView.Add(nameField);
            m_ScrollView.Add(inspectorElement);
            m_SubTitleLabel.text = obj != null ? ObjectNames.NicifyVariableName(obj.GetType().Name) : "";
        }
        public void AddToSelection(ISelectable selectable) {
            graphView?.AddToSelection(selectable);
        }
        public void RemoveFromSelection(ISelectable selectable) {
            graphView?.RemoveFromSelection(selectable);
        }
        public void ClearSelection() {
            graphView?.ClearSelection();
        }
    }
}