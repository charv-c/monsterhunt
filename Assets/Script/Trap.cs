using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trap : MonoBehaviour
{
    [Header("陷阱设置")]
    public float damage = 25f;          // 造成的伤害
    public float stunDuration = 2f;     // 使玩家无法移动的持续时间（秒）
    public bool requirePlayerTag = true; // 只对 Tag 为 "Player" 的对象生效（推荐打开）

    [Header("生命周期")]
    public float lifeTime = 15f;        // 陷阱自动销毁时间（秒），<=0 则不自动销毁

    // 当使用触发器（isTrigger = true）时调用
    void Start()
    {
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        HandleTrigger(other.gameObject);
    }

    // 作为兼容：当使用普通碰撞器时调用
    void OnCollisionEnter(Collision collision)
    {
        HandleTrigger(collision.gameObject);
    }

    private void HandleTrigger(GameObject otherObj)
    {
        if (otherObj == null) return;

        // 可选：仅对 Player tag 生效
        if (requirePlayerTag && !otherObj.CompareTag("Player"))
        {
            // 如果传入的是子物体（例如脚上的 collider），也尝试父物体
            var parent = otherObj.transform.parent;
            if (parent == null || !parent.CompareTag("Player"))
            {
                // 非玩家，销毁陷阱或忽略（这里选择销毁以避免残留）
                Destroy(gameObject);
                return;
            }
            otherObj = parent.gameObject;
        }

        // 尝试找到玩家的 HealthController 与 Player 组件
        HealthController health = otherObj.GetComponent<HealthController>() ?? otherObj.GetComponentInParent<HealthController>();
        Player playerComp = otherObj.GetComponent<Player>() ?? otherObj.GetComponentInParent<Player>();

        // 如果没有找到 HealthController，仍然销毁陷阱并返回
        if (health == null)
        {
            Destroy(gameObject);
            return;
        }

        // 如果玩家处于无敌状态，不受影响，但陷阱依旧销毁
        if (health.IsInvincible)
        {
            Destroy(gameObject);
            return;
        }

        // 造成伤害
        health.TakeDamage(damage);

        // 使玩家无法移动：禁用 Player 组件一段时间（如果存在）
        if (playerComp != null)
        {
            // 防止重复禁用同一玩家：如果已经被禁用则不再启动新的协程
            if (playerComp.enabled)
            {
                playerComp.enabled = false;
                // 在玩家对象上启动协程以确保在陷阱销毁后仍能恢复
                playerComp.StartCoroutine(EnablePlayerAfter(playerComp, stunDuration));
            }
        }

        // 触发后销毁陷阱
        Destroy(gameObject);
    }

    // 在玩家对象上运行的协程：等待后恢复 Player 组件
    private IEnumerator EnablePlayerAfter(Player playerComp, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerComp != null)
        {
            // 仅当对象仍存在时恢复
            playerComp.enabled = true;
        }
    }
}
