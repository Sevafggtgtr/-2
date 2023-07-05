using UnityEngine;
using UnityEngine.UI;

public class UIKillfeedPanel : MonoBehaviour
{
    [SerializeField]
    private UIKillfeedSlot _slotPrefab;

    void Start()
    {
        //foreach (IDamageableObject target in FindObjectsOfType<IDamageableObject>())
            //target.Died += (killer) => SpawnSlot(killer, target.Name);
    }

    public void SpawnSlot(string killer, string target)
    {
        Instantiate(_slotPrefab, transform).Initialize(killer, target);
    }


}
