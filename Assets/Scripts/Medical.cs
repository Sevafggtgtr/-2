using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Medical : MonoBehaviour
{


    void Start()
    {
        
    }

    void Update()
    {
        
    }

    IEnumerator MedicalCourutine()
    {
        yield return new WaitForSeconds(5);
    }
}
