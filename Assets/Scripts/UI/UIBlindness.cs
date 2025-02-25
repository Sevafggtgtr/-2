using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class UIBlindness : MonoBehaviour
{
    private Animation _animation;

    void Start()
    {
        _animation = GetComponent<Animation>();
    }

    public void Activate(float time)
    {
        _animation.Play("Blindness_in");

        IEnumerator Interval()
        {
            yield return new WaitForSeconds(time);

            _animation.Play("Blindness_out");
        }

        StartCoroutine(Interval());
    }

    void Update()
    {
        
    }
}
