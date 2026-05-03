using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlyBlade : MonoBehaviour
{
    [Header("飞刀属性")]
    public float speed = 12f;             // 飞刀飞行速度（单位/s）
    public float maxLifetime = 5f;        // 最长存在时间（秒）
    public float maxDistance = 25f;       // 最大飞行距离（超过则销毁）
    public float damage = 20f;            // 命中玩家造成的伤害值

    private Vector3 _origin;
    private float _lifeTimer;

    void Start()
    {
        _origin = transform.position;
        _lifeTimer = 0f;

        // 可选：如果预制体没有 Rigidbody 且需要触发器回调，请确保 Collider 设置为 isTrigger
        // 如果有 Rigidbody，建议设置为 isKinematic = true。
    }

    void Update()
    {
        float move = speed * Time.deltaTime;
        transform.position += transform.forward * move;

        _lifeTimer += Time.deltaTime;
        if (_lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        if (Vector3.Distance(_origin, transform.position) >= maxDistance)
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 忽略触碰到同一个飞刀/特效之类
        if (other.gameObject == gameObject) return;

        // 优先寻找 HealthController（适配玩家或其他可被伤害的实体）
        var health = other.GetComponent<HealthController>();
        if (health == null)
        {
            // 也尝试在父物体上查找（如果玩家的 HealthController 在父物体）
            health = other.GetComponentInParent<HealthController>();
        }

        if (health != null)
        {
            // 调用伤害接口（HealthController 内部会处理无敌逻辑）
            health.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        // 如果碰到非玩家的实体（如地面、墙），也直接销毁（根据需求可改为反弹等）
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision collision)
    {
        // 作为兼容：如果使用非触发碰撞器时也处理
        var health = collision.collider.GetComponent<HealthController>() ?? collision.collider.GetComponentInParent<HealthController>();
        if (health != null)
        {
            health.TakeDamage(damage);
        }
        Destroy(gameObject);
    }
}
