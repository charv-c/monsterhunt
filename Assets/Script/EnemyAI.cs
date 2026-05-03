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
    void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
        agent = GetComponent<NavMeshAgent>();
        root = new BehaviorTree.Selector(new List<BehaviorTree.Node>
        {
            new BehaviorTree.Selector(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ChasePlayer(transform, playerTransform, agent, 7f),
                new BehaviorTree.RetreatPlayer(transform, playerTransform, agent, 3f)
            }),
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.RangedAttackPlayer()
            })
        });
        
    }

    void Update()
    {
        root.Evaluate();
    }
}
