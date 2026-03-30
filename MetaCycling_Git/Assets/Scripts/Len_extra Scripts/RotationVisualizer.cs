using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Collections;
using UnityEngine;

public class RotationVisualizer : MonoBehaviour
{
    [Header ("Prefab Data")]
    [SerializeField] private GameObject prefab;             //prefab used for this visualization
    [SerializeField] private Vector3 prefabInitRotation;    //prefab rotation + this vector 3 as their initial rotation before adding the actual object rotation to the object

    [Header ("Visualizer Data")]
    [SerializeField] private float prefabDistance = 1f;     //the distance of each prefab while visualizing
    [SerializeField] private Vector3 graphDir;              

    [Header("Sampling Data")]
    [SerializeField] private GameObject target;             //sampling target
    [SerializeField] private int sampleAmount;              //max sampling amount
    [SerializeField] private float sampleInterval = 0.02f;  //interval between sampling

    [SerializeField] private Transform anchor;              //this gameobject acts as an anchor if null

    private Quaternion[] rotationSamples;                   //Rotation samples saved here for better visualization
    private GameObject[] prefabArray;                   //Rotation samples saved here for better visualization
    private int sampleIndex;
    private Coroutine samplingRoutine;                      //coroutine to make it easier to control outside the script

    private void Awake()
    {
        if (anchor == null) anchor = gameObject.transform;
    }

    private void Start()
    {
        rotationSamples = new Quaternion[sampleAmount];
        prefabArray = new GameObject[sampleAmount];
        Invoke("StartGraph", .5f);
    }

    #region Public Functions

    public void StartGraph()
    {
        if (samplingRoutine != null)
            return;

        Vector3 dir = graphDir.normalized;
        if(dir == Vector3.zero)
            dir = Vector3.right;

        for (int i = 0; i < prefabArray.Length; i++)
        {
            Vector3 pos = anchor.position + dir * prefabDistance * i;

            prefabArray[i] = Instantiate(prefab,pos,Quaternion.Euler(prefabInitRotation),anchor);
        }
        sampleIndex = 0;

        samplingRoutine = StartCoroutine(SampleMotion());
    }

    public void StopGraph()
    {
        if (samplingRoutine == null)
            return;

        if(prefabArray.Length > 0)
        {
            foreach (GameObject go in prefabArray)
                Destroy(go);
        }

        StopCoroutine(samplingRoutine);
        samplingRoutine = null;
    }
    #endregion

    IEnumerator SampleMotion()
    {
        float lastSampleTime = Time.time;

        while (true)
        {
            if (target)
            {
                float now = Time.time;
                float deltaTime = now - lastSampleTime;
                lastSampleTime = now;

                Quaternion currRot = target.transform.rotation;
                Quaternion visualRot = currRot * Quaternion.Euler(prefabInitRotation);

                if (deltaTime > 0f)
                {
                    rotationSamples[sampleIndex] = visualRot;
                    prefabArray[sampleIndex].transform.rotation = visualRot;
                    
                    sampleIndex = (sampleIndex + 1) % sampleAmount;
                }
            }

            yield return new WaitForSeconds(sampleInterval);
        }
    }
}
