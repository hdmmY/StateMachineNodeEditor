using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Object = System.Object;
using System.Linq;

public enum StateTransition
{
    Safe,
    Overwrite,
}

public interface IStateMachine
{
    BaseState StateScript { get; }
    StateMapping CurrentStateMap { get; }
    bool IsInTransition { get; }
}

public class StateMachine : IStateMachine
{
    #region Public variables 

    // Broadcast change only after enter transition
    // Parameter : change to state's name
    public event Action<string> Changed;

    public string LastState => _lastState?.State;

    public string State => _currentState?.State;

    public bool IsInTransition => _isInTransition;

    public StateMapping CurrentStateMap => _currentState;

    public BaseState StateScript => _stateScript;

    #endregion

    #region  Public instance method

    public StateMachine (StateMachineRunner engine, BaseState stateScript, string[] states)
    {
        _engine = engine;
        _stateScript = stateScript;

        _stateLookup = new Dictionary<string, StateMapping> ();
        foreach (var state in states)
        {
            var mapping = new StateMapping (state);
            _stateLookup.Add (state, mapping);
        }

        // Reflect methods
        var methods = _stateScript.GetType ().GetMethods (BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.Public |
            BindingFlags.NonPublic);

        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].GetCustomAttributes (typeof (CompilerGeneratedAttribute), true).Length != 0)
            {
                continue;
            }

            int lastIdx = methods[i].Name.LastIndexOf ('_');
            if (lastIdx < 0) continue;

            string name = methods[i].Name.Substring (0, lastIdx);
            if (!states.Contains (name)) continue;

            StateMapping targetState = _stateLookup[name];

            switch (methods[i].Name.Substring (lastIdx + 1))
            {
                case "Enter":
                    if (methods[i].ReturnType == typeof (IEnumerator))
                    {
                        targetState.hasEnterRoutine = true;
                        targetState.EnterRoutine = CreateDelegate<Func<IEnumerator>> (methods[i], _stateScript);
                    }
                    else
                    {
                        targetState.hasEnterRoutine = false;
                        targetState.EnterCall = CreateDelegate<Action> (methods[i], _stateScript);
                    }
                    break;
                case "Exit":
                    if (methods[i].ReturnType == typeof (IEnumerator))
                    {
                        targetState.hasExitRoutine = true;
                        targetState.ExitRoutine = CreateDelegate<Func<IEnumerator>> (methods[i], _stateScript);
                    }
                    else
                    {
                        targetState.hasExitRoutine = false;
                        targetState.ExitCall = CreateDelegate<Action> (methods[i], _stateScript);
                    }
                    break;
                case "Finally":
                    targetState.Finally = CreateDelegate<Action> (methods[i], _stateScript);
                    break;
                case "Update":
                    targetState.Update = CreateDelegate<Action> (methods[i], _stateScript);
                    break;
                case "LateUpdate":
                    targetState.LateUpdate = CreateDelegate<Action> (methods[i], _stateScript);
                    break;
                case "FixedUpdate":
                    targetState.FixedUpdate = CreateDelegate<Action> (methods[i], _stateScript);
                    break;
            }
        }

        _currentState = new StateMapping (null);
    }

    public void ChangeState (string newState)
    {
        ChangeState (newState, StateTransition.Safe);
    }

    public void ChangeState (string newState, StateTransition transition)
    {
        if (_stateLookup == null)
        {
            throw new Exception ("States have not been configured, please call initialized before trying to set state");
        }

        if (!_stateLookup.ContainsKey (newState))
        {
            throw new Exception ("No state with the name " + newState.ToString () + " can be found. Please make sure you are called the correct type the statemachine was initialized with");
        }

        StateMapping nextState = _stateLookup[newState];

        // Cancel any queued changes
        if (_queuedChange != null)
        {
            _engine.StopCoroutine (_queuedChange);
            _queuedChange = null;
        }

        switch (transition)
        {
            case StateTransition.Safe:
                if (_isInTransition)
                {
                    //Already exiting current state on our way to our previous target state
                    if (_exitRoutine != null)
                    {
                        _destinationState = nextState;
                        return;
                    }

                    if (_enterRoutine != null)
                    {
                        _queuedChange = WaitForPreviousTransition (nextState);
                        _engine.StartCoroutine (_queuedChange);
                        return;
                    }
                }
                break;
            case StateTransition.Overwrite:
                if (_currentTransition != null)
                {
                    _engine.StopCoroutine (_currentTransition);
                    _currentTransition = null;
                }
                if (_exitRoutine != null)
                {
                    _engine.StopCoroutine (_exitRoutine);
                    _exitRoutine = null;
                }
                if (_enterRoutine != null)
                {
                    _engine.StopCoroutine (_enterRoutine);
                    _enterRoutine = null;
                }
                break;
        }

        if ((_currentState != null && _currentState.hasExitRoutine) || nextState.hasEnterRoutine)
        {
            _isInTransition = true;
            _currentTransition = ChangeToNewStateRoutine (nextState, transition);
            _engine.StartCoroutine (_currentTransition);
        }
        else //Same frame transition, no coroutines are present
        {
            if (_currentState != null)
            {
                _currentState.ExitCall ();
                _currentState.Finally ();
            }

            _lastState = _currentState;
            _currentState = nextState;

            if (_currentState != null)
            {
                _currentState.EnterCall ();

                if (Changed != null)
                {
                    Changed (_currentState.State);
                }
            }

            _isInTransition = false;
        }
    }

    #endregion

    #region Public static method

    /// <summary>
    /// Inspects a base state script for state methods as definied by the supplied state name , and returns a stateMachine instance used to trasition states.
    /// </summary>
    /// <param name="stateScript">The component with defined state methods</param>
    /// <returns>A valid stateMachine instance to manage state transitions</returns>
    public static StateMachine Initialize (BaseState stateScript, string[] states)
    {
        var engine = stateScript.GetComponent<StateMachineRunner> ();
        if (engine == null) engine = stateScript.gameObject.AddComponent<StateMachineRunner> ();

        return engine.Initialize (stateScript, states);
    }

    /// <summary>
    /// Inspects a base state script for state methods as definied by the supplied state name , and returns a stateMachine instance used to trasition states.
    /// </summary>
    /// <param name="stateScript">The component with defined state methods</param>
    /// <param name="startState">The default starting state</param>/// 
    /// <returns>A valid stateMachine instance to manage state transitions</returns>
    /// <summary>
    public static StateMachine Initialize (BaseState stateScript, string[] states, int startState)
    {
        var engine = stateScript.GetComponent<StateMachineRunner> ();
        if (engine == null) engine = stateScript.gameObject.AddComponent<StateMachineRunner> ();

        return engine.Initialize (stateScript, states, startState);
    }

    #endregion

    #region Private variables

    private StateMachineRunner _engine;

    private BaseState _stateScript;

    private StateMapping _lastState;

    private StateMapping _currentState;

    private StateMapping _destinationState;

    private Dictionary<string, StateMapping> _stateLookup; // state name -> state mapping funcs

    private bool _isInTransition;

    private IEnumerator _currentTransition;

    private IEnumerator _enterRoutine;

    private IEnumerator _exitRoutine;

    private IEnumerator _queuedChange;

    #endregion

    #region Private helper method

    private V CreateDelegate<V> (MethodInfo method, Object target) where V : class
    {
        var ret = (Delegate.CreateDelegate (typeof (V), target, method) as V);

        if (ret == null)
        {
            throw new ArgumentException ("Unabled to create delegate for method called " + method.Name);
        }
        return ret;

    }

    private IEnumerator WaitForPreviousTransition (StateMapping nextState)
    {
        while (_isInTransition)
        {
            yield return null;
        }

        ChangeState (nextState.State);
    }

    private IEnumerator ChangeToNewStateRoutine (StateMapping newState, StateTransition transtiion)
    {
        _destinationState = newState;

        if (_currentState != null)
        {
            if (_currentState.hasExitRoutine)
            {
                _exitRoutine = _currentState.ExitRoutine ();

                if (_exitRoutine != null && transtiion != StateTransition.Overwrite)
                {
                    yield return _engine.StartCoroutine (_exitRoutine);
                }

                _exitRoutine = null;
            }
            else
            {
                _currentState.ExitCall ();
            }

            _currentState.Finally ();
        }

        _lastState = _currentState;
        _currentState = _destinationState;

        if (_currentState != null)
        {
            if (_currentState.hasEnterRoutine)
            {
                _enterRoutine = _currentState.EnterRoutine ();

                if (_enterRoutine != null)
                {
                    yield return _engine.StartCoroutine (_enterRoutine);
                }

                _enterRoutine = null;
            }
            else
            {
                _currentState.EnterCall ();
            }

            if (Changed != null)
            {
                Changed (_currentState.State);
            }
        }

        _isInTransition = false;
    }

    #endregion

}