using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRangedAttack : MonoBehaviour
{
    [Header("远程攻击 - 飞刀设置")]
    public GameObject flybladePrefab;
    public float spawnOffset = 1.2f;

    // 防止重复发射（外部可以调用 ResetFire 重置）
    private bool hasFired = false;

    /// <summary>
    /// 尝试发射一次飞刀。成功发射返回 true（并标记已发射），否则返回 false。
    /// </summary>
    public bool TryFire()
    {
        if (hasFired) return false;
        if (flybladePrefab == null)
        {
            Debug.LogWarning($"{name}: flybladePrefab 未设置，无法发射。");
            return false;
        }

        Vector3 spawnPos = transform.position + transform.forward * spawnOffset;
        Quaternion spawnRot = transform.rotation;
        Instantiate(flybladePrefab, spawnPos, spawnRot);

        hasFired = true;
        return true;
    }

    /// <summary>
    /// 强制发射（忽略 hasFired 状态），并标记为已发射。
    /// </summary>
    public bool FireImmediate()
    {
        if (flybladePrefab == null)
        {
            Debug.LogWarning($"{name}: flybladePrefab 未设置，无法发射。");
            return false;
        }

        Vector3 spawnPos = transform.position + transform.forward * spawnOffset;
        Quaternion spawnRot = transform.rotation;
        Instantiate(flybladePrefab, spawnPos, spawnRot);

        hasFired = true;
        return true;
    }

    /// <summary>
    /// 允许在未来再次发射。
    /// </summary>
    public void ResetFire()
    {
        hasFired = false;
    }
}
