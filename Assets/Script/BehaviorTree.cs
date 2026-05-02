using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        public override NodeState Evaluate()
        {
            Debug.Log("观察玩家");
            return NodeState.SUCCESS; // 返回执行成功
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
