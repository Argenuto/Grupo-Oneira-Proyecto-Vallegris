using System;
using System.Collections.Generic;
using UnityEngine;

public static class BossTreeLayout
{
    public const float NodeWidth = 260f;
    public const float BranchHeight = 64f;
    public const float LeafHeaderHeight = 28f;
    public const float EntryRowHeight = 26f;
    public const float LeafPaddingBottom = 12f;
    public const float VerticalSpacing = 60f;
    public const float HorizontalSpacing = 40f;

    public class NodeLayoutInfo
    {
        public int NodeIndex;
        public BossDecisionNode Node;
        public Rect Bounds;
        public int Depth;
    }

    public class LayoutResult
    {
        public readonly Dictionary<int, NodeLayoutInfo> Nodes = new Dictionary<int, NodeLayoutInfo>();
        public Rect TotalBounds;
    }

    public static float CalculateNodeHeight(BossDecisionNode node)
    {
        if (node is BossDecisionBranch)
            return BranchHeight;

        if (node is BossDecisionLeaf leaf)
        {
            int entriesCount = leaf.entries != null ? leaf.entries.Count : 0;
            return LeafHeaderHeight + (entriesCount * EntryRowHeight) + LeafPaddingBottom;
        }

        return BranchHeight;
    }

    public static LayoutResult Calculate(BossDecisionTree tree)
    {
        LayoutResult result = new LayoutResult();
        if (tree == null || tree.root == null)
            return result;

        IReadOnlyList<BossDecisionNode> orderedNodes = tree.GetNodesInOrder();
        float currentLeafX = 0f;

        Dictionary<BossDecisionNode, int> nodeIndices = new Dictionary<BossDecisionNode, int>();
        for (int i = 0; i < orderedNodes.Count; i++)
            nodeIndices[orderedNodes[i]] = i;

        // Primero calculamos profundidad y alturas
        ComputeLayout(tree.root, 0, ref currentLeafX, nodeIndices, result);

        // Calcular TotalBounds
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        if (result.Nodes.Count > 0)
        {
            foreach (var kvp in result.Nodes)
            {
                Rect r = kvp.Value.Bounds;
                if (r.xMin < minX) minX = r.xMin;
                if (r.yMin < minY) minY = r.yMin;
                if (r.xMax > maxX) maxX = r.xMax;
                if (r.yMax > maxY) maxY = r.yMax;
            }
            result.TotalBounds = Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
        else
        {
            result.TotalBounds = Rect.zero;
        }

        return result;
    }

    private static float ComputeLayout(BossDecisionNode node, int depth, ref float currentLeafX, Dictionary<BossDecisionNode, int> indices, LayoutResult result)
    {
        if (node == null) return 0f;

        float h = CalculateNodeHeight(node);
        float y = depth * (BranchHeight + VerticalSpacing);

        if (node is BossDecisionBranch branch)
        {
            float yesX = branch.yes != null ? ComputeLayout(branch.yes, depth + 1, ref currentLeafX, indices, result) : -1f;
            float noX = branch.no != null ? ComputeLayout(branch.no, depth + 1, ref currentLeafX, indices, result) : -1f;

            float x;
            if (yesX >= 0f && noX >= 0f)
                x = (yesX + noX) * 0.5f;
            else if (yesX >= 0f)
                x = yesX;
            else if (noX >= 0f)
                x = noX;
            else
            {
                x = currentLeafX;
                currentLeafX += NodeWidth + HorizontalSpacing;
            }

            int idx = indices.TryGetValue(node, out int i) ? i : -1;
            if (idx >= 0)
            {
                result.Nodes[idx] = new NodeLayoutInfo
                {
                    NodeIndex = idx,
                    Node = node,
                    Depth = depth,
                    Bounds = new Rect(x, y, NodeWidth, h)
                };
            }

            return x;
        }
        else
        {
            float x = currentLeafX;
            currentLeafX += NodeWidth + HorizontalSpacing;

            int idx = indices.TryGetValue(node, out int i) ? i : -1;
            if (idx >= 0)
            {
                result.Nodes[idx] = new NodeLayoutInfo
                {
                    NodeIndex = idx,
                    Node = node,
                    Depth = depth,
                    Bounds = new Rect(x, y, NodeWidth, h)
                };
            }

            return x;
        }
    }
}
