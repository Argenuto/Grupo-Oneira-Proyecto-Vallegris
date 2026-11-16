using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BossBranchNodeElement : VisualElement
{
    public int NodeIndex { get; private set; }
    public BossDecisionBranch Branch { get; private set; }

    private readonly Label titleLabel;
    private readonly Label subtitleLabel;

    public BossBranchNodeElement(int index, BossDecisionBranch branch)
    {
        NodeIndex = index;
        Branch = branch;

        AddToClassList("boss-node");
        AddToClassList("boss-branch");
        style.position = Position.Absolute;

        titleLabel = new Label();
        titleLabel.AddToClassList("node-title");
        Add(titleLabel);

        subtitleLabel = new Label();
        subtitleLabel.AddToClassList("node-subtitle");
        Add(subtitleLabel);

        VisualElement portsContainer = new VisualElement();
        portsContainer.AddToClassList("branch-ports");

        Label yesPort = new Label("◀ Sí");
        yesPort.AddToClassList("branch-port");
        yesPort.AddToClassList("port-yes");

        Label noPort = new Label("No ▶");
        noPort.AddToClassList("branch-port");
        noPort.AddToClassList("port-no");

        portsContainer.Add(yesPort);
        portsContainer.Add(noPort);
        Add(portsContainer);

        UpdateContent();
    }

    private void UpdateContent()
    {
        if (Branch == null) return;

        switch (Branch.condition)
        {
            case BossDecisionCondition.DistanceBand:
                titleLabel.text = $"Distancia = {Branch.distanceBand}";
                subtitleLabel.text = "Condición de proximidad";
                break;
            case BossDecisionCondition.PlayerAirborne:
                titleLabel.text = "¿Jugador en el aire?";
                subtitleLabel.text = "!IsGroundedForBoss";
                break;
            case BossDecisionCondition.HealthBelow:
                titleLabel.text = $"Vida del jefe ≤ {Branch.healthThreshold}%";
                subtitleLabel.text = $"Umbral HP: {Branch.healthThreshold}%";
                break;
            case BossDecisionCondition.ActionReady:
                string actionName = Branch.action != null ? Branch.action.GetType().Name : "Ninguna";
                titleLabel.text = $"Acción lista: {actionName}";
                subtitleLabel.text = "IsActionReady()";
                break;
            case BossDecisionCondition.PlayerIdleFor:
                titleLabel.text = $"Jugador quieto ≥ {Branch.idleSeconds}s";
                subtitleLabel.text = $"Tiempo inmóvil";
                break;
            default:
                titleLabel.text = Branch.condition.ToString();
                subtitleLabel.text = "";
                break;
        }
    }

    public void SetActivePath(bool active)
    {
        if (active)
            AddToClassList("path-active");
        else
            RemoveFromClassList("path-active");
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            AddToClassList("selected");
        else
            RemoveFromClassList("selected");
    }
}
