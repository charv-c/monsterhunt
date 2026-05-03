using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AI;

public class BehaviorTree : MonoBehaviour
{
    public enum NodeState//节点状态
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    public abstract class Node//抽象节点类
    {
        public abstract NodeState Evaluate();
    }

    public class Selector : Node//选择节点
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

    public class Sequence : Node//序列节点
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

    public class ObservePlayer : Node//观察玩家节点
    {
        private Transform enemyTransform;
        private Transform playerTransform;
        private NavMeshAgent agent;

        private float targetDistance = 7f;//敌人观察玩家的距离
        private float chaseSpeed = 2f;//敌人追逐玩家的速度
        private float orbitSpeed = 2f;//敌人绕圈速度
        private float deadZone = 0.5f;       // 防抖动死区范围

        private bool isStart = false;              // 是否开始观察
        private float orbitDirection = 1f;
        private float GeneOrbitDirection;        // 顺/逆时针

        private float ObserveTime = 3f;          // 观察时间
        private float timer = 0f;                // 观察计时器

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
                return NodeState.FAILURE; // 玩家不存在，返回失败
            }

            if (!isStart)
            {
                GeneOrbitDirection = Random.value > 0.5f ? -1f : 1f;
                Debug.Log(GeneOrbitDirection);
                isStart = true;//随机选择顺时针或逆时针绕圈
            }

            timer += Time.deltaTime;//计时

            Vector3 enemyPos = enemyTransform.position;
            Vector3 playerPos = playerTransform.position;

            Vector3 directionToPlayer = (playerPos - enemyPos).normalized;
            float distanceToPlayer = Vector3.Distance(enemyPos, playerPos);
            float error = distanceToPlayer - targetDistance;

            Vector3 radial = Vector3.zero;

            if (Mathf.Abs(error) > deadZone)
            {
                radial = directionToPlayer * error * chaseSpeed;
            }//防止抖动

            orbitDirection = Mathf.Lerp(orbitDirection, GeneOrbitDirection, Time.deltaTime * 2f);
            Vector3 tangent = Vector3.Cross(Vector3.up, directionToPlayer).normalized * orbitDirection;
            Vector3 orbit = tangent * orbitSpeed;//实现绕圈

            Vector3 moveDirection = radial + orbit;
            Vector3 targetPos = enemyPos + moveDirection;
            agent.SetDestination(targetPos);

            Vector3 lookDirection = (playerPos - enemyPos).normalized;
            if (lookDirection != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(lookDirection);
                enemyTransform.rotation = Quaternion.Slerp(enemyTransform.rotation, rot, Time.deltaTime * 5f);
            }
            if (timer >= ObserveTime)
            {
                timer = 0f; // 重置计时器
                isStart = false; // 重置观察状态
                return NodeState.SUCCESS; // 观察完成，返回成功
            }

            return NodeState.RUNNING;
        }
    }
    public class MeleeAttackPlayer : Node//近战攻击玩家节点
    {
        public override NodeState Evaluate()
        {
            Debug.Log("近战攻击玩家");
            return NodeState.SUCCESS;// 返回执行成功
        }
    }

    public class RangedAttackPlayer : Node//远程攻击玩家节点
    {
        public override NodeState Evaluate()
        {
            Debug.Log("远程攻击玩家");
            return NodeState.SUCCESS;// 返回执行成功
        }
    }

    public class SetTrap : Node//设置陷阱节点
    {
        public override NodeState Evaluate()
        {
            Debug.Log("设置陷阱");
            return NodeState.SUCCESS;// 返回执行成功
        }
    }

        // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
