using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BossTreeCanvas : VisualElement
{
    private readonly VisualElement content;
    private readonly BossTreeEdgeLayer edgeLayer;
    private readonly VisualElement nodeContainer;

    private float zoom = 1.0f;
    private Vector2 pan = Vector2.zero;
    private bool isPanning;
    private Vector2 panStartMousePos;
    private Vector2 panStartPos;

    private BossTreeLayout.LayoutResult currentLayout;
    private Action<int, BossDecisionNode> onNodeSelected;
    private Action onBackgroundClicked;

    private readonly Dictionary<int, VisualElement> nodeElements = new Dictionary<int, VisualElement>();
    private int selectedNodeIndex = -1;

    public BossTreeCanvas()
    {
        AddToClassList("boss-canvas-container");

        content = new VisualElement();
        content.AddToClassList("boss-canvas-content");
        Add(content);

        edgeLayer = new BossTreeEdgeLayer();
        content.Add(edgeLayer);

        nodeContainer = new VisualElement();
        nodeContainer.style.position = Position.Absolute;
        nodeContainer.style.top = 0;
        nodeContainer.style.left = 0;
        nodeContainer.style.right = 0;
        nodeContainer.style.bottom = 0;
        content.Add(nodeContainer);

        RegisterCallback<PointerDownEvent>(OnPointerDown);
        RegisterCallback<PointerMoveEvent>(OnPointerMove);
        RegisterCallback<PointerUpEvent>(OnPointerUp);
        RegisterCallback<WheelEvent>(OnWheel);

        UpdateTransform();
    }

    public void SetCallbacks(Action<int, BossDecisionNode> nodeSelected, Action backgroundClicked)
    {
        onNodeSelected = nodeSelected;
        onBackgroundClicked = backgroundClicked;
    }

    public void BuildTree(BossDecisionTree tree)
    {
        nodeContainer.Clear();
        nodeElements.Clear();
        selectedNodeIndex = -1;

        if (tree == null || tree.root == null)
        {
            edgeLayer.SetEdges(null);
            currentLayout = null;
            return;
        }

        currentLayout = BossTreeLayout.Calculate(tree);
        IReadOnlyList<BossDecisionNode> orderedNodes = tree.GetNodesInOrder();
        List<BossTreeEdgeLayer.EdgeData> edges = new List<BossTreeEdgeLayer.EdgeData>();

        foreach (var kvp in currentLayout.Nodes)
        {
            int index = kvp.Key;
            var info = kvp.Value;
            Rect bounds = info.Bounds;

            if (info.Node is BossDecisionBranch branch)
            {
                BossBranchNodeElement branchElem = new BossBranchNodeElement(index, branch);
                branchElem.style.left = bounds.x;
                branchElem.style.top = bounds.y;
                branchElem.style.width = bounds.width;
                branchElem.style.height = bounds.height;

                branchElem.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        SelectNode(index, branch);
                        evt.StopPropagation();
                    }
                });

                nodeContainer.Add(branchElem);
                nodeElements[index] = branchElem;

                // Crear aristas hacia Sí y No
                if (branch.yes != null)
                {
                    int yesIdx = tree.IndexOf(branch.yes);
                    if (yesIdx >= 0 && currentLayout.Nodes.TryGetValue(yesIdx, out var childInfo))
                    {
                        edges.Add(new BossTreeEdgeLayer.EdgeData
                        {
                            FromNodeIndex = index,
                            ToNodeIndex = yesIdx,
                            IsYes = true,
                            StartPoint = new Vector2(bounds.x + 30f, bounds.yMax),
                            EndPoint = new Vector2(childInfo.Bounds.center.x, childInfo.Bounds.yMin)
                        });
                    }
                }

                if (branch.no != null)
                {
                    int noIdx = tree.IndexOf(branch.no);
                    if (noIdx >= 0 && currentLayout.Nodes.TryGetValue(noIdx, out var childInfo))
                    {
                        edges.Add(new BossTreeEdgeLayer.EdgeData
                        {
                            FromNodeIndex = index,
                            ToNodeIndex = noIdx,
                            IsYes = false,
                            StartPoint = new Vector2(bounds.xMax - 30f, bounds.yMax),
                            EndPoint = new Vector2(childInfo.Bounds.center.x, childInfo.Bounds.yMin)
                        });
                    }
                }
            }
            else if (info.Node is BossDecisionLeaf leaf)
            {
                BossLeafNodeElement leafElem = new BossLeafNodeElement(index, leaf);
                leafElem.style.left = bounds.x;
                leafElem.style.top = bounds.y;
                leafElem.style.width = bounds.width;
                leafElem.style.height = bounds.height;

                leafElem.RegisterCallback<PointerDownEvent>(evt =>
                {
                    if (evt.button == 0)
                    {
                        SelectNode(index, leaf);
                        evt.StopPropagation();
                    }
                });

                nodeContainer.Add(leafElem);
                nodeElements[index] = leafElem;
            }
        }

        edgeLayer.SetEdges(edges);
        FrameAll();
    }

    public void ShowPatternChain(BossPattern pattern)
    {
        nodeContainer.Clear();
        nodeElements.Clear();
        edgeLayer.SetEdges(null);
        currentLayout = null;
        selectedNodeIndex = -1;

        BossPatternChainElement chainElem = new BossPatternChainElement(pattern);
        chainElem.style.position = Position.Absolute;
        chainElem.style.left = 40;
        chainElem.style.top = 40;
        nodeContainer.Add(chainElem);

        pan = Vector2.zero;
        zoom = 1f;
        UpdateTransform();
    }

    public void SelectNode(int index, BossDecisionNode node)
    {
        selectedNodeIndex = index;
        foreach (var kvp in nodeElements)
        {
            if (kvp.Value is BossBranchNodeElement branchElem)
                branchElem.SetSelected(kvp.Key == index);
            else if (kvp.Value is BossLeafNodeElement leafElem)
                leafElem.SetSelected(kvp.Key == index);
        }

        onNodeSelected?.Invoke(index, node);
    }

    public void ClearSelection()
    {
        selectedNodeIndex = -1;
        foreach (var kvp in nodeElements)
        {
            if (kvp.Value is BossBranchNodeElement branchElem)
                branchElem.SetSelected(false);
            else if (kvp.Value is BossLeafNodeElement leafElem)
                leafElem.SetSelected(false);
        }
        onBackgroundClicked?.Invoke();
    }

    public void HighlightDecision(BossDecisionRecord record, BossController controller)
    {
        if (record == null) return;

        HashSet<(int from, int to)> activeEdges = new HashSet<(int from, int to)>();
        if (record.NodePath != null && record.NodePath.Length > 1)
        {
            for (int i = 0; i < record.NodePath.Length - 1; i++)
            {
                activeEdges.Add((record.NodePath[i], record.NodePath[i + 1]));
            }
        }
        edgeLayer.SetActiveEdges(activeEdges);

        HashSet<int> pathNodes = new HashSet<int>(record.NodePath ?? Array.Empty<int>());
        foreach (var kvp in nodeElements)
        {
            int idx = kvp.Key;
            if (kvp.Value is BossBranchNodeElement bElem)
            {
                bElem.SetActivePath(pathNodes.Contains(idx));
            }
            else if (kvp.Value is BossLeafNodeElement lElem)
            {
                if (idx == record.LeafNodeIndex)
                    lElem.HighlightDecision(record, controller, record.PhaseIndex);
                else
                    lElem.ResetHighlights();
            }
        }
    }

    public void UpdateLiveCooldowns(BossController controller, int phaseIndex)
    {
        foreach (var kvp in nodeElements)
        {
            if (kvp.Value is BossLeafNodeElement leafElem)
                leafElem.UpdateLiveCooldowns(controller, phaseIndex);
        }
    }

    public void ApplyStats(Dictionary<int, int> leafChosenCounts, Dictionary<int, Dictionary<int, int>> entryCounts)
    {
        foreach (var kvp in nodeElements)
        {
            if (kvp.Value is BossLeafNodeElement leafElem)
            {
                int c = 0;
                if (leafChosenCounts != null)
                    leafChosenCounts.TryGetValue(kvp.Key, out c);

                Dictionary<int, int> eCounts = null;
                if (entryCounts != null)
                    entryCounts.TryGetValue(kvp.Key, out eCounts);

                leafElem.ApplyStats(c, eCounts);
            }
        }
    }

    public void ResetHighlights()
    {
        edgeLayer.SetActiveEdges(null);
        foreach (var kvp in nodeElements)
        {
            if (kvp.Value is BossBranchNodeElement bElem)
                bElem.SetActivePath(false);
            else if (kvp.Value is BossLeafNodeElement lElem)
                lElem.ResetHighlights();
        }
    }

    public void FrameAll()
    {
        if (currentLayout == null || currentLayout.Nodes.Count == 0)
        {
            pan = new Vector2(40, 40);
            zoom = 1f;
            UpdateTransform();
            return;
        }

        Rect bounds = currentLayout.TotalBounds;
        float viewportWidth = resolvedStyle.width > 0 ? resolvedStyle.width : 600f;
        float viewportHeight = resolvedStyle.height > 0 ? resolvedStyle.height : 400f;

        float margin = 40f;
        float availWidth = Mathf.Max(100f, viewportWidth - margin * 2f);
        float availHeight = Mathf.Max(100f, viewportHeight - margin * 2f);

        float scaleX = availWidth / Mathf.Max(1f, bounds.width);
        float scaleY = availHeight / Mathf.Max(1f, bounds.height);
        zoom = Mathf.Clamp(Mathf.Min(scaleX, scaleY), 0.3f, 1.2f);

        float centerX = bounds.center.x * zoom;
        float centerY = bounds.center.y * zoom;

        pan.x = (viewportWidth * 0.5f) - centerX;
        pan.y = (viewportHeight * 0.5f) - centerY;

        UpdateTransform();
    }

    private void UpdateTransform()
    {
        content.style.translate = new Translate(pan.x, pan.y);
        content.style.scale = new Scale(new Vector3(zoom, zoom, 1f));
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button == 1 || evt.button == 2 || (evt.button == 0 && evt.actionKey))
        {
            isPanning = true;
            panStartMousePos = evt.position;
            panStartPos = pan;
            this.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }
        else if (evt.button == 0)
        {
            ClearSelection();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (isPanning && this.HasPointerCapture(evt.pointerId))
        {
            Vector2 delta = (Vector2)evt.position - panStartMousePos;
            pan = panStartPos + delta;
            UpdateTransform();
            evt.StopPropagation();
        }
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (isPanning && this.HasPointerCapture(evt.pointerId))
        {
            isPanning = false;
            this.ReleasePointer(evt.pointerId);
            evt.StopPropagation();
        }
    }

    private void OnWheel(WheelEvent evt)
    {
        float zoomDelta = evt.delta.y > 0 ? (1f / 1.1f) : 1.1f;
        float newZoom = Mathf.Clamp(zoom * zoomDelta, 0.3f, 2.0f);

        if (!Mathf.Approximately(newZoom, zoom))
        {
            Vector2 mouse = evt.localMousePosition;
            pan = mouse - (mouse - pan) * (newZoom / zoom);
            zoom = newZoom;
            UpdateTransform();
        }

        evt.StopPropagation();
    }
}
