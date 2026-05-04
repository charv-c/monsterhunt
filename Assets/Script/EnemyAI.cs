using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform playerTransform;
    private BehaviorTree.Node root;
    NavMeshAgent agent;

    // 缺省情况下，Enemy 游戏对象上应挂载 EnemyRangedAttack 组件并在其 Inspector 中设置飞刀预制体
    private EnemyRangedAttack rangedAttackComponent;

    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        rangedAttackComponent = GetComponent<EnemyRangedAttack>();

        root = new BehaviorTree.Selector(new List<BehaviorTree.Node>
        {
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ObservePlayer(transform, playerTransform, agent),
            }),
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ObservePlayer(transform, playerTransform, agent),
                // 传入 EnemyRangedAttack 组件（组件需挂在同一个 GameObject 上，并在 Inspector 中设置预制体）
                new BehaviorTree.RangedAttackPlayer(rangedAttackComponent)
            }),
            new BehaviorTree.SetTrap()
        });
        
    }

    void Update()
    {
        root.Evaluate();
    }
}
