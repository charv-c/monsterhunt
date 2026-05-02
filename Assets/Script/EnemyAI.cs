using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    private BehaviorTree.Node root;
    void Start()
    {
        root = new BehaviorTree.Selector(new List<BehaviorTree.Node>
        {
            new BehaviorTree.Selector(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ObservePlayer(),
                new BehaviorTree.MeleeAttackPlayer()
            }),
            new BehaviorTree.Sequence(new List<BehaviorTree.Node>
            {
                new BehaviorTree.ObservePlayer(),
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
