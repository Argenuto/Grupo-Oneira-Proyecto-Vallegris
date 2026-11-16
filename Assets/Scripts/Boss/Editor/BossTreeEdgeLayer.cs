using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BossTreeEdgeLayer : VisualElement
{
    public struct EdgeData
    {
        public Vector2 StartPoint;
        public Vector2 EndPoint;
        public bool IsYes;
        public int FromNodeIndex;
        public int ToNodeIndex;
        public bool IsActive;
    }

    private readonly List<EdgeData> edges = new List<EdgeData>();
    private readonly VisualElement labelContainer;

    public BossTreeEdgeLayer()
    {
        pickingMode = PickingMode.Ignore;
        AddToClassList("boss-edge-layer");

        labelContainer = new VisualElement();
        labelContainer.pickingMode = PickingMode.Ignore;
        Add(labelContainer);

        generateVisualContent += OnGenerateVisualContent;
    }

    public void SetEdges(List<EdgeData> newEdges)
    {
        edges.Clear();
        if (newEdges != null)
            edges.AddRange(newEdges);

        UpdateLabels();
        MarkDirtyRepaint();
    }

    public void SetActiveEdges(HashSet<(int from, int to)> activeEdges)
    {
        bool changed = false;
        for (int i = 0; i < edges.Count; i++)
        {
            EdgeData e = edges[i];
            bool active = activeEdges != null && activeEdges.Contains((e.FromNodeIndex, e.ToNodeIndex));
            if (e.IsActive != active)
            {
                e.IsActive = active;
                edges[i] = e;
                changed = true;
            }
        }

        if (changed)
            MarkDirtyRepaint();
    }

    private void UpdateLabels()
    {
        labelContainer.Clear();
        for (int i = 0; i < edges.Count; i++)
        {
            EdgeData edge = edges[i];
            Label lbl = new Label(edge.IsYes ? "Sí" : "No");
            lbl.AddToClassList("edge-label");
            lbl.AddToClassList(edge.IsYes ? "edge-label-yes" : "edge-label-no");

            // Posicionar etiqueta un poco debajo del origen
            float labelX = edge.StartPoint.x + (edge.IsYes ? -24f : 8f);
            float labelY = edge.StartPoint.y + 6f;

            lbl.style.left = labelX;
            lbl.style.top = labelY;
            labelContainer.Add(lbl);
        }
    }

    private void OnGenerateVisualContent(MeshGenerationContext mgc)
    {
        Painter2D painter = mgc.painter2D;
        if (painter == null) return;

        Color defaultColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);
        Color activeColor = new Color(0.23f, 0.51f, 0.96f, 1f); // Azul brillante
        Color yesColor = new Color(0.3f, 0.75f, 0.4f, 0.85f);
        Color noColor = new Color(0.85f, 0.35f, 0.35f, 0.85f);

        for (int i = 0; i < edges.Count; i++)
        {
            EdgeData edge = edges[i];
            Vector2 p0 = edge.StartPoint;
            Vector2 p3 = edge.EndPoint;

            float deltaY = Mathf.Max(20f, Mathf.Abs(p3.y - p0.y) * 0.5f);
            Vector2 p1 = new Vector2(p0.x, p0.y + deltaY);
            Vector2 p2 = new Vector2(p3.x, p3.y - deltaY);

            painter.BeginPath();
            painter.MoveTo(p0);
            painter.BezierCurveTo(p1, p2, p3);

            if (edge.IsActive)
            {
                painter.strokeColor = activeColor;
                painter.lineWidth = 4f;
            }
            else
            {
                painter.strokeColor = edge.IsYes ? yesColor : noColor;
                painter.lineWidth = 2f;
            }

            painter.Stroke();

            // Pequeña flecha en el destino (p3)
            DrawArrowHead(painter, p3, edge.IsActive ? activeColor : painter.strokeColor);
        }
    }

    private void DrawArrowHead(Painter2D painter, Vector2 target, Color color)
    {
        float arrowSize = 6f;
        painter.BeginPath();
        painter.MoveTo(target);
        painter.LineTo(new Vector2(target.x - arrowSize, target.y - arrowSize * 1.5f));
        painter.LineTo(new Vector2(target.x + arrowSize, target.y - arrowSize * 1.5f));
        painter.ClosePath();
        painter.fillColor = color;
        painter.Fill();
    }
}
