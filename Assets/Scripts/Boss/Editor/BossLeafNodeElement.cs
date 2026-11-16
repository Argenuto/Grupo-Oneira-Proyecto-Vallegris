using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BossLeafNodeElement : VisualElement
{
    public int NodeIndex { get; private set; }
    public BossDecisionLeaf Leaf { get; private set; }

    private readonly Label titleLabel;
    private readonly VisualElement entriesContainer;
    private readonly List<EntryRowView> rowViews = new List<EntryRowView>();

    private class EntryRowView
    {
        public VisualElement Root;
        public VisualElement FillBar;
        public Label NameLabel;
        public Label WeightLabel;
        public Label CooldownLabel;
        public Label StatusLabel;
        public BossWeightedEntry Entry;
    }

    public BossLeafNodeElement(int index, BossDecisionLeaf leaf)
    {
        NodeIndex = index;
        Leaf = leaf;

        AddToClassList("boss-node");
        AddToClassList("boss-leaf");
        style.position = Position.Absolute;

        VisualElement header = new VisualElement();
        header.AddToClassList("leaf-header");

        titleLabel = new Label($"HOJA (Nodo #{index})");
        titleLabel.AddToClassList("leaf-title");
        header.Add(titleLabel);

        if (Leaf != null && (Leaf.fallbackAction != null || Leaf.fallbackPattern != null))
        {
            string fbName = Leaf.fallbackAction != null ? Leaf.fallbackAction.GetType().Name : Leaf.fallbackPattern.name;
            Label fbBadge = new Label($"Resp: {fbName}");
            fbBadge.AddToClassList("entry-badge");
            header.Add(fbBadge);
        }

        Add(header);

        entriesContainer = new VisualElement();
        Add(entriesContainer);

        BuildRows();
    }

    private void BuildRows()
    {
        entriesContainer.Clear();
        rowViews.Clear();

        if (Leaf == null || Leaf.entries == null)
            return;

        float totalWeight = 0f;
        for (int i = 0; i < Leaf.entries.Count; i++)
        {
            if (Leaf.entries[i] != null && Leaf.entries[i].weight > 0f)
                totalWeight += Leaf.entries[i].weight;
        }

        for (int i = 0; i < Leaf.entries.Count; i++)
        {
            BossWeightedEntry entry = Leaf.entries[i];
            if (entry == null) continue;

            EntryRowView row = new EntryRowView();
            row.Entry = entry;

            row.Root = new VisualElement();
            row.Root.AddToClassList("entry-row");

            row.FillBar = new VisualElement();
            row.FillBar.AddToClassList("entry-bar");
            float pct = totalWeight > 0f ? (entry.weight / totalWeight) * 100f : 0f;
            row.FillBar.style.width = Length.Percent(pct);
            row.Root.Add(row.FillBar);

            string actionTitle = GetEntryTitle(entry);
            row.NameLabel = new Label(actionTitle);
            row.NameLabel.AddToClassList("entry-name");
            row.Root.Add(row.NameLabel);

            row.WeightLabel = new Label($"{pct:0}% ({entry.weight:0.##})");
            row.WeightLabel.AddToClassList("entry-badge");
            row.Root.Add(row.WeightLabel);

            if (entry.cooldown > 0f)
            {
                row.CooldownLabel = new Label($"cd {entry.cooldown:0.#}s");
                row.CooldownLabel.AddToClassList("entry-badge");
                row.Root.Add(row.CooldownLabel);
            }

            if (entry.maxConsecutives > 1)
            {
                Label maxLabel = new Label($"máx×{entry.maxConsecutives}");
                maxLabel.AddToClassList("entry-badge");
                row.Root.Add(maxLabel);
            }

            row.StatusLabel = new Label("");
            row.StatusLabel.AddToClassList("entry-badge");
            row.StatusLabel.style.display = DisplayStyle.None;
            row.Root.Add(row.StatusLabel);

            entriesContainer.Add(row.Root);
            rowViews.Add(row);
        }
    }

    private string GetEntryTitle(BossWeightedEntry entry)
    {
        if (entry.action != null)
            return entry.action.GetType().Name;
        if (entry.combo != null && entry.combo.Count > 0 && entry.combo[0] != null)
            return "Combo: " + entry.combo[0].GetType().Name;
        if (entry.pattern != null)
            return "Patrón: " + entry.pattern.name;
        return "Vacío";
    }

    public void ResetHighlights()
    {
        RemoveFromClassList("leaf-active");
        for (int i = 0; i < rowViews.Count; i++)
        {
            var row = rowViews[i];
            row.Root.RemoveFromClassList("chosen");
            row.Root.RemoveFromClassList("excluded");
            row.Root.RemoveFromClassList("cooldown");
            row.Root.RemoveFromClassList("dead-entry");
            row.StatusLabel.style.display = DisplayStyle.None;
        }
    }

    public void HighlightDecision(BossDecisionRecord record, BossController controller, int phaseIndex)
    {
        AddToClassList("leaf-active");

        for (int i = 0; i < rowViews.Count; i++)
        {
            var row = rowViews[i];
            row.Root.RemoveFromClassList("chosen");
            row.Root.RemoveFromClassList("excluded");
            row.Root.RemoveFromClassList("cooldown");

            bool isChosen = (record.ChosenEntryIndex == i);
            if (isChosen)
            {
                row.Root.AddToClassList("chosen");
                row.StatusLabel.text = "✓ Elegida";
                row.StatusLabel.style.display = DisplayStyle.Flex;
            }

            if (record.Candidates != null)
            {
                for (int c = 0; c < record.Candidates.Count; c++)
                {
                    var cand = record.Candidates[c];
                    if (cand.EntryIndex == i)
                    {
                        if (cand.Excluded)
                        {
                            row.Root.AddToClassList("excluded");
                            row.StatusLabel.text = $"✕ {cand.Reason}";
                            row.StatusLabel.style.display = DisplayStyle.Flex;
                        }
                        else if (!isChosen)
                        {
                            row.WeightLabel.text = $"{cand.FinalWeight:0.##}";
                        }
                        break;
                    }
                }
            }
        }
    }

    public void UpdateLiveCooldowns(BossController controller, int phaseIndex)
    {
        if (controller == null) return;

        for (int i = 0; i < rowViews.Count; i++)
        {
            float rem = controller.GetEntryCooldownRemaining(phaseIndex, NodeIndex, i);
            var row = rowViews[i];
            if (rem > 0.05f)
            {
                row.Root.AddToClassList("cooldown");
                if (row.CooldownLabel != null)
                    row.CooldownLabel.text = $"cd {rem:0.0}s";
            }
            else
            {
                row.Root.RemoveFromClassList("cooldown");
                if (row.CooldownLabel != null && row.Entry != null)
                    row.CooldownLabel.text = $"cd {row.Entry.cooldown:0.#}s";
            }
        }
    }

    public void ApplyStats(int chosenTimes, Dictionary<int, int> entryChosenCounts)
    {
        titleLabel.text = $"HOJA #{NodeIndex} (×{chosenTimes})";
        for (int i = 0; i < rowViews.Count; i++)
        {
            int c = 0;
            if (entryChosenCounts != null)
                entryChosenCounts.TryGetValue(i, out c);

            var row = rowViews[i];
            if (c == 0 && chosenTimes > 0)
                row.Root.AddToClassList("dead-entry");
            else
                row.Root.RemoveFromClassList("dead-entry");
        }
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            AddToClassList("selected");
        else
            RemoveFromClassList("selected");
    }
}
