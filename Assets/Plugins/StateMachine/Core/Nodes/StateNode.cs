using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

[Node (false, "State/State Node", new Type[] { typeof (StateMachineCanvas) })]
public class StateNode : BaseStateNode
{
    #region Node GUI Properties

    private const string ID = "StateNode";

    public override string GetID => ID;

    public override Vector2 DefaultSize => new Vector2 (150, 100);

    public readonly ConnectionKnobAttribute StateTransOutAttribute =
        new ConnectionKnobAttribute ("Out", Direction.Out, "StateTransOut", ConnectionCount.Single);

    public readonly ConnectionKnobAttribute StateTransInAttribute =
        new ConnectionKnobAttribute ("In", Direction.In, "StateTransIn", ConnectionCount.Single);

    public static readonly Color DefaultStateColor = new Color (131 / 255f, 234 / 255f, 243 / 255f, 255 / 255f);

    public static readonly Color NormalStateColor = new Color (255 / 255f, 255 / 255f, 255 / 255f, 255 / 255f);

    #endregion

    #region Node GUI

    protected override void OnCreate ()
    {
        StateName = "Null State";

        bool haveDefaultState = false;

        if (NodeEditor.curNodeCanvas.nodes != null)
        {
            foreach (var node in NodeEditor.curNodeCanvas.nodes)
            {
                if (node is StateNode && (node as StateNode).IsDefaultState)
                {
                    haveDefaultState = true;
                    break;
                }
            }
        }

        if (!haveDefaultState) IsDefaultState = true;
    }

    protected internal override void OnDelete ()
    {
        if (NodeEditor.curNodeCanvas.nodes.Count == 1) return;
        if (!IsDefaultState) return;

        foreach (var node in NodeEditor.curNodeCanvas.nodes)
        {
            if (node is StateNode && (node as StateNode).IsDefaultState == false)
            {
                (node as StateNode).IsDefaultState = true;
                break;
            }
        }
    }

    // Create a condition node
    protected override internal void OnAddConnection (ConnectionPort port, ConnectionPort connection)
    {
        if (port.styleID == "StateTransIn") return;
        if (connection.body.GetType () == typeof (ConditionNode)) return;

        Vector2 pos = ((port as ConnectionKnob).GetCanvasSpaceKnob ().position +
            (connection as ConnectionKnob).GetCanvasSpaceKnob ().position) / 2;

        var conditionNode = Node.Create ("ConditionNode", pos) as ConditionNode;
        conditionNode.position -= conditionNode.size / 2;

        if (port.CanApplyConnection (conditionNode.ConditionTransIn))
        {
            port.ApplyConnection (conditionNode.ConditionTransIn);
        }

        connection.RemoveConnection (port);
        if (connection.CanApplyConnection (conditionNode.ConditionTransOut))
        {
            connection.ApplyConnection (conditionNode.ConditionTransOut);
        }
    }

    public override void NodeGUI ()
    {
        GUILayout.Space (3);

        GUIStyle labelStyle = new GUIStyle (GUI.skin.label);
        labelStyle.normal.textColor = new Color (1, 1, 1, 0.85f);
        EditorGUI.LabelField (new Rect (0, 0, 150, 50), StateDescription, labelStyle);

        int leftHeight = 0;
        int rightHeight = 0;

        foreach (var port in dynamicConnectionPorts)
        {
            if (port.styleID == "StateTransOut")
            {
                UpdateConnectKnobPosition (port as ConnectionKnob, NodeSide.Left, leftHeight);
                leftHeight += 5;
            }
            else
            {
                UpdateConnectKnobPosition (port as ConnectionKnob, NodeSide.Right, rightHeight);
                rightHeight += 5;
            }
        }

        if (IsDefaultState)
        {
            backgroundColor = DefaultStateColor;
        }
        else
        {
            backgroundColor = NormalStateColor;
        }
    }

    public override void DrawNodePropertyEditor ()
    {
        GUILayout.Space (8);

        EditorGUIUtility.labelWidth = 70;

        StateName = EditorGUILayout.TextField ("Name", StateName);

        GUILayout.Space (3);
        EditorGUILayout.LabelField ("Description");
        StateDescription = EditorGUILayout.TextArea (StateDescription);

        // Draw methods

        UpdateMethodInfo ();

        GUILayout.Space (10);

        string methodName;

        if (_stateInfoNode != null && StateMethods != null)
        {
            if (StateMethods.TryGetValue ("Enter", out methodName))
            {
                DrawReadonlyTextField ("Enter", methodName);
            }

            if (StateMethods.TryGetValue ("Update", out methodName))
            {
                DrawReadonlyTextField ("Update", methodName);
            }

            if (StateMethods.TryGetValue ("LateUpdate", out methodName))
            {
                DrawReadonlyTextField ("LateUpdate", methodName);
            }

            if (StateMethods.TryGetValue ("FixedUpdate", out methodName))
            {
                DrawReadonlyTextField ("FixedUpdate", methodName);
            }

            if (StateMethods.TryGetValue ("Exit", out methodName))
            {
                DrawReadonlyTextField ("Exit", methodName);
            }

            if (StateMethods.TryGetValue ("Finally", out methodName))
            {
                DrawReadonlyTextField ("Finally", methodName);
            }
        }
    }

    #endregion

    #region Public variables

    public Dictionary<string, string> StateMethods;

    /// <summary>
    /// Whether this state is the default state
    /// </summary>
    public bool IsDefaultState;

    #endregion

    #region  Private variables and methods

    private StateInfoNode _stateInfoNode;

    private long _nextUpdateTime;

    private void UpdateMethodInfo ()
    {
        long current = DateTimeOffset.Now.ToUnixTimeSeconds ();

        if (current > _nextUpdateTime)
        {
            _nextUpdateTime = current + Random.Range (2, 5);

            SetStateMethods ();
        }
    }

    private void SetStateMethods ()
    {
        _stateInfoNode = FindStateInfoNode ();

        if (_stateInfoNode == null) return;
        if (_stateInfoNode.StateScript == null) return;

        if (StateMethods == null) StateMethods = new Dictionary<string, string> ();
        StateMethods.Clear ();

        var methods = _stateInfoNode.StateScript.GetType ().GetMethods (BindingFlags.Instance |
            BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].GetCustomAttributes (typeof (CompilerGeneratedAttribute), true).Length != 0)
            {
                continue;
            }

            int lastIdx = methods[i].Name.LastIndexOf ('_');

            if (lastIdx < 0) continue;

            if (methods[i].Name.Substring (0, lastIdx) != StateName) continue;

            switch (methods[i].Name.Substring (lastIdx + 1))
            {
                case "Enter":
                    StateMethods["Enter"] = methods[i].Name;
                    break;
                case "Exit":
                    StateMethods["Exit"] = methods[i].Name;
                    break;
                case "Finally":
                    StateMethods["Finally"] = methods[i].Name;
                    break;
                case "Update":
                    StateMethods["Update"] = methods[i].Name;
                    break;
                case "LateUpdate":
                    StateMethods["LateUpdate"] = methods[i].Name;
                    break;
                case "FixedUpdate":
                    StateMethods["FixedUpdate"] = methods[i].Name;
                    break;
            }
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

    private void DrawReadonlyTextField (string label, string text)
    {
        EditorGUILayout.BeginHorizontal ();
        EditorGUILayout.LabelField (label, GUILayout.Width (EditorGUIUtility.labelWidth - 4));
        EditorGUILayout.SelectableLabel (text, EditorStyles.textField,
            GUILayout.Height (EditorGUIUtility.singleLineHeight));
        EditorGUILayout.EndHorizontal ();
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

    #endregion
}