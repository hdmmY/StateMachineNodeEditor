using System.Collections;
using UnityEngine;

[System.Serializable]
public class SMS_NPC_001 : BaseState
{
    #region Attack State

    private void Attack_Enter ()
    {
        Debug.Log ("Enter Attack State");
    }

    public void Attack_Update ()
    {
        Debug.Log ("Update Attack State");
    }

    public IEnumerator Attack_Exit ()
    {
        Debug.Log ("Exit Attack State");
        yield break;
    }

    #endregion

    #region Move State 

    private IEnumerator Move_Enter ()
    {
        float timer = 0f;

        while (timer < 2f)
        {
            timer += Time.deltaTime;
            Debug.LogFormat ("Enter Move State. Timer : {0}", timer);
            yield return null;
        }
    }

    private void Move_Update ()
    {
        Debug.Log ("Update Move State");
    }

    private void Move_LateUpdate ()
    {
        Debug.Log ("LateUpdate Move State");
    }

    private void Move_Exit ()
    {
        Debug.Log ("Exit Move State");
    }

    private void Move_Finally ()
    {
        Debug.Log ("Finally Move State");
    }

    #endregion

    #region Speak State

    private void Speek_Update()
    {
        char[] word = new char[30];

        for(int i = 0; i < 30; i++)
        {
            word[i] = (char)(Random.Range(0, 26) + 'a');
        }

        Debug.Log(new string(word));
    }


    #endregion




}