using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachineRunner : MonoBehaviour
{
    #region Public methods

    public StateMachine Initialize (BaseState stateScript, string[] states)
    {
        var fsm = new StateMachine (this, stateScript, states);

        _stateMachineList.Add (fsm);

        return fsm;
    }

    public StateMachine Initialize (BaseState stateScript, string[] states, int startState)
    {
        var fsm = Initialize (stateScript, states);

        if (startState >= states.Length) return fsm;

        fsm.ChangeState (states[startState]);

        return fsm;
    }

    #endregion 

    #region Monobehavior method

    private void FixedUpdate ()
    {
        for (int i = 0; i < _stateMachineList.Count; i++)
        {
            var fsm = _stateMachineList[i];

            if (!fsm.IsInTransition && fsm.StateScript.enabled)
            {
                fsm.CurrentStateMap.FixedUpdate ();
            }
        }
    }

    private void Update ()
    {
        for (int i = 0; i < _stateMachineList.Count; i++)
        {
            var fsm = _stateMachineList[i];

            if (!fsm.IsInTransition && fsm.StateScript.enabled)
            {
                fsm.CurrentStateMap.Update ();
            }
        }
    }

    private void LateUpdate ()
    {
        for (int i = 0; i < _stateMachineList.Count; i++)
        {
            var fsm = _stateMachineList[i];

            if (!fsm.IsInTransition && fsm.StateScript.enabled)
            {
                fsm.CurrentStateMap.LateUpdate ();
            }
        }
    }

    #endregion

    private List<IStateMachine> _stateMachineList = new List<IStateMachine> ();
}