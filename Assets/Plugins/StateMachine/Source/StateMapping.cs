using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class StateMapping: 　ISerializationCallbackReceiver
{
    public static void DoNothing () { }

    public static IEnumerator DoNothingCoroutine () { yield break; }

    public string State;

    public bool hasEnterRoutine;
    public Action EnterCall = DoNothing;
    public Func<IEnumerator> EnterRoutine = DoNothingCoroutine;

    public bool hasExitRoutine;
    public Action ExitCall = DoNothing;
    public Func<IEnumerator> ExitRoutine = DoNothingCoroutine;

    public Action Finally = DoNothing;

    public Action Update = DoNothing;

    public Action LateUpdate = DoNothing;

    public Action FixedUpdate = DoNothing;

    public StateMapping (string state)
    {
        State = state;
    }

    #region Serialize

    [Serializable]
    private class ActionSerializer : SerializableDelegate<Action> { }

    [Serializable]
    private class FuncIEnumeratorSerializer : SerializableDelegate<Func<IEnumerator>> { }

    private ActionSerializer _enterCall = new ActionSerializer ();
    private ActionSerializer _exitCall = new ActionSerializer ();
    private ActionSerializer _finally = new ActionSerializer ();
    private ActionSerializer _update = new ActionSerializer ();
    private ActionSerializer _lateUpdate = new ActionSerializer ();
    private ActionSerializer _fixedUpdate = new ActionSerializer ();
    private FuncIEnumeratorSerializer _enterRoutine = new FuncIEnumeratorSerializer ();
    private FuncIEnumeratorSerializer _exitRoutine = new FuncIEnumeratorSerializer ();

    public void OnBeforeSerialize ()
    {
        _enterCall.SetDelegate (EnterCall);
        _exitCall.SetDelegate (ExitCall);
        _finally.SetDelegate (Finally);
        _update.SetDelegate (Update);
        _lateUpdate.SetDelegate (LateUpdate);
        _fixedUpdate.SetDelegate (FixedUpdate);
        _enterRoutine.SetDelegate (EnterRoutine);
        _exitRoutine.SetDelegate (ExitRoutine);
    }

    public void OnAfterDeserialize ()
    {
        EnterCall = _enterCall.CreateDelegate ();
        ExitCall = _exitCall.CreateDelegate ();
        Finally = _finally.CreateDelegate ();
        Update = _update.CreateDelegate ();
        LateUpdate = _lateUpdate.CreateDelegate ();
        FixedUpdate = _fixedUpdate.CreateDelegate ();
        EnterRoutine = _enterRoutine.CreateDelegate ();
        ExitRoutine = _exitRoutine.CreateDelegate ();
    }

    #endregion

}