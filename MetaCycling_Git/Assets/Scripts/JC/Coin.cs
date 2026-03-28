using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : MonoBehaviour
{
    public float lifeSpan = 1.0f; // 金幣存在時間
    private float timer = 0f;
    public MotionManager manager;
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifeSpan)
        {
            Destroy(gameObject);
        }
    }

    // 碰到手把時觸發
    void OnTriggerEnter(Collider healthcare)
    {
        if (healthcare.CompareTag("GameController"))
        {
            Collect();
        }
    }

    public void Collect()
    {
        manager.OnCoinCollected(transform.position);
        Destroy(gameObject);
    }
}
