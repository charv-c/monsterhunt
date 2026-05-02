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
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ObservePlayer(transform, playerTransform, agent),
            }),
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ObservePlayer(transform, playerTransform, agent),
                new BehaviorTree.RangedAttackPlayer()
            }),
            new BehaviorTree.SetTrap()
        });
        
    }

    void Update()
    {
        root.Evaluate();
    }
}
