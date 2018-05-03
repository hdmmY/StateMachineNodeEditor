using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NodeEditorFramework;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;
using System.Linq;

[Serializable]
[Node (false, "State/ConditionNode", new Type[] { typeof (StateMachineCanvas) })]
public class ConditionNode : Node
{
    #region GUI Properties

    private const string Id = "ConditionNode";

    public override string GetID => Id;

    public override string Title => "Condition";

    public override bool HideTitle => true;

    public override Vector2 DefaultSize => _defaultSize;

    [ConnectionKnob ("Out", Direction.Out, "ConditionTransOut", ConnectionCount.Single)]
    public ConnectionKnob ConditionTransOut;

    [ConnectionKnob ("In", Direction.In, "ConditionTransIn", ConnectionCount.Single)]
    public ConnectionKnob ConditionTransIn;

    public string ConditionDescription = "条件描述 ";

    private Vector2 _defaultSize;

    #endregion

    #region GUI methods

    protected override void OnCreate ()
    {
        InitCondition ();
    }

    public override void NodeGUI ()
    {
        GUIStyle labelStyle = new GUIStyle (GUI.skin.label);
        labelStyle.fontSize = 10;
        labelStyle.normal.textColor = new Color (1, 1, 1, 0.85f);
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.fontStyle = NodeEditor.curEditorState.selectedNode == this ?
            FontStyle.Bold : FontStyle.Normal;

        EditorGUILayout.LabelField (ConditionDescription, labelStyle);

        _defaultSize.y = 22;
        _defaultSize.x = ConditionDescription.Length * 10;

        UpdateConnectKnobPosition (ConditionTransOut, NodeSide.Left, 20);
        UpdateConnectKnobPosition (ConditionTransIn, NodeSide.Right, 20);
    }

    public override void DrawNodePropertyEditor ()
    {
        GUILayout.Space (8);

        EditorGUIUtility.labelWidth = 70;

        EditorGUILayout.LabelField ("Description");
        ConditionDescription = EditorGUILayout.TextField (ConditionDescription);
        GUILayout.Space (10);

        //Draw condition method

        UpdateConditionInfo ();

        if (_serializedObject == null)
        {
            InitCondition ();
        }

        _serializedObject.Update ();
        _conditionEditorList.DoLayoutList ();
        _serializedObject.ApplyModifiedProperties ();
    }

    #endregion

    #region Public member variables

    public MutiConditionEditor Conditions;

    #endregion

    #region Private variables and methods

    private SerializedObject _serializedObject;

    private StateInfoNode _stateInfoNode;

    private ReorderableList _conditionEditorList;

    private FieldInfo[] _stateInfoFields;

    private long _nextUpdateTime;

    private void InitCondition ()
    {
        if (Conditions == null)
        {
            Conditions = CreateInstance<MutiConditionEditor> ();
        }

        _stateInfoNode = FindStateInfoNode ();

        _serializedObject = new SerializedObject (Conditions);

        _conditionEditorList = new ReorderableList (_serializedObject,
            _serializedObject.FindProperty ("Conditions"), true, true, true, true);
        _conditionEditorList.drawElementCallback = DisplayConditionElement;
        _conditionEditorList.drawHeaderCallback = DisplayConditionHealder;
        _conditionEditorList.onAddCallback = CreateConditionEditor;
    }

    private void UpdateConnectKnobPosition (ConnectionKnob knob, NodeSide defaultSide, float defaultOffset)
    {
        if (knob.connected ())
        {
            Vector2 selfPos = center;
            Vector2 otherPos = knob.connections[0].body.center;

            Vector2 bottomLeft = new Vector2 (rect.xMin, rect.yMax);
            Vector2 bottomRight = new Vector2 (rect.xMax, rect.yMax);
            Vector2 topRight = new Vector2 (rect.xMax, rect.yMin);
            Vector2 topLeft = new Vector2 (rect.xMin, rect.yMin);

            Vector2 intersectPos = Vector2.zero;

            // top
            if (HMathf.SegmentIntersect (selfPos, otherPos, topLeft, topRight, ref intersectPos))
            {
                knob.side = NodeSide.Top;
                knob.sidePosition = (intersectPos.x - rect.xMin);
            }
            // right
            else if (HMathf.SegmentIntersect (selfPos, otherPos, topRight, bottomRight, ref intersectPos))
            {
                knob.side = NodeSide.Right;
                knob.sidePosition = (intersectPos.y - rect.yMin);
            }
            // bottom
            else if (HMathf.SegmentIntersect (selfPos, otherPos, bottomRight, bottomLeft, ref intersectPos))
            {
                knob.side = NodeSide.Bottom;
                knob.sidePosition = (intersectPos.x - rect.xMin);
            }
            // left
            else if (HMathf.SegmentIntersect (selfPos, otherPos, bottomLeft, topLeft, ref intersectPos))
            {
                knob.side = NodeSide.Left;
                knob.sidePosition = (intersectPos.y - rect.yMin);
            }
        }
        else
        {
            knob.side = defaultSide;
            knob.sidePosition = defaultOffset;
        }
    }

    private void UpdateConditionInfo ()
    {
        long current = DateTimeOffset.Now.ToUnixTimeSeconds ();

        if (current > _nextUpdateTime)
        {
            _nextUpdateTime = current + Random.Range (2, 5);

            UpdateStateInfoFields ();
        }
    }

    private StateInfoNode FindStateInfoNode ()
    {
        if (NodeEditor.curNodeCanvas?.nodes == null) return null;

        foreach (var node in NodeEditor.curNodeCanvas.nodes)
        {
            if (node.Title == "StateInfo")
            {
                return node as StateInfoNode;
            }
        }

        return null;
    }

    private void UpdateStateInfoFields ()
    {
        _stateInfoNode = FindStateInfoNode ();

        if (_stateInfoNode == null)
        {
            Debug.LogError ("Must have a state info node!");
            return;
        }

        var fields = _stateInfoNode.StateInfo.GetType ().GetFields ();

        _stateInfoFields = fields.Where (x => x.FieldType == typeof (int) ||
            x.FieldType == typeof (float) || x.FieldType == typeof (bool)).ToArray ();
    }

    private void DisplayConditionElement (Rect rect, int index, bool isActive, bool isFocused)
    {
        if (_stateInfoFields == null) UpdateStateInfoFields ();

        var element = _conditionEditorList.serializedProperty.GetArrayElementAtIndex (index);

        int variableIdx = EditorGUI.Popup (
            new Rect (rect.x, rect.y, 100, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative ("FieldIdx").intValue,
            _stateInfoFields.Select (x => x.Name).ToArray ());

        ConditionType conditionType;
        if (_stateInfoFields[variableIdx].FieldType == typeof (int))
        {
            conditionType = ConditionType.Int;
        }
        else if (_stateInfoFields[variableIdx].FieldType == typeof (float))
        {
            conditionType = ConditionType.Float;
        }
        else
        {
            conditionType = ConditionType.Bool;
        }

        element.FindPropertyRelative ("ConditionType").enumValueIndex = (int) conditionType;
        element.FindPropertyRelative ("FieldIdx").intValue = variableIdx;

        switch (conditionType)
        {
            case ConditionType.Int:

            EditorGUI.PropertyField (
            new Rect (rect.x + 105, rect.y, 50, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative ("IntConditionType"), GUIContent.none);

            EditorGUI.PropertyField (
            new Rect (rect.x + 160, rect.y, 50, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative ("IntTargetValue"), GUIContent.none);
            break;

            case ConditionType.Float:

            EditorGUI.PropertyField (
            new Rect (rect.x + 105, rect.y, 50, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative ("FloatConditionType"), GUIContent.none);

            EditorGUI.PropertyField (
            new Rect (rect.x + 160, rect.y, 50, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative ("FloatTargetValue"), GUIContent.none);
            break;

            case ConditionType.Bool:

            EditorGUI.PropertyField (
            new Rect (rect.x + 105, rect.y, 100, EditorGUIUtility.singleLineHeight),
            element.FindPropertyRelative ("BoolConditionType"), GUIContent.none);
            break;
        }
    }

    private void DisplayConditionHealder (Rect rect)
    {
        EditorGUI.LabelField (rect, "Conditions");
    }

    private void CreateConditionEditor (ReorderableList list)
    {
        UpdateStateInfoFields ();

        _serializedObject.Update ();

        int index = list.serializedProperty.arraySize;
        list.serializedProperty.arraySize++;
        list.index = index;

        _serializedObject.ApplyModifiedProperties ();

        var firstEleVal = _stateInfoFields[0];

        ConditionType conditionType;

        if (firstEleVal.FieldType == typeof (int))
        {
            conditionType = ConditionType.Int;
        }
        else if (firstEleVal.FieldType == typeof (float))
        {
            conditionType = ConditionType.Float;
        }
        else
        {
            conditionType = ConditionType.Bool;
        }

        var element = list.serializedProperty.GetArrayElementAtIndex (index);

        element.FindPropertyRelative ("ConditionType").enumValueIndex = (int) conditionType;
        element.FindPropertyRelative ("Name").stringValue = firstEleVal.Name;
        element.FindPropertyRelative ("FieldIdx").intValue = 0;

        switch (conditionType)
        {
            case ConditionType.Int:
            element.FindPropertyRelative ("IntConditionType").enumValueIndex = 0;
            element.FindPropertyRelative ("IntTargetValue").intValue = 0;
            break;
            case ConditionType.Float:
            element.FindPropertyRelative ("FloatConditionType").enumValueIndex = 0;
            element.FindPropertyRelative ("FloatTargetValue").floatValue = 0;
            break;
            case ConditionType.Bool:
            element.FindPropertyRelative ("BoolConditionType").enumValueIndex = 0;
            break;
        }
    }

    #endregion
}

#region Condition Connection 

public class ConditionTransOutType : ConnectionKnobStyle
{
    public override string Identifier => "ConditionTransOut";

    public override Color Color => Color.cyan;    
}

public class ConditionTransInType : ConnectionKnobStyle
{
    public override string Identifier => "ConditionTransIn";

    public override Color Color => Color.black;    
}

#endregion

#region Condition Type

public enum ConditionType
{
    Int,
    Float,
    Bool
}

public enum ConditionInt
{
    Greater,
    Less,
    Equals,
    NotEqual
}

public enum ConditionFloat
{
    Greater,
    Less
}

public enum ConditionBool
{
    True,
    False
}

[Serializable]
public class ConditionEditor
{
    public ConditionType ConditionType;

    public string Name;

    public int FieldIdx;

    public ConditionInt IntConditionType;
    public int IntTargetValue;

    public ConditionFloat FloatConditionType;
    public float FloatTargetValue;

    public ConditionBool BoolConditionType;
}

[Serializable]
public class MutiConditionEditor : ScriptableObject
{
    public List<ConditionEditor> Conditions;
}

#endregion