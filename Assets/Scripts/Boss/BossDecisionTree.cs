using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public enum BossDistanceBand
{
    Near,
    Medium,
    Far
}

public enum BossDecisionCondition
{
    DistanceBand,
    PlayerAirborne,
    HealthBelow,
    ActionReady,
    PlayerIdleFor
}

[CreateAssetMenu(menuName = "Game/Boss/Decision Tree", fileName = "BossDecisionTree_")]
public class BossDecisionTree : ScriptableObject
{
    [SerializeReference, Required]
    public BossDecisionNode root = new BossDecisionLeaf();

    public BossDecisionLeaf Evaluate(BossController controller, BossContext context)
    {
        return root != null ? root.Evaluate(controller, context, 0) : null;
    }
}

[Serializable]
public abstract class BossDecisionNode
{
    public abstract BossDecisionLeaf Evaluate(BossController controller, BossContext context, int depth);
}

[Serializable]
public class BossDecisionBranch : BossDecisionNode
{
    public BossDecisionCondition condition;
    [ShowIf("@condition == BossDecisionCondition.DistanceBand")]
    public BossDistanceBand distanceBand = BossDistanceBand.Near;
    [ShowIf("@condition == BossDecisionCondition.HealthBelow"), Range(0f, 100f)]
    public float healthThreshold = 66f;
    [ShowIf("@condition == BossDecisionCondition.ActionReady")]
    [SerializeReference] public BossAction action;
    [ShowIf("@condition == BossDecisionCondition.PlayerIdleFor"), Min(0f)]
    public float idleSeconds = 1f;

    [SerializeReference, LabelText("Sí")]
    public BossDecisionNode yes;
    [SerializeReference, LabelText("No")]
    public BossDecisionNode no;

    public override BossDecisionLeaf Evaluate(BossController controller, BossContext context, int depth)
    {
        if (depth >= 3)
            return null;

        bool result = false;
        switch (condition)
        {
            case BossDecisionCondition.DistanceBand:
                if (context.HasLivingPlayer)
                {
                    float distance = Mathf.Abs(context.player.position.x - context.Position.x);
                    result = distanceBand == BossDistanceBand.Near
                        ? distance < controller.NearDistance
                        : distanceBand == BossDistanceBand.Medium
                            ? distance >= controller.NearDistance && distance <= controller.FarDistance
                            : distance > controller.FarDistance;
                }
                break;
            case BossDecisionCondition.PlayerAirborne:
                result = context.playerController != null && !context.playerController.IsGroundedForBoss;
                break;
            case BossDecisionCondition.HealthBelow:
                result = controller.HealthPercent <= healthThreshold;
                break;
            case BossDecisionCondition.ActionReady:
                result = action != null && controller.IsActionReady(action);
                break;
            case BossDecisionCondition.PlayerIdleFor:
                result = context.HasLivingPlayer && controller.PlayerIdleDuration >= idleSeconds;
                break;
        }

        BossDecisionNode next = result ? yes : no;
        if (next == null)
            return null;

        return next.Evaluate(controller, context, depth + 1);
    }
}

[Serializable]
public class BossDecisionLeaf : BossDecisionNode
{
    [ListDrawerSettings(ShowIndexLabels = true, DraggableItems = true)]
    public List<BossWeightedEntry> entries = new List<BossWeightedEntry>();
    [SerializeReference, LabelText("Acción de respaldo")]
    public BossAction fallbackAction = new WaitAction { duration = 0.5f };
    [LabelText("Patrón de respaldo")]
    public BossPattern fallbackPattern;

    public override BossDecisionLeaf Evaluate(BossController controller, BossContext context, int depth) => this;
}

[Serializable]
public class BossWeightedEntry
{
    [SerializeReference] public BossAction action;
    [SerializeReference, ListDrawerSettings(ShowIndexLabels = true)]
    public List<BossAction> combo = new List<BossAction>();
    public BossPattern pattern;
    [Min(0f)] public float weight = 1f;
    [Min(0f)] public float cooldown;
    [Min(1)] public int maxConsecutives = 2;
}
