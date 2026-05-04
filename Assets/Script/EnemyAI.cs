using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform playerTransform;
    private BehaviorTree.Node root;
    NavMeshAgent agent;

    private EnemyRangedAttack rangedAttackComponent;
    private EnemySetTrap trapComponent;

    // 行为树节点引用，用于重置状态
    private BehaviorTree.ObservePlayer observeNode;
    private BehaviorTree.RangedAttackPlayer rangedAttackNode;
    private BehaviorTree.SetTrap setTrapNode;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        rangedAttackComponent = GetComponent<EnemyRangedAttack>();
        trapComponent = GetComponent<EnemySetTrap>();

        // 创建可复用的节点实例
        observeNode = new BehaviorTree.ObservePlayer(transform, playerTransform, agent);
        rangedAttackNode = new BehaviorTree.RangedAttackPlayer(rangedAttackComponent, agent);
        setTrapNode = new BehaviorTree.SetTrap(trapComponent, agent);

        // ===== 行为树结构 =====
        // Selector 从左到右执行，第一个 SUCCESS 即停止
        // 优先级：放陷阱(3~5m) > 远程攻击(观察后) > 靠近玩家(>10m)
        root = new BehaviorTree.Selector(new List<BehaviorTree.Node>
        {
            // 分支1：距离 3~5 米时放陷阱（不移动）
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.CheckDistance(transform, playerTransform, 3f, 5f),
                setTrapNode
            }),

            // 分支2：远程攻击（先观察3秒，然后发射飞刀，不移动）
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                observeNode,
                rangedAttackNode
            }),

            // 分支3：距离 >10 米时靠近玩家
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.CheckDistanceGreater(transform, playerTransform, 10f),
                new BehaviorTree.ApproachPlayer(transform, playerTransform, agent)
            })
        });
    }

    void Update()
    {
        BehaviorTree.NodeState state = root.Evaluate();

        // 当行为树完成一个完整周期（SUCCESS）后，重置各节点状态以便下次执行
        if (state == BehaviorTree.NodeState.SUCCESS)
        {
            ResetBehaviorTree();
        }
    }

    /// <summary>
    /// 重置行为树各节点的执行状态，允许下次重新执行
    /// </summary>
    private void ResetBehaviorTree()
    {
        observeNode.Reset();
        rangedAttackNode.ResetFired();
        setTrapNode.ResetPlaced();
    }
}