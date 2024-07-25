using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    public event UnityAction OnClick;

    protected Button _button;
    void Start()
    {
        _button = GetComponent<Button>(); 

        _button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        
    }
}
