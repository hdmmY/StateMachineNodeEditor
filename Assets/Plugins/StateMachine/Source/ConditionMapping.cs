using System;
using System.Collections;
using UnityEngine;

[Serializable]
public class ConditionMapping: 　ISerializationCallbackReceiver
{
    public Func<int, bool> ConditionInt;

    public Func<float, bool> ConditionFloat;

    public Func<bool, bool> ConditionBool;

    #region Serialize

    [Serializable]
    private class ConditionIntSerializer : SerializableDelegate<Func<int, bool>> { }

    [Serializable]
    private class ConditionFloatSerializer : SerializableDelegate<Func<float, bool>> { }

    [Serializable]
    private class ConditionBoolSerializer : SerializableDelegate<Func<bool, bool>> { }

    private ConditionIntSerializer _conditionInt;

    private ConditionFloatSerializer _conditionFloat;

    private ConditionBoolSerializer _conditionBool;

    public void OnBeforeSerialize ()
    {
        if (ConditionInt != null)
            _conditionInt.SetDelegate (ConditionInt);

        if (ConditionFloat != null)
            _conditionFloat.SetDelegate (ConditionFloat);

        if (ConditionBool != null)
            _conditionBool.SetDelegate (ConditionBool);
    }

    public void OnAfterDeserialize ()
    {
        if (_conditionInt != null)
            ConditionInt = _conditionInt.CreateDelegate ();

        if (_conditionFloat != null)
            ConditionFloat = _conditionFloat.CreateDelegate ();

        if (_conditionBool != null)
            ConditionBool = _conditionBool.CreateDelegate ();
    }

    #endregion

}