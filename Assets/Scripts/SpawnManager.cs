using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnManager : MonoBehaviour
{
    public GameObject[] _enemes;
    private int _index;

    void Start()
    {
        InvokeRepeating("Spawn", 2, 60f);
    }


    void Spawn()
    {
        Vector3 position = new Vector3(Random.Range(10, 30), 0, Random.Range(20, -40));
        _index = Random.Range(0, _enemes.Length);
        Instantiate(_enemes[_index], position, _enemes[_index].transform.rotation);
    }
}
