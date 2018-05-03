using System;
using NodeEditorFramework;
using UnityEditor;
using UnityEngine;

[Node (false, "State/StateInfoNode", new Type[] { typeof (StateMachineCanvas) })]
public class StateInfoNode : Node
{
    private const string ID = "StateInfoNode";

    public override string GetID => ID;

    public override string Title => "StateInfo";

    public override Color TitleColor => Color.black;

    public override bool AutoLayout => true;

    public override Vector2 MinSize => new Vector2 (200, 90);

    public BaseStateInfo StateInfo;

    public BaseState StateScript;

    #region Node GUI 

    public override void NodeGUI ()
    {
        GUILayout.Space (8);

        StateInfo = EditorGUILayout.ObjectField (StateInfo, typeof (BaseStateInfo),
            false) as BaseStateInfo;

        StateScript = EditorGUILayout.ObjectField (StateScript, typeof (BaseState),
            false) as BaseState;
    }

    #endregion
}