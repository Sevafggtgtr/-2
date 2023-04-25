using UnityEngine;
using UnityEngine.UI;

public class UIKillfeedSlot : MonoBehaviour
{
    [SerializeField]
    private Text _killerText,
                 _targetText;

    public void Initialize(string killer,string target)
    {
        _killerText.text = killer;
        _targetText.text = target;

        Invoke("Destroy", 5);
    }

    private void Destroy()
    {
        Destroy(gameObject);
    }
}
