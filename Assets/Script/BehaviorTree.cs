using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BehaviorTree : MonoBehaviour
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    public abstract class Node
    {
        public abstract NodeState Evaluate();
    }

    public class Selector : Node
    {
        private List<Node> nodes = new List<Node>();
        public Selector(List<Node> nodes)
        {
            this.nodes = nodes;
        }
        public override NodeState Evaluate()
        {
            foreach (Node node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.SUCCESS:
                        return NodeState.SUCCESS;
                    case NodeState.RUNNING:
                        return NodeState.RUNNING;
                    case NodeState.FAILURE:
                        continue;
                    default:
                        continue;
                }
            }
            return NodeState.FAILURE;
        }
    }

    public class Sequence : Node
    {
        private List<Node> nodes = new List<Node>();
        public Sequence(List<Node> nodes)
        {
            this.nodes = nodes;
        }
        public override NodeState Evaluate()
        {
            bool anyNodeRunning = false;
            foreach (Node node in nodes)
            {
                switch (node.Evaluate())
                {
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        anyNodeRunning = true;
                        continue;
                    case NodeState.FAILURE:
                        return NodeState.FAILURE;
                    default:
                        return NodeState.SUCCESS;
                }
            }
            return anyNodeRunning ? NodeState.RUNNING : NodeState.SUCCESS;
        }
    }

    // ========== 条件节点 ==========

    /// <summary>
    /// 检查与玩家的距离是否在指定范围内
    /// </summary>
    public class CheckDistance : Node
    {
        private Transform enemyTransform;
        private Transform playerTransform;
        private float minDistance;
        private float maxDistance;

        public CheckDistance(Transform enemyTransform, Transform playerTransform, float minDistance, float maxDistance)
        {
            this.enemyTransform = enemyTransform;
            this.playerTransform = playerTransform;
            this.minDistance = minDistance;
            this.maxDistance = maxDistance;
        }

        public override NodeState Evaluate()
        {
            if (playerTransform == null) return NodeState.FAILURE;

            float distance = Vector3.Distance(enemyTransform.position, playerTransform.position);
            if (distance >= minDistance && distance <= maxDistance)
            {
                return NodeState.SUCCESS;
            }
            return NodeState.FAILURE;
        }
    }

    /// <summary>
    /// 检查与玩家的距离是否大于指定值
    /// </summary>
    public class CheckDistanceGreater : Node
    {
        private Transform enemyTransform;
        private Transform playerTransform;
        private float threshold;

        public CheckDistanceGreater(Transform enemyTransform, Transform playerTransform, float threshold)
        {
            this.enemyTransform = enemyTransform;
            this.playerTransform = playerTransform;
            this.threshold = threshold;
        }

        public override NodeState Evaluate()
        {
            if (playerTransform == null) return NodeState.FAILURE;

            float distance = Vector3.Distance(enemyTransform.position, playerTransform.position);
            if (distance > threshold)
            {
                return NodeState.SUCCESS;
            }
            return NodeState.FAILURE;
        }
    }

    // ========== 行为节点 ==========

    /// <summary>
    /// 靠近玩家：直接朝玩家移动
    /// </summary>
    public class ApproachPlayer : Node
    {
        private Transform enemyTransform;
        private Transform playerTransform;
        private NavMeshAgent agent;
        private float approachSpeed = 5f;
        private float stopDistance = 3f;

        public ApproachPlayer(Transform enemyTransform, Transform playerTransform, NavMeshAgent agent)
        {
            this.enemyTransform = enemyTransform;
            this.playerTransform = playerTransform;
            this.agent = agent;
        }

        public override NodeState Evaluate()
        {
            if (playerTransform == null) return NodeState.FAILURE;

            float distance = Vector3.Distance(enemyTransform.position, playerTransform.position);
            if (distance <= stopDistance)
            {
                agent.isStopped = true;
                agent.ResetPath();
                return NodeState.SUCCESS;
            }

            agent.isStopped = false;
            agent.speed = approachSpeed;
            agent.SetDestination(playerTransform.position);

            // 面朝玩家
            Vector3 lookDirection = (playerTransform.position - enemyTransform.position).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(lookDirection);
                enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, rot, Time.deltaTime * 5f);
            }

            return NodeState.RUNNING;
        }
    }

    /// <summary>
    /// 观察玩家：绕圈运动，保持距离
    /// </summary>
    public class ObservePlayer : Node
    {
        private Transform enemyTransform;
        private Transform playerTransform;
        private NavMeshAgent agent;

        private float targetDistance = 7f;
        private float chaseSpeed = 2f;
        private float orbitSpeed = 2f;
        private float deadZone = 0.5f;

        private bool isStart = false;
        private float orbitDirection = 1f;
        private float GeneOrbitDirection;

        private float ObserveTime = 3f;
        private float timer = 0f;

        public ObservePlayer(Transform enemyTransform, Transform playerTransform, NavMeshAgent agent)
        {
            this.enemyTransform = enemyTransform;
            this.playerTransform = playerTransform;
            this.agent = agent;
        }

        public override NodeState Evaluate()
        {
            if (playerTransform == null)
            {
                return NodeState.FAILURE;
            }

            if (!isStart)
            {
                GeneOrbitDirection = Random.value > 0.5f ? -1f : 1f;
                isStart = true;
            }

            timer += Time.deltaTime;

            Vector3 enemyPos = enemyTransform.position;
            Vector3 playerPos = playerTransform.position;

            Vector3 directionToPlayer = (playerPos - enemyPos).normalized;
            float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);
            float error = distanceToPlayer - targetDistance;

            Vector3 radial = Vector3.zero;
            if (Mathf.Abs(error) > deadZone)
            {
                radial = directionToPlayer * error * chaseSpeed;
            }

            orbitDirection = Mathf.Lerp(orbitDirection, GeneOrbitDirection, Time.deltaTime * 2f);
            Vector3 tangent = Vector3.Cross(Vector3.up, directionToPlayer).normalized * orbitDirection;
            Vector3 orbit = tangent * orbitSpeed;

            Vector3 moveDirection = radial + orbit;
            Vector3 targetPos = enemyPos + moveDirection;

            agent.isStopped = false;
            agent.SetDestination(targetPos);

            Vector3 lookDirection = (playerPos - enemyPos).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(lookDirection);
                enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, rot, Time.deltaTime * 5f);
            }

            if (timer >= ObserveTime)
            {
                timer = 0f;
                isStart = false;
                return NodeState.SUCCESS;
            }

            return NodeState.RUNNING;
        }

        public void Reset()
        {
            timer = 0f;
            isStart = false;
        }
    }

    public class MeleeAttackPlayer : Node
    {
        public override NodeState Evaluate()
        {
            Debug.Log("近战攻击玩家");
            return NodeState.SUCCESS;
        }
    }

    /// <summary>
    /// 远程攻击玩家：停止移动，发射飞刀
    /// </summary>
    public class RangedAttackPlayer : Node
    {
        private EnemyRangedAttack rangedAttackComponent;
        private NavMeshAgent agent;
        private bool hasFired = false;

        public RangedAttackPlayer(EnemyRangedAttack rangedAttackComponent, NavMeshAgent agent)
        {
            this.rangedAttackComponent = rangedAttackComponent;
            this.agent = agent;
        }

        public override NodeState Evaluate()
        {
            if (rangedAttackComponent == null)
            {
                Debug.LogWarning("RangedAttackPlayer: rangedAttackComponent is null");
                return NodeState.FAILURE;
            }

            // 停止移动
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (!hasFired)
            {
                bool fired = rangedAttackComponent.TryFire();
                if (fired)
                {
                    hasFired = true;
                    return NodeState.SUCCESS;
                }
                else
                {
                    return NodeState.FAILURE;
                }
            }

            return NodeState.SUCCESS;
        }

        public void ResetFired()
        {
            hasFired = false;
            if (rangedAttackComponent != null)
                rangedAttackComponent.ResetFire();
        }
    }

    /// <summary>
    /// 放置陷阱：停止移动，放置陷阱
    /// </summary>
    public class SetTrap : Node
    {
        private EnemySetTrap trapComponent;
        private NavMeshAgent agent;
        private bool hasPlaced = false;

        public SetTrap(EnemySetTrap trapComponent, NavMeshAgent agent)
        {
            this.trapComponent = trapComponent;
            this.agent = agent;
        }

        public override NodeState Evaluate()
        {
            if (trapComponent == null)
            {
                Debug.LogWarning("SetTrap: trapComponent is null");
                return NodeState.FAILURE;
            }

            // 停止移动
            if (agent != null)
            {
                agent.isStopped = true;
                agent.ResetPath();
            }

            if (!hasPlaced)
            {
                bool placed = trapComponent.TryPlaceTrap();
                if (placed)
                {
                    hasPlaced = true;
                    Debug.Log("放置陷阱成功");
                    return NodeState.SUCCESS;
                }
                else
                {
                    // 可能已存在陷阱或prefab未设置
                    return NodeState.FAILURE;
                }
            }

            return NodeState.SUCCESS;
        }

        public void ResetPlaced()
        {
            hasPlaced = false;
        }
    }

    void Start()
    {
    }

    void Update()
    {
    }
}