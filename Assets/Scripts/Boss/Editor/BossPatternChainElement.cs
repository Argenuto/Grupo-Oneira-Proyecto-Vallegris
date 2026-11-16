using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BossPatternChainElement : VisualElement
{
    private readonly VisualElement stepsContainer;
    private readonly List<VisualElement> stepCards = new List<VisualElement>();

    public BossPatternChainElement(BossPattern pattern)
    {
        AddToClassList("pattern-chain-container");

        Label title = new Label("Esta fase usa un patrón fijo");
        title.AddToClassList("pattern-chain-title");
        Add(title);

        if (pattern != null)
        {
            bool isLoop = pattern.loopMode == BossPattern.LoopMode.Loop;
            Label patternName = new Label($"Patrón: {pattern.name} {(isLoop ? "(En bucle)" : "(Una sola vez)")}");
            patternName.style.fontSize = 11;
            patternName.style.color = new Color(0.7f, 0.7f, 0.7f);
            patternName.style.marginBottom = 10;
            Add(patternName);
        }

        stepsContainer = new VisualElement();
        stepsContainer.style.alignItems = Align.Center;
        Add(stepsContainer);

        BuildChain(pattern);
    }

    private void BuildChain(BossPattern pattern)
    {
        stepsContainer.Clear();
        stepCards.Clear();

        if (pattern == null || pattern.actions == null || pattern.actions.Count == 0)
        {
            Label empty = new Label("(Patrón sin acciones configuradas)");
            stepsContainer.Add(empty);
            return;
        }

        for (int i = 0; i < pattern.actions.Count; i++)
        {
            BossAction act = pattern.actions[i];
            VisualElement card = new VisualElement();
            card.AddToClassList("pattern-step-card");

            string actName = act != null ? act.GetType().Name : "Nula";
            Label nameLbl = new Label($"Paso {i + 1}: {actName}");
            nameLbl.style.fontSize = 12;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            card.Add(nameLbl);

            if (act != null)
            {
                Label details = new Label($"Telegraph: {act.telegraphDuration:0.##}s | Recovery: {act.recoveryDuration:0.##}s");
                details.style.fontSize = 10;
                details.style.color = new Color(0.6f, 0.6f, 0.6f);
                card.Add(details);
            }

            stepsContainer.Add(card);
            stepCards.Add(card);

            if (i < pattern.actions.Count - 1)
            {
                Label arrow = new Label("▼");
                arrow.style.fontSize = 12;
                arrow.style.color = new Color(0.5f, 0.5f, 0.5f);
                arrow.style.marginBottom = 4;
                stepsContainer.Add(arrow);
            }
            else if (pattern.loopMode == BossPattern.LoopMode.Loop)
            {
                Label loopLbl = new Label("↺ Retorna al paso 1");
                loopLbl.style.fontSize = 11;
                loopLbl.style.color = new Color(0.3f, 0.8f, 0.5f);
                loopLbl.style.marginTop = 4;
                stepsContainer.Add(loopLbl);
            }
        }
    }

    public void HighlightStep(int actionIndex)
    {
        for (int i = 0; i < stepCards.Count; i++)
        {
            if (i == actionIndex)
                stepCards[i].AddToClassList("pattern-step-active");
            else
                stepCards[i].RemoveFromClassList("pattern-step-active");
        }
    }
}
