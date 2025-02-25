using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIButton : MonoBehaviour
{
    public event UnityAction OnClick;

    public Button Button { get; private set; }

    void Start()
    {
        Button = GetComponent<Button>(); 

        Button.onClick.AddListener(OnClick);
    }

    void Update()
    {
        
    }
}
