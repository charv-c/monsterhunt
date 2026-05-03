using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySetTrap : MonoBehaviour
{
    [Header("陷阱设置")]
    public GameObject trapPrefab;             // 在 Inspector 拖入 trap 预制体
    public Vector3 spawnOffset = Vector3.zero; // 相对于敌人位置的偏移（默认 0）

    // 当前已放置的陷阱引用（用于防止重复放置 / 便于移除）
    private GameObject _currentTrap;

    /// <summary>
    /// 尝试在敌人位置放置陷阱。成功返回 true，失败返回 false（例如已放置或 prefab 未设置）。
    /// </summary>
    public bool TryPlaceTrap()
    {
        if (trapPrefab == null)
        {
            Debug.LogWarning($"{name}: trapPrefab 未设置，无法放置陷阱。");
            return false;
        }

        if (_currentTrap != null)
        {
            // 已经放置了一个陷阱，默认不重复放置
            return false;
        }

        Vector3 spawnPos = transform.position + spawnOffset;
        Quaternion spawnRot = transform.rotation;
        _currentTrap = Instantiate(trapPrefab, spawnPos, spawnRot);
        return true;
    }

    /// <summary>
    /// 强制放置（绕过已有陷阱检测），返回实例化的 GameObject 或 null。
    /// </summary>
    public GameObject PlaceTrapImmediate()
    {
        if (trapPrefab == null)
        {
            Debug.LogWarning($"{name}: trapPrefab 未设置，无法放置陷阱。");
            return null;
        }

        Vector3 spawnPos = transform.position + spawnOffset;
        Quaternion spawnRot = transform.rotation;
        // 若已有陷阱，先销毁（可根据需求改为允许多个）
        if (_currentTrap != null)
        {
            Destroy(_currentTrap);
            _currentTrap = null;
        }

        _currentTrap = Instantiate(trapPrefab, spawnPos, spawnRot);
        return _currentTrap;
    }

    /// <summary>
    /// 移除已放置的陷阱（若存在）。
    /// </summary>
    public void RemoveTrap()
    {
        if (_currentTrap != null)
        {
            Destroy(_currentTrap);
            _currentTrap = null;
        }
    }

    /// <summary>
    /// 当前是否已经放置了陷阱
    /// </summary>
    public bool HasPlacedTrap()
    {
        return _currentTrap != null;
    }
}
