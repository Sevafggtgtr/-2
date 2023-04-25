using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawn : MonoBehaviour
{
    [SerializeField]
    private GameObject enemy;
    [SerializeField]
    public int waveCount = 1;

    void Start()
    {
        SpawnEnemy(waveCount);
    }


    void SpawnEnemy(int enemiesToSpawn)
    {
        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Instantiate(enemy, positionSpawn(), enemy.transform.rotation);
        }
    }

    private Vector3 positionSpawn()
    {
        float posX = Random.Range(-10, 60);
        float posZ = Random.Range(20, -40);

        return new Vector3(posX, 0, posZ);
    }

    private void Update()
    {
        GameObject[] enemys = GameObject.FindGameObjectsWithTag("Enemy");
        int enemyCount = enemys.Length;
        if (enemyCount == 0)
        {
            waveCount++;
            SpawnEnemy(waveCount);
        }
    }
}
