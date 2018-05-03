using System;
using NodeEditorFramework;
using UnityEngine;

[Node (true, "State/BaseStateNode", new Type[] { typeof (StateMachineCanvas) })]
public abstract class BaseStateNode : Node
{
    public override string Title => StateName;

    public override bool AutoLayout => false;

    public override bool AllowRecursion => true;

    public override Color TitleColor => Color.black;

    public string StateName;

    public string StateDescription;
}

#region Connection knob style

public class StateTransOutType : ConnectionKnobStyle
{
    public override string Identifier => "StateTransOut";
    public override Color Color => Color.black;
}

public class StateTransInType : ConnectionKnobStyle
{
    public override string Identifier => "StateTransIn";
    public override Color Color => Color.cyan;
}

#endregion