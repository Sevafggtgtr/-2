using UnityEngine;
using UnityEngine.UI;

public class NicknameInputField : MonoBehaviour
{
    private string _oldText;

    private InputField _inputField;

    void Start()
    {
        _inputField = GetComponent<InputField>();

        _oldText = _inputField.text;

        _inputField.onSubmit.AddListener(call =>
        {
            if (call == "")
                _inputField.text = _oldText;
            else
            {
                _oldText = call;
            }
        });
    }


    void Update()
    {
        
    }
}
