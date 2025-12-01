using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPoolManager : MonoBehaviour
{
    [Header("Prefabs Settings")]
    [SerializeField]private GameObject[] poolPrefabs;

    [Header("Pool Settings")]
    [SerializeField]private Transform sceneObjectPool;
    List<GameObject> pooledObjects = new List<GameObject>();
    [SerializeField]private int amountForEach = 10;

    [Header("Pool Settings")]
    [SerializeField]private bool randomSpawn = true;
    [SerializeField]private Transform[] refTransforms;
    [SerializeField]private Transform[] initializeRefTransforms;
    [SerializeField]private float sizeMin, sizeMax;
    [SerializeField]private int initialAmount = 10;
    [SerializeField]private float spawnTimeMin, spawnTimeMax;

    IEnumerator cor;

    void Start()
    {
        InitailizeObjectPool();
        InitailizeScene();
        SetRandomSpawn(randomSpawn);
    }

    void InitailizeObjectPool()
    {
        foreach(var item in poolPrefabs)
        {
            for(int i = 0; i < amountForEach; i++)
            {
                var newSpawned = Instantiate(item, Vector3.zero, item.transform.rotation, sceneObjectPool);
                newSpawned.SetActive(false);
                pooledObjects.Add(newSpawned);
            }
        }
    }
    public GameObject GetPooledObject()
    {
        var usableObjects = new List<GameObject>();
        for(int i = 0; i < pooledObjects.Count; i++)
        {
            if(!pooledObjects[i].gameObject.activeSelf)
            {
                usableObjects.Add(pooledObjects[i]);
            }
        }

        if(usableObjects.Count > 0)
        {
            return usableObjects[Random.Range(0, usableObjects.Count)];
        }
        else
        {
            return null;
        }
    }

    public void SetRandomSpawn(bool value)
    {
        randomSpawn = value;
        if(value)
        {
            cor = RandomSpawn();
            StartCoroutine(cor);
        }
        else
        {
            if(cor != null) StopCoroutine(cor);
        }
    }
    IEnumerator RandomSpawn()
    {
        while(randomSpawn)
        {
            var obj = GetPooledObject();
            if(!obj)
            {
                yield return new WaitForSeconds(Random.Range(spawnTimeMin, spawnTimeMax));
            }
            SetObjectActive(obj);

            yield return new WaitForSeconds(Random.Range(spawnTimeMin, spawnTimeMax));
        }
        yield return null;
    }

    void InitailizeScene()
    {
        for(int i = 0; i < initialAmount; i++)
        {
            var obj = GetPooledObject();
            SetObjectActive(obj, true);
        }
    }

    void SetObjectActive(GameObject m_obj, bool initialize = false)
    {
        bool refGroup = (Random.Range(0, 1f) > 0.5f)? true : false;
        var m_refTransforms = initialize? initializeRefTransforms: refTransforms;
        var pos1 = refGroup? m_refTransforms[0].position: m_refTransforms[2].position;
        var pos2 = refGroup? m_refTransforms[1].position: m_refTransforms[3].position;
        
        
        Vector3 spawnPos = new Vector3(Random.Range(pos1.x, pos2.x), Random.Range(pos1.y, pos2.y), Random.Range(pos1.z, pos2.z));

        m_obj.transform.position = spawnPos;
        m_obj.transform.rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360f), 0));
        m_obj.transform.localScale = new Vector3(Random.Range(sizeMin, sizeMax), Random.Range(sizeMin, sizeMax), Random.Range(sizeMin, sizeMax));
        m_obj.SetActive(true);
    }
}
