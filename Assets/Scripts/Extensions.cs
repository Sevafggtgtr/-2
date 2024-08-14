using System.Collections;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public static class Extensions
{
    public static IEnumerator Timer(float time, UnityAction callback)
    {
        yield return new WaitForSeconds(time);

        callback.Invoke();
    }
}
   

   
