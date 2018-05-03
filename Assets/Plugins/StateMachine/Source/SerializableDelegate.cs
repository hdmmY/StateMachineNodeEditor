using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

[Serializable]
public class SerializableDelegate<T> where T : class
{
    [SerializeField]
    private UnityEngine.Object _target;
    [SerializeField]
    private string _methodName = "";
    [SerializeField]
    private byte[] _serialData = { };

    static SerializableDelegate ()
    {
        if (!typeof (T).IsSubclassOf (typeof (Delegate)))
        {
            throw new InvalidOperationException (typeof (T).Name + " is not a delegate type.");
        }
    }

    public void SetDelegate (T action)
    {
        if (action == null)
        {
            _target = null;
            _methodName = "";
            _serialData = new byte[] { };
            return;
        }

        var delAction = action as Delegate;
        if (delAction == null)
        {
            throw new InvalidOperationException (typeof (T).Name + " is not a delegate type.");
        }

        _target = delAction.Target as UnityEngine.Object;

        if (_target != null)
        {
            _methodName = delAction.Method.Name;
            _serialData = null;
        }
        else
        {
            //Serialize the data to a binary stream
            using (var stream = new MemoryStream ())
            {
                (new BinaryFormatter ()).Serialize (stream, action);
                stream.Flush ();
                _serialData = stream.ToArray ();
            }
            _methodName = null;
        }
    }

    public T CreateDelegate ()
    {
        if (_serialData.Length == 0 && _methodName == "")
        {
            return null;
        }

        if (_target != null)
        {
            return Delegate.CreateDelegate (typeof (T), _target, _methodName) as T;
        }

        using (var stream = new MemoryStream (_serialData))
        {
            return (new BinaryFormatter ()).Deserialize (stream) as T;
        }
    }
}